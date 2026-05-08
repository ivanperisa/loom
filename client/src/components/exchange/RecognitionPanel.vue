<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import { useI18n } from 'vue-i18n'
import { useExchangeStore } from '@/stores/exchange.store'
import { useAuthStore } from '@/stores/auth.store'
import type { RecognitionEntryResponse } from '@/types/recognition.types'
import type { LearningAgreementResponse, ExchangeResponse } from '@/types/exchange.types'
import { exportRecognitionExcel } from '@/utils/exportRecognition'
import StatusBadge from '@/components/common/StatusBadge.vue'
import UnsavedChangesBar from '@/components/common/UnsavedChangesBar.vue'
import { documentStatus } from '@/utils/documentStatus'

const props = defineProps<{
  exchangeId: string
  exchange: ExchangeResponse
  learningAgreement: LearningAgreementResponse
  studyProfileName: string
}>()

const { t, locale } = useI18n()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()

const loading = ref(true)
const isSaving = ref(false)
const hasUnsavedChanges = computed(() => {
  if (!exchangeStore.serverRecognition) return false
  return courseGroups.value.some((g) => {
    const e = editableGrades[g.foreignCourseCode]
    if (!e) return false
    return g.entries.some(
      (x) =>
        x.enrollmentStatus !== (e.enrollmentStatus || null) ||
        x.originalGrade !== (e.originalGrade || null) ||
        x.ectsGrade !== (e.ectsGrade || null) ||
        x.hrGrade !== (e.hrGrade || null) ||
        x.examDate !== (e.examDate || null),
    )
  })
})

const isCoordinator = computed(() => authStore.canActAsCoordinator)

interface GradeData {
  enrollmentStatus: string
  originalGrade: string
  ectsGrade: string
  hrGrade: string
  examDate: string
}

interface CourseGroup {
  foreignCourseCode: string
  foreignCourseNameEn: string
  foreignCourseNameHr: string | null
  foreignCourseEcts: number
  foreignCourseHours: string | null
  totalAwardedEcts: number
  entries: RecognitionEntryResponse[]
}

const editableGrades = reactive<Record<string, GradeData>>({})

const courseGroups = computed<CourseGroup[]>(() => {
  if (!exchangeStore.serverRecognition) return []
  const map = new Map<string, CourseGroup>()
  for (const entry of exchangeStore.serverRecognition.entries) {
    const code = entry.foreignCourseCode
    if (!map.has(code)) {
      map.set(code, {
        foreignCourseCode: code,
        foreignCourseNameEn: entry.foreignCourseNameEn,
        foreignCourseNameHr: entry.foreignCourseNameHr,
        foreignCourseEcts: entry.foreignCourseEcts,
        foreignCourseHours: entry.foreignCourseHours,
        totalAwardedEcts: 0,
        entries: [],
      })
    }
    const g = map.get(code)!
    g.totalAwardedEcts = Math.round((g.totalAwardedEcts + entry.awardedEcts) * 10) / 10
    g.entries.push(entry)
  }
  return Array.from(map.values())
})

function groupIsRejected(group: CourseGroup): boolean {
  return group.entries.some((e) => e.isRecognized === false)
}

function initGrades() {
  const rec = exchangeStore.serverRecognition
  if (!rec) return
  const seen = new Set<string>()
  for (const entry of rec.entries) {
    if (seen.has(entry.foreignCourseCode)) continue
    seen.add(entry.foreignCourseCode)
    editableGrades[entry.foreignCourseCode] = {
      enrollmentStatus: entry.enrollmentStatus ?? '',
      originalGrade: entry.originalGrade ?? '',
      ectsGrade: entry.ectsGrade ?? '',
      hrGrade: entry.hrGrade ?? '',
      examDate: entry.examDate ?? '',
    }
  }
}

onMounted(async () => {
  try {
    if (!exchangeStore.serverRecognition) {
      await exchangeStore.fetchRecognition(props.exchangeId)
    }
    initGrades()
  } finally {
    loading.value = false
  }
})

async function saveAll() {
  isSaving.value = true
  try {
    const entriesToSave = courseGroups.value.flatMap((g) => {
      const grades = editableGrades[g.foreignCourseCode]
      if (!grades) return []
      return g.entries.map((e) => ({
        learningAgreementEntryId: e.learningAgreementEntryId,
        enrollmentStatus: grades.enrollmentStatus || null,
        originalGrade: grades.originalGrade || null,
        ectsGrade: grades.ectsGrade || null,
        hrGrade: grades.hrGrade || null,
        examDate: grades.examDate || null,
      }))
    })
    await exchangeStore.saveRecognition(props.exchangeId, { entries: entriesToSave })
    initGrades()
  } finally {
    isSaving.value = false
  }
}

async function toggleGroupRecognition(group: CourseGroup) {
  if (!isCoordinator.value || !exchangeStore.serverRecognition) return
  isSaving.value = true
  try {
    const newValue = groupIsRejected(group)
    for (const entry of group.entries) {
      await exchangeStore.setEntryRecognized(props.exchangeId, entry.id, newValue)
    }
  } finally {
    isSaving.value = false
  }
}

async function submitRecognition() {
  await exchangeStore.updateRecognitionStatus(props.exchangeId, {
    status: documentStatus.Submitted,
  })
}
async function approveRecognition() {
  await exchangeStore.updateRecognitionStatus(props.exchangeId, { status: documentStatus.Approved })
}
async function rejectRecognition() {
  await exchangeStore.updateRecognitionStatus(props.exchangeId, { status: documentStatus.Rejected })
}
async function backToRecognitionDraft() {
  await exchangeStore.updateRecognitionStatus(props.exchangeId, { status: documentStatus.Draft })
}
function discardChanges() {
  initGrades()
}

function doExport() {
  if (!exchangeStore.serverRecognition) return
  exportRecognitionExcel(
    exchangeStore.serverRecognition,
    props.learningAgreement,
    props.exchange,
    locale.value,
  )
}

const totalCols = computed(() => 16)

const rejectedBg = '#FFCCCC'
</script>

<template>
  <div>
    <div v-if="loading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="h-14 animate-pulse rounded bg-primary/20"></div>
    </div>

    <template v-else-if="exchangeStore.serverRecognition">
      <!-- Status + actions bar -->
      <div class="relative mb-3 flex flex-wrap items-center justify-between gap-3">
        <div class="flex items-center gap-3">
          <StatusBadge :status="exchangeStore.serverRecognition!.status" />
        </div>
        <span
          class="pointer-events-none absolute left-1/2 -translate-x-1/2 text-sm font-semibold text-light/80"
        >
          {{ studyProfileName }}
        </span>
        <div class="flex flex-wrap gap-2">
          <button
            type="button"
            class="rounded-lg border border-primary/40 px-4 py-2 text-sm font-medium text-primary-light transition hover:bg-primary/10"
            @click="doExport"
          >
            {{ t('recognition.export') }}
          </button>
          <!-- Student actions -->
          <template v-if="!isCoordinator">
            <button
              v-if="exchangeStore.serverRecognition!.status === documentStatus.Draft"
              type="button"
              class="rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
              @click="submitRecognition"
            >
              {{ t('recognition.actions.submit') }}
            </button>
            <template
              v-else-if="exchangeStore.serverRecognition!.status === documentStatus.Submitted"
            >
              <span
                class="inline-block rounded-lg border border-primary/20 px-4 py-2 text-sm text-light/60"
              >
                {{ t('exchange.status.waitingApproval') }}
              </span>
              <button
                type="button"
                class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40"
                @click="backToRecognitionDraft"
              >
                {{ t('recognition.actions.backToDraft') }}
              </button>
            </template>
            <button
              v-else-if="exchangeStore.serverRecognition!.status === documentStatus.Rejected"
              type="button"
              class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40"
              @click="backToRecognitionDraft"
            >
              {{ t('recognition.actions.backToDraft') }}
            </button>
          </template>
          <!-- Coordinator actions -->
          <template v-if="isCoordinator">
            <template v-if="exchangeStore.serverRecognition!.status === documentStatus.Submitted">
              <button
                type="button"
                class="rounded-lg bg-green-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-green-500"
                @click="approveRecognition"
              >
                {{ t('recognition.actions.approve') }}
              </button>
              <button
                type="button"
                class="rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-red-500"
                @click="rejectRecognition"
              >
                {{ t('recognition.actions.reject') }}
              </button>
            </template>
            <button
              v-if="
                exchangeStore.serverRecognition!.status === documentStatus.Approved ||
                exchangeStore.serverRecognition!.status === documentStatus.Rejected
              "
              type="button"
              class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40"
              @click="backToRecognitionDraft"
            >
              {{ t('recognition.actions.backToDraft') }}
            </button>
          </template>
        </div>
      </div>

      <!-- Unsaved changes bar -->
      <UnsavedChangesBar
        v-if="hasUnsavedChanges"
        :saving="isSaving"
        @save="saveAll"
        @discard="discardChanges"
      />

      <div
        v-if="courseGroups.length === 0"
        class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center"
      >
        <p class="text-light/60">{{ t('recognition.noEntries') }}</p>
      </div>

      <!-- Recognition table -->
      <div v-else class="overflow-x-auto doc-table-wrap">
        <table
          style="
            border-collapse: collapse;
            width: 100%;
            min-width: 1200px;
            font-size: 11px;
            color: #000;
          "
        >
          <thead>
            <!-- Row 1: super-headers + rowspanned headers -->
            <tr>
              <th rowspan="2" class="rec-th" style="min-width: 70px">
                {{ t('recognition.col.foreignCode') }}
              </th>
              <th rowspan="2" class="rec-th" style="min-width: 160px">
                {{ t('recognition.col.foreignNameEn') }}
              </th>
              <th rowspan="2" class="rec-th" style="min-width: 90px">
                {{ t('recognition.col.enrollmentStatus') }}
              </th>
              <th rowspan="2" class="rec-th" style="min-width: 140px">
                {{ t('recognition.col.foreignNameHr') }}
              </th>
              <th rowspan="2" class="rec-th" style="min-width: 70px">
                {{ t('recognition.col.hours') }}
              </th>
              <th rowspan="2" class="rec-th" style="min-width: 40px">
                {{ t('recognition.col.ects') }}
              </th>
              <th rowspan="2" class="rec-th" style="min-width: 28px">
                {{ t('recognition.col.rbr') }}
              </th>

              <!-- Super-header spanning 5 cols (H–L) -->
              <th colspan="5" class="rec-th" style="font-size: 10px">
                {{ t('recognition.col.recognizedAs') }}
              </th>

              <th rowspan="2" class="rec-th" style="min-width: 60px">
                {{ t('recognition.col.originalGrade') }}
              </th>
              <th rowspan="2" class="rec-th" style="min-width: 55px">
                {{ t('recognition.col.ectsGrade') }}
              </th>
              <th rowspan="2" class="rec-th" style="min-width: 55px">
                {{ t('recognition.col.hrGrade') }}
              </th>
              <th rowspan="2" class="rec-th" style="min-width: 80px">
                {{ t('recognition.col.examDate') }}
              </th>
            </tr>

            <!-- Row 2: sub-headers -->
            <tr>
              <th class="rec-th" style="min-width: 130px; border-top: none">
                {{ t('recognition.col.slotName') }}
              </th>
              <th class="rec-th" style="min-width: 55px; border-top: none">
                {{ t('recognition.col.slotCode') }}
              </th>
              <th class="rec-th" style="min-width: 110px; border-top: none">
                {{ t('recognition.col.slotCategory') }}
              </th>
              <th class="rec-th" style="min-width: 38px; border-top: none">
                {{ t('recognition.col.semester') }}
              </th>
              <th class="rec-th" style="min-width: 50px; border-top: none">
                {{ t('recognition.col.awardedEcts') }}
              </th>
            </tr>
          </thead>

          <tbody>
            <template v-for="group in courseGroups" :key="group.foreignCourseCode">
              <tr v-for="(entry, idx) in group.entries" :key="entry.learningAgreementEntryId">
                <!-- A–F: foreign course data, rowspanned -->
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td rec-td--center rec-td--bold"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  <div class="mb-1">{{ group.foreignCourseCode }}</div>

                  <!-- Gumb vidljiv samo koordinatoru -->
                  <div v-if="isCoordinator">
                    <button
                      type="button"
                      @click="toggleGroupRecognition(group)"
                      class="w-full rounded border py-1 px-0.5 text-[9px] uppercase tracking-tighter transition-all active:scale-95"
                      :class="
                        groupIsRejected(group)
                          ? 'bg-green-600 border-green-700 text-white shadow-sm'
                          : 'bg-red-50 text-red-600 border-red-200 hover:bg-red-600 hover:text-white'
                      "
                    >
                      {{
                        groupIsRejected(group)
                          ? t('recognition.actions.approve')
                          : t('recognition.actions.reject')
                      }}
                    </button>
                  </div>
                </td>
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ group.foreignCourseNameEn }}
                  <div
                    v-if="group.foreignCourseNameHr"
                    style="font-size: 9px; color: #555; font-style: italic"
                  >
                    {{ group.foreignCourseNameHr }}
                  </div>
                </td>
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td-grade"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  <input
                    v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.enrollmentStatus"
                    type="text"
                    :disabled="isCoordinator"
                    class="rec-input"
                    placeholder="—"
                  />
                </td>
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td rec-td--center rec-td--small"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ group.foreignCourseNameHr ?? '—' }}
                </td>
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td rec-td--center"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ group.foreignCourseHours ?? '—' }}
                </td>
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td rec-td--center rec-td--bold"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ group.foreignCourseEcts }}
                </td>

                <!-- G: Rbr. per slot -->
                <td
                  class="rec-td rec-td--center"
                  :style="{
                    background: groupIsRejected(group) ? rejectedBg : entry.courseSlotColor,
                  }"
                >
                  {{ idx + 1 }}
                </td>

                <!-- H: slot name -->
                <td
                  class="rec-td"
                  :style="{
                    background: groupIsRejected(group) ? rejectedBg : entry.courseSlotColor,
                  }"
                >
                  {{ entry.courseSlotName }}
                </td>

                <!-- I: slot code -->
                <td
                  class="rec-td rec-td--center"
                  :style="{
                    background: groupIsRejected(group) ? rejectedBg : entry.courseSlotColor,
                  }"
                >
                  {{ entry.courseSlotCode ?? '—' }}
                </td>

                <!-- J: category name -->
                <td
                  class="rec-td rec-td--small"
                  :style="{
                    background: groupIsRejected(group) ? rejectedBg : entry.courseSlotColor,
                  }"
                >
                  {{ entry.courseSlotCategoryName }}
                </td>

                <!-- K: semester -->
                <td
                  class="rec-td rec-td--center"
                  :style="{
                    background: groupIsRejected(group) ? rejectedBg : entry.courseSlotColor,
                  }"
                >
                  {{ entry.courseSlotSemester }}
                </td>

                <!-- L: awarded ECTS -->
                <td
                  class="rec-td rec-td--center rec-td--bold"
                  :style="{
                    background: groupIsRejected(group) ? rejectedBg : entry.courseSlotColor,
                  }"
                >
                  {{ entry.awardedEcts }}
                </td>

                <!-- M–P: grades, rowspanned -->
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td-grade"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  <input
                    v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.originalGrade"
                    type="text"
                    :disabled="isCoordinator"
                    class="rec-input"
                    placeholder="—"
                  />
                </td>
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td-grade"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  <input
                    v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.ectsGrade"
                    type="text"
                    :disabled="isCoordinator"
                    class="rec-input"
                    placeholder="—"
                  />
                </td>
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td-grade"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  <input
                    v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.hrGrade"
                    type="text"
                    :disabled="isCoordinator"
                    class="rec-input"
                    placeholder="—"
                  />
                </td>
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td-grade"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  <input
                    v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.examDate"
                    type="date"
                    :disabled="isCoordinator"
                    class="rec-input rec-input--date"
                  />
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>

<style scoped>
.rec-th {
  border: 1px solid #aaa;
  background: #d9d9d9;
  padding: 3px 4px;
  text-align: center;
  font-size: 9px;
  font-weight: bold;
  color: #000;
}

.rec-td {
  border: 1px solid #aaa;
  padding: 3px 4px;
  vertical-align: middle;
}
.rec-td--center {
  text-align: center;
}
.rec-td--bold {
  font-weight: bold;
}
.rec-td--small {
  font-size: 10px;
  color: #555;
}

.rec-td-grade {
  border: 1px solid #aaa;
  padding: 2px 3px;
  vertical-align: middle;
}

.rec-input {
  width: 100%;
  border: none;
  outline: none;
  font-family: Calibri, Arial, sans-serif;
  font-size: 11px;
  background: transparent;
  text-align: center;
}
.rec-input--date {
  text-align: left;
}
</style>
