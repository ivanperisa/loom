<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import { useI18n } from 'vue-i18n'
import { recognitionService } from '@/services/recognition.service'
import { useAuthStore } from '@/stores/auth.store'
import type { RecognitionResponse, RecognitionEntryResponse } from '@/types/recognition.types'
import type { LearningAgreementResponse, ExchangeResponse } from '@/types/exchange.types'
import { exportRecognitionExcel } from '@/utils/exportRecognition'
import StatusBadge from '@/components/common/StatusBadge.vue'
import UnsavedChangesBar from '@/components/common/UnsavedChangesBar.vue'

const props = defineProps<{
  exchangeId: string
  exchange: ExchangeResponse
  learningAgreement: LearningAgreementResponse
}>()

const { t, locale } = useI18n()
const authStore = useAuthStore()

const recognition = ref<RecognitionResponse | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const isSaving = ref(false)
const hasUnsavedChanges = computed(() => {
  if (!recognition.value) return false
  return courseGroups.value.some(g => {
    const e = editableGrades[g.foreignCourseCode]
    if (!e) return false
    return g.entries.some(x => 
      x.enrollmentStatus !== (e.enrollmentStatus || null) ||
      x.originalGrade !== (e.originalGrade || null) ||
      x.ectsGrade !== (e.ectsGrade || null) ||
      x.hrGrade !== (e.hrGrade || null) ||
      x.examDate !== (e.examDate || null)
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
  if (!recognition.value) return []
  const map = new Map<string, CourseGroup>()
  for (const entry of recognition.value.entries) {
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
  return group.entries.some(e => e.isRecognized === false)
}

function initGrades(rec: RecognitionResponse) {
  const seen = new Set<string>()
  for (const entry of rec.entries) {
    if (seen.has(entry.foreignCourseCode)) continue
    seen.add(entry.foreignCourseCode)
    editableGrades[entry.foreignCourseCode] = {
      enrollmentStatus: entry.enrollmentStatus ?? '',
      originalGrade:    entry.originalGrade ?? '',
      ectsGrade:        entry.ectsGrade ?? '',
      hrGrade:          entry.hrGrade ?? '',
      examDate:         entry.examDate ?? '',
    }
  }
}

onMounted(async () => {
  try {
    const res = await recognitionService.getOrCreate(props.exchangeId)
    recognition.value = res.data
    initGrades(res.data)
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
})

async function saveAll() {
  isSaving.value = true
  try {
    const entriesToSave = courseGroups.value.flatMap(g => {
      const grades = editableGrades[g.foreignCourseCode]
      if (!grades) return []
      return g.entries.map(e => ({
        learningAgreementEntryId: e.learningAgreementEntryId,
        enrollmentStatus: grades.enrollmentStatus || null,
        originalGrade: grades.originalGrade || null,
        ectsGrade: grades.ectsGrade || null,
        hrGrade: grades.hrGrade || null,
        examDate: grades.examDate || null
      }))
    })
    const res = await recognitionService.saveRecognition(props.exchangeId, { entries: entriesToSave })
    recognition.value = res.data
    initGrades(res.data)
  } catch {
    error.value = t("common.error")
  } finally {
    isSaving.value = false
  }
}

async function toggleGroupRecognition(group: CourseGroup) {
  if (!isCoordinator.value || !recognition.value) return
  
  const isCurrentlyRejected = groupIsRejected(group)
  const newValue = isCurrentlyRejected
  
  try {
    isSaving.value = true
    
    const promises = group.entries.map(entry => 
      recognitionService.setEntryRecognized(props.exchangeId, entry.id, newValue)
    )
    
    const results = await Promise.all(promises)
    
    const last = results[results.length - 1]
    if (last !== undefined) recognition.value = last.data
  } catch {
    error.value = t('common.error')
  } finally {
    isSaving.value = false
  }
}

async function submitRecognition() {
  try { const res = await recognitionService.updateStatus(props.exchangeId, { status: 'Submitted' }); recognition.value = res.data } catch { error.value = t('common.error') }
}
async function approveRecognition() {
  try { const res = await recognitionService.updateStatus(props.exchangeId, { status: 'Approved' }); recognition.value = res.data } catch { error.value = t('common.error') }
}
async function rejectRecognition() {
  try { const res = await recognitionService.updateStatus(props.exchangeId, { status: 'Rejected' }); recognition.value = res.data } catch { error.value = t('common.error') }
}
async function backToRecognitionDraft() {
  try { const res = await recognitionService.updateStatus(props.exchangeId, { status: 'Draft' }); recognition.value = res.data } catch { error.value = t('common.error') }
}
function discardChanges() {
  if (recognition.value) initGrades(recognition.value)
}

function doExport() {
  if (!recognition.value) return
  exportRecognitionExcel(recognition.value, props.learningAgreement, props.exchange, locale.value)
}

const totalCols = computed(() => 16)

const thStyle = 'border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: bold; color: #000;'
const rejectedBg = '#FFCCCC'
</script>

<template>
  <div>
    <div v-if="loading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="h-14 animate-pulse rounded bg-primary/20"></div>
    </div>

    <div v-else-if="error" class="rounded-xl border border-red-400/30 bg-red-900/20 p-6 text-center">
      <p class="text-red-300">{{ error }}</p>
    </div>

    <template v-else-if="recognition">
      <!-- Status + actions bar -->
      <div class="mb-3 flex flex-wrap items-center justify-between gap-3">
        <div class="flex items-center gap-3">
          <StatusBadge :status="recognition.status" i18n-prefix="recognitionStatus" />
        </div>
        <div class="flex flex-wrap gap-2">
          <button type="button" class="rounded-lg border border-primary/40 px-4 py-2 text-sm font-medium text-primary-light transition hover:bg-primary/10" @click="doExport">
            {{ t('recognition.export') }}
          </button>
          <!-- Student actions -->
          <template v-if="!isCoordinator">
            <button v-if="recognition.status === 'Draft'" type="button" class="rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark" @click="submitRecognition">
              {{ t('recognition.actions.submit') }}
            </button>
            <template v-else-if="recognition.status === 'Submitted'">
              <span class="inline-block rounded-lg border border-primary/20 px-4 py-2 text-sm text-light/60">
                {{ t('recognition.status.waitingApproval') }}
              </span>
              <button type="button" class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40" @click="backToRecognitionDraft">
                {{ t('recognition.actions.backToDraft') }}
              </button>
            </template>
            <button v-else-if="recognition.status === 'Rejected'" type="button" class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40" @click="backToRecognitionDraft">
              {{ t('recognition.actions.backToDraft') }}
            </button>
          </template>
          <!-- Coordinator actions -->
          <template v-if="isCoordinator">
            <template v-if="recognition.status === 'Submitted'">
              <button type="button" class="rounded-lg bg-green-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-green-500" @click="approveRecognition">{{ t('recognition.actions.approve') }}</button>
              <button type="button" class="rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-red-500" @click="rejectRecognition">{{ t('recognition.actions.reject') }}</button>
            </template>
            <button v-if="recognition.status === 'Approved' || recognition.status === 'Rejected'" type="button" class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40" @click="backToRecognitionDraft">
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

      <div v-if="courseGroups.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center">
        <p class="text-light/60">{{ t('recognition.noEntries') }}</p>
      </div>

      <!-- Recognition table -->
      <div v-else class="overflow-x-auto" style="font-family: Calibri, Arial, sans-serif;">
        <table style="border-collapse: collapse; width: 100%; min-width: 1200px; font-size: 11px; color: #000;">
          <thead>
            <!-- Row 1: super-headers + rowspanned headers -->
            <tr>
              <th rowspan="2" :style="thStyle" style="min-width:70px;">{{ t('recognition.col.foreignCode') }}</th>
              <th rowspan="2" :style="thStyle" style="min-width:160px;">{{ t('recognition.col.foreignNameEn') }}</th>
              <th rowspan="2" :style="thStyle" style="min-width:90px;">{{ t('recognition.col.enrollmentStatus') }}</th>
              <th rowspan="2" :style="thStyle" style="min-width:140px;">{{ t('recognition.col.foreignNameHr') }}</th>
              <th rowspan="2" :style="thStyle" style="min-width:70px;">{{ t('recognition.col.hours') }}</th>
              <th rowspan="2" :style="thStyle" style="min-width:40px;">{{ t('recognition.col.ects') }}</th>
              <th rowspan="2" :style="thStyle" style="min-width:28px;">{{ t('recognition.col.rbr') }}</th>

              <!-- Super-header spanning 5 cols (H–L) -->
              <th colspan="5" :style="thStyle" style="font-size:10px;">
                {{ t('recognition.col.recognizedAs') }}
              </th>

              <th rowspan="2" :style="thStyle" style="min-width:60px;">{{ t('recognition.col.originalGrade') }}</th>
              <th rowspan="2" :style="thStyle" style="min-width:55px;">{{ t('recognition.col.ectsGrade') }}</th>
              <th rowspan="2" :style="thStyle" style="min-width:55px;">{{ t('recognition.col.hrGrade') }}</th>
              <th rowspan="2" :style="thStyle" style="min-width:80px;">{{ t('recognition.col.examDate') }}</th>
            </tr>

            <!-- Row 2: sub-headers -->
            <tr>
              <th :style="thStyle" style="min-width:130px; border-top: none;">
                {{ t('recognition.col.slotName') }}
              </th>
              <th :style="thStyle" style="min-width:55px; border-top: none;">
                {{ t('recognition.col.slotCode') }}
              </th>
              <th :style="thStyle" style="min-width:110px; border-top: none;">
                {{ t('recognition.col.slotCategory') }}
              </th>
              <th :style="thStyle" style="min-width:38px; border-top: none;">
                {{ t('recognition.col.semester') }}
              </th>
              <th :style="thStyle" style="min-width:50px; border-top: none;">
                {{ t('recognition.col.awardedEcts') }}
              </th>
            </tr>
          </thead>

          <tbody>
            <template v-for="group in courseGroups" :key="group.foreignCourseCode">
              <tr v-for="(entry, idx) in group.entries" :key="entry.learningAgreementEntryId">

                <!-- A–F: foreign course data, rowspanned -->
                <td v-if="idx === 0" :rowspan="group.entries.length"
                    style="border: 1px solid #aaa; padding: 4px; vertical-align: middle; text-align: center; font-weight: bold;"
                    :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }">
                    
                    <div class="mb-1">{{ group.foreignCourseCode }}</div>

                    <!-- Gumb vidljiv samo koordinatoru -->
                    <div v-if="isCoordinator">
                      <button 
                        type="button"
                        @click="toggleGroupRecognition(group)"
                        class="w-full rounded border py-1 px-0.5 text-[9px] uppercase tracking-tighter transition-all active:scale-95"
                        :class="groupIsRejected(group) 
                          ? 'bg-green-600 border-green-700 text-white shadow-sm' 
                          : 'bg-red-50 text-red-600 border-red-200 hover:bg-red-600 hover:text-white'"
                      >
                        {{ groupIsRejected(group) 
                          ? t('recognition.actions.approve') 
                          : t('recognition.actions.reject') 
                        }}
                      </button>
                    </div>
                </td>
                <td v-if="idx === 0" :rowspan="group.entries.length"
                  style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }">
                  {{ group.foreignCourseNameEn }}
                  <div v-if="group.foreignCourseNameHr" style="font-size:9px;color:#555;font-style:italic;">{{ group.foreignCourseNameHr }}</div>
                </td>
                <td v-if="idx === 0" :rowspan="group.entries.length"
                  style="border: 1px solid #aaa; padding: 2px 3px; vertical-align: middle;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }">
                  <input v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.enrollmentStatus"
                    type="text" :disabled="isCoordinator"
                    style="width:100%;border:none;outline:none;font-family:Calibri,Arial,sans-serif;font-size:11px;background:transparent;text-align:center;" placeholder="—" />
                </td>
                <td v-if="idx === 0" :rowspan="group.entries.length"
                  style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle; text-align:center; font-size:10px; color:#555;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }">
                  {{ group.foreignCourseNameHr ?? '—' }}
                </td>
                <td v-if="idx === 0" :rowspan="group.entries.length"
                  style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle; text-align:center;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }">
                  {{ group.foreignCourseHours ?? '—' }}
                </td>
                <td v-if="idx === 0" :rowspan="group.entries.length"
                  style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle; text-align:center; font-weight:bold;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }">
                  {{ group.foreignCourseEcts }}
                </td>

                <!-- G: Rbr. per slot -->
                <td style="border:1px solid #aaa;padding:3px 4px;text-align:center;vertical-align:middle;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : entry.courseSlotColor }">
                  {{ idx + 1 }}
                </td>

                <!-- H: slot name -->
                <td style="border:1px solid #aaa;padding:3px 4px;vertical-align:middle;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : entry.courseSlotColor }">
                  {{ entry.courseSlotName }}
                </td>

                <!-- I: slot code -->
                <td style="border:1px solid #aaa;padding:3px 4px;text-align:center;vertical-align:middle;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : entry.courseSlotColor }">
                  {{ entry.courseSlotCode ?? '—' }}
                </td>

                <!-- J: category name -->
                <td style="border:1px solid #aaa;padding:3px 4px;vertical-align:middle;font-size:10px;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : entry.courseSlotColor }">
                  {{ entry.courseSlotCategoryName }}
                </td>

                <!-- K: semester -->
                <td style="border:1px solid #aaa;padding:3px 4px;text-align:center;vertical-align:middle;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : entry.courseSlotColor }">
                  {{ entry.courseSlotSemester }}
                </td>

                <!-- L: awarded ECTS -->
                <td style="border:1px solid #aaa;padding:3px 4px;text-align:center;vertical-align:middle;font-weight:bold;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : entry.courseSlotColor }">
                  {{ entry.awardedEcts }}
                </td>

                <!-- M–P: grades, rowspanned -->
                <td v-if="idx === 0" :rowspan="group.entries.length"
                  style="border:1px solid #aaa;padding:2px 3px;vertical-align:middle;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }">
                  <input v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.originalGrade"
                    type="text" :disabled="isCoordinator"
                    style="width:100%;border:none;outline:none;font-family:Calibri,Arial,sans-serif;font-size:11px;background:transparent;text-align:center;" placeholder="—" />
                </td>
                <td v-if="idx === 0" :rowspan="group.entries.length"
                  style="border:1px solid #aaa;padding:2px 3px;vertical-align:middle;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }">
                  <input v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.ectsGrade"
                    type="text" :disabled="isCoordinator"
                    style="width:100%;border:none;outline:none;font-family:Calibri,Arial,sans-serif;font-size:11px;background:transparent;text-align:center;" placeholder="—" />
                </td>
                <td v-if="idx === 0" :rowspan="group.entries.length"
                  style="border:1px solid #aaa;padding:2px 3px;vertical-align:middle;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }">
                  <input v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.hrGrade"
                    type="text" :disabled="isCoordinator"
                    style="width:100%;border:none;outline:none;font-family:Calibri,Arial,sans-serif;font-size:11px;background:transparent;text-align:center;" placeholder="—" />
                </td>
                <td v-if="idx === 0" :rowspan="group.entries.length"
                  style="border:1px solid #aaa;padding:2px 3px;vertical-align:middle;"
                  :style="{ background: groupIsRejected(group) ? rejectedBg : '#fff' }">
                  <input v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.examDate"
                    type="date" :disabled="isCoordinator"
                    style="width:100%;border:none;outline:none;font-family:Calibri,Arial,sans-serif;font-size:11px;background:transparent;" />
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>
