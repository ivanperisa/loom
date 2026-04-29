<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import { useI18n } from 'vue-i18n'
import { recognitionService } from '@/services/recognition.service'
import { useAuthStore } from '@/stores/auth.store'
import type { RecognitionResponse, RecognitionEntryResponse } from '@/types/recognition.types'
import type { LearningAgreementResponse, ExchangeResponse } from '@/types/exchange.types'
import { exportRecognitionExcel } from '@/utils/exportRecognition'

const props = defineProps<{
  exchangeId: string
  exchange: ExchangeResponse
  learningAgreement: LearningAgreementResponse
}>()

const { t } = useI18n()
const authStore = useAuthStore()

const recognition = ref<RecognitionResponse | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const savingGroup = ref<string | null>(null)

const isCoordinator = computed(() => authStore.canActAsCoordinator)

const statusColorClass: Record<string, string> = {
  Draft:     'bg-slate-500/20 text-slate-300 border-slate-400',
  Submitted: 'bg-primary/20 text-primary-light border-primary',
  Approved:  'bg-green-500/20 text-green-300 border-green-400',
  Rejected:  'bg-red-500/20 text-red-300 border-red-400',
}

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

const totalAwardedEcts = computed(() =>
  courseGroups.value.reduce((s, g) => s + g.totalAwardedEcts, 0)
)

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

async function saveGroup(group: CourseGroup) {
  const grades = editableGrades[group.foreignCourseCode]
  if (!grades) return
  savingGroup.value = group.foreignCourseCode
  try {
    let lastData: RecognitionResponse | null = null
    for (const entry of group.entries) {
      const res = await recognitionService.upsertEntry(props.exchangeId, {
        slotMappingId:    entry.slotMappingId,
        enrollmentStatus: grades.enrollmentStatus || null,
        originalGrade:    grades.originalGrade || null,
        ectsGrade:        grades.ectsGrade || null,
        hrGrade:          grades.hrGrade || null,
        examDate:         grades.examDate || null,
      })
      lastData = res.data
    }
    if (lastData) { recognition.value = lastData; initGrades(lastData) }
  } catch {
    error.value = t('common.error')
  } finally {
    savingGroup.value = null
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
function doExport() {
  if (!recognition.value) return
  exportRecognitionExcel(recognition.value, props.learningAgreement, props.exchange)
}
</script>

<template>
  <div>
    <!-- Loading -->
    <div v-if="loading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="h-14 animate-pulse rounded bg-primary/20"></div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="rounded-xl border border-red-400/30 bg-red-900/20 p-6 text-center">
      <p class="text-red-300">{{ error }}</p>
    </div>

    <template v-else-if="recognition">
      <!-- Status + actions bar -->
      <div class="mb-3 flex flex-wrap items-center justify-between gap-3">
        <span
          class="rounded-full border px-3 py-0.5 text-xs font-semibold"
          :class="statusColorClass[recognition.status] ?? statusColorClass.Draft"
        >
          {{ t(`recognitionStatus.${recognition.status}`) }}
        </span>
        <div class="flex gap-2">
          <button
            type="button"
            class="rounded-lg border border-primary/40 px-4 py-2 text-sm font-medium text-primary-light transition hover:bg-primary/10"
            @click="doExport"
          >
            {{ t('recognition.export') }}
          </button>
          <button
            v-if="!isCoordinator && recognition.status === 'Draft'"
            type="button"
            class="rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
            @click="submitRecognition"
          >
            {{ t('recognition.actions.submit') }}
          </button>
          <template v-if="isCoordinator && recognition.status === 'Submitted'">
            <button type="button" class="rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-green-700" @click="approveRecognition">
              {{ t('recognition.actions.approve') }}
            </button>
            <button type="button" class="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-red-700" @click="rejectRecognition">
              {{ t('recognition.actions.reject') }}
            </button>
          </template>
        </div>
      </div>

      <!-- No entries -->
      <div v-if="courseGroups.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center">
        <p class="text-light/60">{{ t('recognition.noEntries') }}</p>
      </div>

      <!-- Recognition table -->
      <div v-else class="overflow-x-auto" style="font-family: Calibri, Arial, sans-serif;">
        <table style="border-collapse: collapse; width: 100%; min-width: 1100px; font-size: 11px; color: #000;">

          <!-- Section title row -->
          <thead>
            <tr>
              <th colspan="17" style="border: 1px solid #aaa; background: #bfbfbf; padding: 4px 6px; text-align: left; font-size: 11px; font-weight: bold; color: #000;">
                Predmeti koji se uznaju za predmete/obveze iz nastavnog programa
              </th>
            </tr>

            <!-- Column group headers -->
            <tr>
              <th colspan="6" style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 10px; font-weight: bold; color: #000;">
                Strani predmet
              </th>
              <th colspan="6" style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 10px; font-weight: bold; color: #000;">
                Priznaje se za predmet
              </th>
              <th colspan="4" style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 10px; font-weight: bold; color: #000;">
                Ocjene
              </th>
              <th rowspan="2" style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 10px; font-weight: bold; color: #000; vertical-align: middle;">
              </th>
            </tr>

            <!-- Column headers -->
            <tr>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 70px;">Šifra predmeta</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 160px;">Naziv engleski</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 90px;">Status predmeta</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 140px;">Naziv - hrvatski</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 65px;">Sati (P/A/L)</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 40px;">ECTS</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 30px;">Rbr.</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 130px;">Naziv</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 55px;">Šifra grupe</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 110px;">Kategorija</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 40px;">Sem.</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 50px;">ECTS prizn.</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 60px;">Ocjena orig.</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 55px;">Ocjena ECTS</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 55px;">Ocjena hrv.</th>
              <th style="border: 1px solid #aaa; background: #d9d9d9; padding: 3px 4px; text-align: center; font-size: 9px; font-weight: normal; color: #000; min-width: 80px;">Datum polag.</th>
            </tr>
          </thead>

          <tbody>
            <template v-for="(group, gi) in courseGroups" :key="group.foreignCourseCode">
              <!-- First entry row (rowspanned for foreign course + grade columns) -->
              <tr>
                <td :rowspan="group.entries.length" style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle; text-align: center; background: #fff; font-weight: bold;">
                  {{ group.foreignCourseCode }}
                </td>
                <td :rowspan="group.entries.length" style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle; background: #fff;">
                  {{ group.foreignCourseNameEn }}
                  <div v-if="group.foreignCourseNameHr" style="font-size: 9px; color: #555; font-style: italic;">{{ group.foreignCourseNameHr }}</div>
                </td>
                <!-- Status input — rowspanned -->
                <td :rowspan="group.entries.length" style="border: 1px solid #aaa; padding: 2px 3px; vertical-align: middle; background: #fff;">
                  <input
                    v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.enrollmentStatus"
                    type="text"
                    :disabled="isCoordinator"
                    style="width: 100%; border: none; outline: none; font-family: Calibri, Arial, sans-serif; font-size: 11px; background: transparent; text-align: center;"
                    placeholder="—"
                  />
                </td>
                <td :rowspan="group.entries.length" style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle; background: #fff; font-size: 10px; color: #555;">
                  {{ group.foreignCourseNameHr ?? '—' }}
                </td>
                <td :rowspan="group.entries.length" style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle; text-align: center; background: #fff;">
                  {{ group.foreignCourseHours ?? '—' }}
                </td>
                <td :rowspan="group.entries.length" style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle; text-align: center; background: #fff; font-weight: bold;">
                  {{ group.foreignCourseEcts }}
                </td>

                <!-- First slot row -->
                <td style="border: 1px solid #aaa; padding: 3px 4px; text-align: center; vertical-align: middle;" :style="{ background: group.entries[0].courseSlotColor }">1</td>
                <td style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle;" :style="{ background: group.entries[0].courseSlotColor }">{{ group.entries[0].courseSlotName }}</td>
                <td style="border: 1px solid #aaa; padding: 3px 4px; text-align: center; vertical-align: middle;" :style="{ background: group.entries[0].courseSlotColor }">{{ group.entries[0].courseSlotCode ?? '—' }}</td>
                <td style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle; font-size: 10px;" :style="{ background: group.entries[0].courseSlotColor }">{{ group.entries[0].courseSlotCategoryName }}</td>
                <td style="border: 1px solid #aaa; padding: 3px 4px; text-align: center; vertical-align: middle;" :style="{ background: group.entries[0].courseSlotColor }">{{ group.entries[0].courseSlotSemester }}</td>
                <td style="border: 1px solid #aaa; padding: 3px 4px; text-align: center; vertical-align: middle; font-weight: bold;" :style="{ background: group.entries[0].courseSlotColor }">{{ group.entries[0].awardedEcts }}</td>

                <!-- Grade inputs — rowspanned -->
                <td :rowspan="group.entries.length" style="border: 1px solid #aaa; padding: 2px 3px; vertical-align: middle; background: #fff;">
                  <input
                    v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.originalGrade"
                    type="text"
                    :disabled="isCoordinator"
                    style="width: 100%; border: none; outline: none; font-family: Calibri, Arial, sans-serif; font-size: 11px; background: transparent; text-align: center;"
                    placeholder="—"
                  />
                </td>
                <td :rowspan="group.entries.length" style="border: 1px solid #aaa; padding: 2px 3px; vertical-align: middle; background: #fff;">
                  <input
                    v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.ectsGrade"
                    type="text"
                    :disabled="isCoordinator"
                    style="width: 100%; border: none; outline: none; font-family: Calibri, Arial, sans-serif; font-size: 11px; background: transparent; text-align: center;"
                    placeholder="—"
                  />
                </td>
                <td :rowspan="group.entries.length" style="border: 1px solid #aaa; padding: 2px 3px; vertical-align: middle; background: #fff;">
                  <input
                    v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.hrGrade"
                    type="text"
                    :disabled="isCoordinator"
                    style="width: 100%; border: none; outline: none; font-family: Calibri, Arial, sans-serif; font-size: 11px; background: transparent; text-align: center;"
                    placeholder="—"
                  />
                </td>
                <td :rowspan="group.entries.length" style="border: 1px solid #aaa; padding: 2px 3px; vertical-align: middle; background: #fff;">
                  <input
                    v-if="editableGrades[group.foreignCourseCode]"
                    v-model="editableGrades[group.foreignCourseCode]!.examDate"
                    type="date"
                    :disabled="isCoordinator"
                    style="width: 100%; border: none; outline: none; font-family: Calibri, Arial, sans-serif; font-size: 11px; background: transparent;"
                  />
                </td>

                <!-- Save button — rowspanned -->
                <td :rowspan="group.entries.length" style="border: 1px solid #aaa; padding: 3px; vertical-align: middle; text-align: center; background: #fff;">
                  <button
                    v-if="!isCoordinator"
                    type="button"
                    :disabled="savingGroup === group.foreignCourseCode"
                    style="font-family: Calibri, Arial, sans-serif; font-size: 10px; padding: 3px 8px; cursor: pointer; background: #EA580C; color: #fff; border: none; border-radius: 3px;"
                    :style="savingGroup === group.foreignCourseCode ? { opacity: '0.5', cursor: 'default' } : {}"
                    @click="saveGroup(group)"
                  >
                    {{ savingGroup === group.foreignCourseCode ? '...' : t('settings.institutions.save') }}
                  </button>
                </td>
              </tr>

              <!-- Additional slot rows (no foreign course / grade cells — those are rowspanned) -->
              <tr v-for="(entry, idx) in group.entries.slice(1)" :key="entry.slotMappingId">
                <td style="border: 1px solid #aaa; padding: 3px 4px; text-align: center; vertical-align: middle;" :style="{ background: entry.courseSlotColor }">{{ idx + 2 }}</td>
                <td style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle;" :style="{ background: entry.courseSlotColor }">{{ entry.courseSlotName }}</td>
                <td style="border: 1px solid #aaa; padding: 3px 4px; text-align: center; vertical-align: middle;" :style="{ background: entry.courseSlotColor }">{{ entry.courseSlotCode ?? '—' }}</td>
                <td style="border: 1px solid #aaa; padding: 3px 4px; vertical-align: middle; font-size: 10px;" :style="{ background: entry.courseSlotColor }">{{ entry.courseSlotCategoryName }}</td>
                <td style="border: 1px solid #aaa; padding: 3px 4px; text-align: center; vertical-align: middle;" :style="{ background: entry.courseSlotColor }">{{ entry.courseSlotSemester }}</td>
                <td style="border: 1px solid #aaa; padding: 3px 4px; text-align: center; vertical-align: middle; font-weight: bold;" :style="{ background: entry.courseSlotColor }">{{ entry.awardedEcts }}</td>
              </tr>
            </template>

            <!-- UKUPNO row -->
            <tr>
              <td colspan="11" style="border: 1px solid #aaa; padding: 3px 6px; background: #d9d9d9; font-weight: bold; font-size: 11px; color: #000; text-align: right;">
                UKUPNO
              </td>
              <td style="border: 1px solid #aaa; padding: 3px 4px; background: #d9d9d9; font-weight: bold; text-align: center; color: #000;">
                {{ totalAwardedEcts }}
              </td>
              <td colspan="5" style="border: 1px solid #aaa; background: #d9d9d9;"></td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>
