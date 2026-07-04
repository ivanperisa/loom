<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

// Shared display shape — satisfied by both RecognitionEntryResponse and MappingSchemeEntryResponse.
interface RecognitionRow {
  id: string
  partnerCourseCode: string
  partnerCourseName: string
  partnerCourseNameHr: string | null
  partnerCourseHours: string | null
  partnerCourseEcts: number
  homeSlotCourseIsvuCode: number | null
  homeSlotCourseName: string
  homeSlotCourseGroupIsvuCode: number | null
  homeSlotCourseGroupName: string
  homeSlotColor: string
  homeSlotSemester: number
  awardedEcts: number
}

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
  entries: RecognitionRow[]
}

const props = defineProps<{
  entries: RecognitionRow[]
  readonly: boolean
  editableGrades?: Record<string, GradeData>
}>()

const { t } = useI18n()

const courseGroups = computed<CourseGroup[]>(() => {
  const map = new Map<string, CourseGroup>()
  for (const entry of props.entries) {
    const code = entry.partnerCourseCode
    if (!map.has(code)) {
      map.set(code, {
        partnerCourseCode: code,
        partnerCourseName: entry.partnerCourseName,
        partnerCourseNameHr: entry.partnerCourseNameHr,
        partnerCourseEcts: entry.partnerCourseEcts,
        partnerCourseHours: entry.partnerCourseHours,
        entries: [],
      })
    }
    map.get(code)!.entries.push(entry)
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
        <template v-for="group in courseGroups" :key="group.partnerCourseCode">
          <tr v-for="(entry, idx) in group.entries" :key="entry.id">
            <td v-if="idx === 0" :rowspan="group.entries.length" class="rec-td rec-td--center rec-td--bold" style="background: #fff">
              {{ group.partnerCourseCode }}
            </td>
            <td v-if="idx === 0" :rowspan="group.entries.length" class="rec-td" style="background: #fff">
              {{ group.partnerCourseName }}
            </td>

            <!-- Enrollment status: dropdown when editable, blank when read-only -->
            <td v-if="idx === 0" :rowspan="group.entries.length" class="rec-td-grade" style="background: #fff">
              <select
                v-if="!readonly && editableGrades?.[group.partnerCourseCode]"
                v-model="editableGrades[group.partnerCourseCode]!.enrollmentStatus"
                class="rec-input"
              >
                <option value="">{{ t('recognition.enrollment.none') }}</option>
                <option value="Passed">{{ t('recognition.enrollment.passed') }}</option>
                <option value="NotPassed">{{ t('recognition.enrollment.notPassed') }}</option>
              </select>
            </td>

            <td v-if="idx === 0" :rowspan="group.entries.length" class="rec-td rec-td--center" style="background: #fff">
              {{ group.partnerCourseHours ?? '—' }}
            </td>
            <td v-if="idx === 0" :rowspan="group.entries.length" class="rec-td rec-td--center rec-td--bold" style="background: #fff">
              {{ group.partnerCourseEcts }}
            </td>

            <td class="rec-td rec-td--center" style="background: #fff">{{ idx + 1 }}</td>
            <td class="rec-td rec-td--center" style="background: #fff">{{ entry.homeSlotCourseIsvuCode }}</td>
            <td class="rec-td" style="background: #fff">{{ entry.homeSlotCourseName }}</td>
            <td class="rec-td rec-td--center" style="background: #fff">{{ entry.homeSlotCourseGroupIsvuCode ?? '—' }}</td>
            <td class="rec-td rec-td--center" :style="{ background: entry.homeSlotColor }">
              {{ entry.homeSlotCourseGroupName || t('recognition.col.mandatoryCourse') }}
            </td>
            <td class="rec-td rec-td--center" style="background: #fff">{{ entry.homeSlotSemester }}</td>
            <td class="rec-td rec-td--center rec-td--bold" :style="{ background: entry.homeSlotColor }">
              {{ entry.awardedEcts }}
            </td>

            <!-- Grade columns: inputs when editable, blank when read-only -->
            <td v-if="idx === 0" :rowspan="group.entries.length" class="rec-td-grade" style="background: #ddd9c3">
              <input
                v-if="!readonly && editableGrades?.[group.partnerCourseCode]"
                v-model="editableGrades[group.partnerCourseCode]!.originalGrade"
                type="text"
                class="rec-input"
                placeholder="—"
              />
            </td>
            <td v-if="idx === 0" :rowspan="group.entries.length" class="rec-td-grade" style="background: #ddd9c3">
              <input
                v-if="!readonly && editableGrades?.[group.partnerCourseCode]"
                v-model="editableGrades[group.partnerCourseCode]!.ectsGrade"
                type="text"
                class="rec-input"
                placeholder="—"
              />
            </td>
            <td v-if="idx === 0" :rowspan="group.entries.length" class="rec-td-grade" style="background: #ddd9c3">
              <input
                v-if="!readonly && editableGrades?.[group.partnerCourseCode]"
                v-model="editableGrades[group.partnerCourseCode]!.hrGrade"
                type="text"
                class="rec-input"
                placeholder="—"
              />
            </td>
            <td v-if="idx === 0" :rowspan="group.entries.length" class="rec-td-grade" style="background: #ddd9c3">
              <input
                v-if="!readonly && editableGrades?.[group.partnerCourseCode]"
                v-model="editableGrades[group.partnerCourseCode]!.examDate"
                type="date"
                class="rec-input rec-input--date"
              />
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
