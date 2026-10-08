<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import CourseUrlLink from '@/components/common/CourseUrlLink.vue'

interface RecognitionRow {
  id: string
  partnerCourseId: string | null
  partnerCourseCode: string
  partnerCourseName: string
  partnerCourseNameHr: string | null
  partnerCourseUrl: string | null
  partnerCourseHours: string | null
  partnerCourseEcts: number
  homeSlotCourseIsvuCode: number | null
  homeSlotCourseName: string
  homeSlotCourseGroupIsvuCode: number | null
  homeSlotCourseGroupName: string
  homeSlotColor: string
  homeSlotSemester: number
  awardedEcts: number
  enrollmentStatus: string | null
}

const NOT_PASSED_BG = '#ffcccc'

interface GradeData {
  enrollmentStatus: string
  originalGrade: string
  ectsGrade: string
  hrGrade: string
  examDate: string
}

interface CourseGroup {
  /** The partner course id (grades belong to a course); the code only for rows without one. */
  key: string
  partnerCourseCode: string
  partnerCourseName: string
  partnerCourseNameHr: string | null
  partnerCourseUrl: string | null
  partnerCourseEcts: number
  partnerCourseHours: string | null
  rows: RecognitionRow[][]
  isNotPassed: boolean
}

const props = defineProps<{
  entries: RecognitionRow[]
  readonly: boolean
  /** Grades keyed by partner course id. */
  editableGrades?: Record<string, GradeData>
  /** Results approved: grades are shown, not edited. */
  locked?: boolean
}>()

const { t } = useI18n()

function rowAwardedEcts(row: RecognitionRow[]): number {
  return Math.round(row.reduce((sum, e) => sum + e.awardedEcts, 0) * 10) / 10
}

function canEdit(group: CourseGroup): boolean {
  return !props.readonly && !props.locked && !!props.editableGrades?.[group.key]
}

function shownGrade(group: CourseGroup, field: keyof GradeData): string {
  if (props.readonly || !props.locked) return ''
  const value = props.editableGrades?.[group.key]?.[field] ?? ''
  if (field === 'examDate') return formatExamDateDisplay(value)
  if (field === 'enrollmentStatus') return value === 'Passed' ? t('recognition.enrollment.passed') : value === 'NotPassed' ? t('recognition.enrollment.notPassed') : ''
  return value
}

function formatExamDateDisplay(iso: string): string {
  const [y, m, d] = iso.split('-')
  return y && m && d ? `${d}/${m}/${y}` : ''
}

const courseGroups = computed<CourseGroup[]>(() => {
  const map = new Map<string, CourseGroup>()
  for (const entry of props.entries) {
    const key = entry.partnerCourseId ?? `code:${entry.partnerCourseCode}`
    if (!map.has(key)) {
      map.set(key, {
        key,
        partnerCourseCode: entry.partnerCourseCode,
        partnerCourseName: entry.partnerCourseName,
        partnerCourseNameHr: entry.partnerCourseNameHr,
        partnerCourseUrl: entry.partnerCourseUrl,
        partnerCourseEcts: entry.partnerCourseEcts,
        partnerCourseHours: entry.partnerCourseHours,
        rows: [],
        isNotPassed: false,
      })
    }
    const group = map.get(key)!
    if (entry.homeSlotCourseIsvuCode == null && entry.homeSlotCourseGroupIsvuCode != null) {
      const existingRow = group.rows.find(
        (row) =>
          row[0]!.homeSlotCourseIsvuCode == null &&
          row[0]!.homeSlotCourseGroupIsvuCode === entry.homeSlotCourseGroupIsvuCode,
      )
      if (existingRow) {
        existingRow.push(entry)
        continue
      }
    }
    group.rows.push([entry])
  }
  for (const group of map.values()) {
    if (props.readonly) continue
    const liveStatus = props.editableGrades?.[group.key]?.enrollmentStatus
    group.isNotPassed = liveStatus !== undefined
      ? liveStatus === 'NotPassed'
      : group.rows.flat().some((r) => r.enrollmentStatus === 'NotPassed')
  }
  return Array.from(map.values())
})
</script>

<template>
  <div class="overflow-x-auto doc-table-wrap">
    <table
      style="border-collapse: collapse; width: 100%; min-width: 1200px; font-size: 11px; color: #000; table-layout: fixed;"
    >
      <colgroup>
        <col style="width: 70px" />
        <col style="width: 160px" />
        <col style="width: 90px" />
        <col style="width: 70px" />
        <col style="width: 40px" />
        <col style="width: 28px" />
        <col style="width: 70px" />
        <col style="width: 130px" />
        <col style="width: 55px" />
        <col style="width: 110px" />
        <col style="width: 38px" />
        <col style="width: 50px" />
        <col style="width: 60px" />
        <col style="width: 55px" />
        <col style="width: 55px" />
        <col style="width: 80px" />
      </colgroup>
      <thead>
        <tr>
          <th class="rec-th" style="min-width: 70px">{{ t('recognition.col.partnerCode') }}</th>
          <th class="rec-th" style="min-width: 160px">{{ t('recognition.col.partnerName') }}</th>
          <th class="rec-th" style="min-width: 90px">{{ t('recognition.col.enrollmentStatus') }}</th>
          <th class="rec-th" style="min-width: 70px">{{ t('recognition.col.partnerHours') }}</th>
          <th class="rec-th" style="min-width: 40px">{{ t('recognition.col.partnerEcts') }}</th>
          <th class="rec-th" style="min-width: 28px">{{ t('recognition.col.rbr') }}</th>
          <th class="rec-th" style="min-width: 70px">{{ t('recognition.col.recognizedAs') }}</th>
          <th class="rec-th" style="min-width: 130px">{{ t('recognition.col.homeSlotCourseName') }}</th>
          <th class="rec-th" style="min-width: 55px">{{ t('recognition.col.homeSlotCourseGroupIsvuCode') }}</th>
          <th class="rec-th" style="min-width: 110px">{{ t('recognition.col.homeSlotCourseGroupName') }}</th>
          <th class="rec-th" style="min-width: 38px">{{ t('recognition.col.homeSlotSemester') }}</th>
          <th class="rec-th" style="min-width: 50px">{{ t('recognition.col.awardedEcts') }}</th>
          <th class="rec-th" style="min-width: 60px">{{ t('recognition.col.originalGrade') }}</th>
          <th class="rec-th" style="min-width: 55px">{{ t('recognition.col.ectsGrade') }}</th>
          <th class="rec-th" style="min-width: 55px">{{ t('recognition.col.hrGrade') }}</th>
          <th class="rec-th" style="min-width: 80px">{{ t('recognition.col.examDate') }}</th>
        </tr>
      </thead>

      <tbody>
        <template v-for="group in courseGroups" :key="group.key">
          <tr v-for="(row, idx) in group.rows" :key="row[0]!.id">
            <td v-if="idx === 0" :rowspan="group.rows.length" class="rec-td rec-td--center rec-td--bold" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#fff' }">
              <span style="display: inline-flex; align-items: center; justify-content: center; gap: 3px;">
                <span>{{ group.partnerCourseCode }}</span>
                <CourseUrlLink v-if="group.partnerCourseUrl" block :url="group.partnerCourseUrl" :title="t('admin.institutions.courseUrl')" />
              </span>
            </td>
            <td v-if="idx === 0" :rowspan="group.rows.length" class="rec-td" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#fff' }">
              {{ group.partnerCourseName }}
              <div v-if="group.partnerCourseNameHr" class="rec-name-hr">{{ group.partnerCourseNameHr }}</div>
            </td>

            <!-- Enrollment status: dropdown when editable, blank when read-only -->
            <td v-if="idx === 0" :rowspan="group.rows.length" class="rec-td-grade" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#fff' }">
              <select
                v-if="canEdit(group)"
                v-model="editableGrades![group.key]!.enrollmentStatus"
                class="rec-input"
              >
                <option value="">{{ t('recognition.enrollment.none') }}</option>
                <option value="Passed">{{ t('recognition.enrollment.passed') }}</option>
                <option value="NotPassed">{{ t('recognition.enrollment.notPassed') }}</option>
              </select>
              <span v-else class="rec-locked">{{ shownGrade(group, 'enrollmentStatus') }}</span>
            </td>

            <td v-if="idx === 0" :rowspan="group.rows.length" class="rec-td rec-td--center" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#fff' }">
              {{ group.partnerCourseHours }}
            </td>
            <td v-if="idx === 0" :rowspan="group.rows.length" class="rec-td rec-td--center rec-td--bold" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#fff' }">
              {{ group.partnerCourseEcts }}
            </td>

            <td class="rec-td rec-td--center" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#fff' }">{{ idx + 1 }}</td>
            <td class="rec-td rec-td--center" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#fff' }">{{ row[0]!.homeSlotCourseIsvuCode }}</td>
            <td class="rec-td" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#fff' }">{{ row[0]!.homeSlotCourseName }}</td>
            <td class="rec-td rec-td--center" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#fff' }">{{ row[0]!.homeSlotCourseGroupIsvuCode }}</td>
            <td class="rec-td rec-td--center" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : row[0]!.homeSlotColor }">
              {{ row[0]!.homeSlotCourseGroupName || t('recognition.col.mandatoryCourse') }}
            </td>
            <td class="rec-td rec-td--center" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#fff' }">{{ row[0]!.homeSlotSemester }}</td>
            <td class="rec-td rec-td--center rec-td--bold" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : row[0]!.homeSlotColor }">
              {{ rowAwardedEcts(row) }}
            </td>

            <!-- Grade columns: inputs when editable, blank when read-only -->
            <td v-if="idx === 0" :rowspan="group.rows.length" class="rec-td-grade" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#ddd9c3' }">
              <input
                v-if="canEdit(group)"
                v-model="editableGrades![group.key]!.originalGrade"
                type="text"
                class="rec-input"
                placeholder="—"
              />
              <span v-else class="rec-locked">{{ shownGrade(group, 'originalGrade') }}</span>
            </td>
            <td v-if="idx === 0" :rowspan="group.rows.length" class="rec-td-grade" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#ddd9c3' }">
              <input
                v-if="canEdit(group)"
                v-model="editableGrades![group.key]!.ectsGrade"
                type="text"
                class="rec-input"
                placeholder="—"
              />
              <span v-else class="rec-locked">{{ shownGrade(group, 'ectsGrade') }}</span>
            </td>
            <td v-if="idx === 0" :rowspan="group.rows.length" class="rec-td-grade" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#ddd9c3' }">
              <input
                v-if="canEdit(group)"
                v-model="editableGrades![group.key]!.hrGrade"
                type="text"
                class="rec-input"
                placeholder="—"
              />
              <span v-else class="rec-locked">{{ shownGrade(group, 'hrGrade') }}</span>
            </td>
            <td v-if="idx === 0" :rowspan="group.rows.length" class="rec-td-grade" :style="{ background: group.isNotPassed ? NOT_PASSED_BG : '#ddd9c3' }">
              <div v-if="canEdit(group)" class="rec-date-wrap">
                <input
                  v-model="editableGrades![group.key]!.examDate"
                  type="date"
                  class="rec-input rec-input--date"
                />
                <span class="rec-date-overlay">{{ formatExamDateDisplay(editableGrades![group.key]!.examDate) }}</span>
              </div>
              <span v-else class="rec-locked">{{ shownGrade(group, 'examDate') }}</span>
            </td>
          </tr>
        </template>
      </tbody>
    </table>
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
  word-break: break-word;
}
.rec-td {
  border: 1px solid #aaa;
  padding: 3px 4px;
  vertical-align: middle;
  word-break: break-word;
}
.rec-td--center {
  text-align: center;
}
.rec-td--bold {
  font-weight: bold;
}
.rec-name-hr {
  font-size: 10px;
  color: #666;
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
  color: transparent;
}
.rec-date-wrap {
  position: relative;
}
.rec-date-overlay {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  pointer-events: none;
  font-family: Calibri, Arial, sans-serif;
  font-size: 11px;
  color: #000;
}
.rec-locked {
  display: block;
  text-align: center;
  font-size: 11px;
  color: #000;
}
</style>
