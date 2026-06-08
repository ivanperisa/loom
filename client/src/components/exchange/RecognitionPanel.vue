<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import { useI18n } from 'vue-i18n'
import { useExchangeStore } from '@/stores/exchange.store'
import { useAuthStore } from '@/stores/auth.store'
import type { RecognitionEntryResponse } from '@/types/recognition.types'
import { exportExchangeExcel } from '@/utils/exportExchange'
import StatusBadge from '@/components/common/StatusBadge.vue'
import UnsavedChangesBar from '@/components/common/UnsavedChangesBar.vue'
import { documentStatus } from '@/utils/documentStatus'

const props = defineProps<{
  exchangeId: string
  homeProfileName: string
}>()

const { t, locale } = useI18n()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()

const loading = ref(true)
const isSaving = ref(false)
const hasUnsavedChanges = computed(() => {
  if (!exchangeStore.serverRecognition) return false
  return courseGroups.value.some((g) => {
    const e = editableGrades[g.partnerCourseCode]
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
  partnerCourseCode: string
  partnerCourseName: string
  partnerCourseNameHr: string | null
  partnerCourseEcts: number
  partnerCourseHours: string | null
  totalAwardedEcts: number
  entries: RecognitionEntryResponse[]
}

const editableGrades = reactive<Record<string, GradeData>>({})

const courseGroups = computed<CourseGroup[]>(() => {
  if (!exchangeStore.serverRecognition) return []
  const map = new Map<string, CourseGroup>()
  for (const entry of exchangeStore.serverRecognition.entries) {
    const code = entry.partnerCourseCode
    if (!map.has(code)) {
      map.set(code, {
        partnerCourseCode: code,
        partnerCourseName: entry.partnerCourseName,
        partnerCourseNameHr: entry.partnerCourseNameHr,
        partnerCourseEcts: entry.partnerCourseEcts,
        partnerCourseHours: entry.partnerCourseHours,
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
    if (seen.has(entry.partnerCourseCode)) continue
    seen.add(entry.partnerCourseCode)
    editableGrades[entry.partnerCourseCode] = {
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
      const grades = editableGrades[g.partnerCourseCode]
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
  exportExchangeExcel(
    exchangeStore.serverRecognition,
    exchangeStore.serverLearningAgreement!,
    exchangeStore.exchange!,
    locale.value,
  )
}

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
          {{ homeProfileName }}
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
            <tr>
              <th class="rec-th" style="min-width: 70px">{{ t('recognition.col.partnerCode') }}</th>
              <th class="rec-th" style="min-width: 160px">
                {{ t('recognition.col.partnerName') }}
              </th>
              <th class="rec-th" style="min-width: 90px">
                {{ t('recognition.col.enrollmentStatus') }}
              </th>
              <th class="rec-th" style="min-width: 140px">
                {{ t('recognition.col.partnerNameHr') }}
              </th>
              <th class="rec-th" style="min-width: 70px">
                {{ t('recognition.col.partnerHours') }}
              </th>
              <th class="rec-th" style="min-width: 40px">{{ t('recognition.col.partnerEcts') }}</th>
              <th class="rec-th" style="min-width: 28px">{{ t('recognition.col.rbr') }}</th>
              <th class="rec-th" style="min-width: 70px">
                {{ t('recognition.col.recognizedAs') }}
              </th>
              <th class="rec-th" style="min-width: 130px">
                {{ t('recognition.col.homeSlotCourseName') }}
              </th>
              <th class="rec-th" style="min-width: 55px">
                {{ t('recognition.col.homeSlotCourseGroupIsvuCode') }}
              </th>
              <th class="rec-th" style="min-width: 110px">
                {{ t('recognition.col.homeSlotCourseGroupName') }}
              </th>
              <th class="rec-th" style="min-width: 38px">
                {{ t('recognition.col.homeSlotSemester') }}
              </th>
              <th class="rec-th" style="min-width: 50px">{{ t('recognition.col.awardedEcts') }}</th>
              <th class="rec-th" style="min-width: 60px">
                {{ t('recognition.col.originalGrade') }}
              </th>
              <th class="rec-th" style="min-width: 55px">{{ t('recognition.col.ectsGrade') }}</th>
              <th class="rec-th" style="min-width: 55px">{{ t('recognition.col.hrGrade') }}</th>
              <th class="rec-th" style="min-width: 80px">{{ t('recognition.col.examDate') }}</th>
            </tr>
          </thead>

          <tbody>
            <template v-for="group in courseGroups" :key="group.partnerCourseCode">
              <tr v-for="(entry, idx) in group.entries" :key="entry.learningAgreementEntryId">
                <!-- A: Šifra predmeta -->
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td rec-td--center rec-td--bold"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  <div class="mb-1">{{ group.partnerCourseCode }}</div>
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

                <!-- B: Naziv engleski -->
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ group.partnerCourseName }}
                  <div
                    v-if="group.partnerCourseNameHr"
                    style="font-size: 9px; color: #555; font-style: italic"
                  >
                    {{ group.partnerCourseNameHr }}
                  </div>
                </td>

                <!-- C: Status predmeta -->
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td-grade"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  <input
                    v-if="editableGrades[group.partnerCourseCode]"
                    v-model="editableGrades[group.partnerCourseCode]!.enrollmentStatus"
                    type="text"
                    :disabled="isCoordinator"
                    class="rec-input"
                    placeholder="—"
                  />
                </td>

                <!-- D: Naziv - hrvatski -->
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td rec-td--center rec-td--small"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ group.partnerCourseNameHr ?? '—' }}
                </td>

                <!-- E: Sati -->
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td rec-td--center"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ group.partnerCourseHours ?? '—' }}
                </td>

                <!-- F: ECTS -->
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td rec-td--center rec-td--bold"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ group.partnerCourseEcts }}
                </td>

                <!-- G: Rbr. -->
                <td
                  class="rec-td rec-td--center"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ idx + 1 }}
                </td>

                <!-- H: Priznaje se za predmet -->
                <td
                  class="rec-td rec-td--center"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ entry.homeSlotCourseIsvuCode }}
                </td>

                <!-- I: Naziv -->
                <td
                  class="rec-td"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ entry.homeSlotCourseName }}
                </td>

                <!-- J: Izb. grupa -->
                <td
                  class="rec-td rec-td--center"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ entry.homeSlotCourseGroupIsvuCode ?? '—' }}
                </td>

                <!-- K: Naziv izb. grupe -->
                <td
                  class="rec-td rec-td--center"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : entry.homeSlotColor }"
                >
                  {{ entry.homeSlotCourseGroupName || t('recognition.col.mandatoryCourse') }}
                </td>

                <!-- L: Semestar -->
                <td
                  class="rec-td rec-td--center"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }"
                >
                  {{ entry.homeSlotSemester }}
                </td>

                <!-- M: Priznato ECTS-a -->
                <td
                  class="rec-td rec-td--center rec-td--bold"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : entry.homeSlotColor }"
                >
                  {{ entry.awardedEcts }}
                </td>

                <!-- N: Ocjena originalna -->
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td-grade"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#ddd9c3' }"
                >
                  <input
                    v-if="editableGrades[group.partnerCourseCode]"
                    v-model="editableGrades[group.partnerCourseCode]!.originalGrade"
                    type="text"
                    :disabled="isCoordinator"
                    class="rec-input"
                    placeholder="—"
                  />
                </td>
                <!-- O: Ocjena ECTS -->
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td-grade"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#ddd9c3' }"
                >
                  <input
                    v-if="editableGrades[group.partnerCourseCode]"
                    v-model="editableGrades[group.partnerCourseCode]!.ectsGrade"
                    type="text"
                    :disabled="isCoordinator"
                    class="rec-input"
                    placeholder="—"
                  />
                </td>
                <!-- P: Ocjena hrv. -->
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td-grade"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#ddd9c3' }"
                >
                  <input
                    v-if="editableGrades[group.partnerCourseCode]"
                    v-model="editableGrades[group.partnerCourseCode]!.hrGrade"
                    type="text"
                    :disabled="isCoordinator"
                    class="rec-input"
                    placeholder="—"
                  />
                </td>
                <!-- Q: Datum polaganja -->
                <td
                  v-if="idx === 0"
                  :rowspan="group.entries.length"
                  class="rec-td-grade"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#ddd9c3' }"
                >
                  <input
                    v-if="editableGrades[group.partnerCourseCode]"
                    v-model="editableGrades[group.partnerCourseCode]!.examDate"
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
  background: #ffffcc;
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
