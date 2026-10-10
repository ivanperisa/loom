import { computed, ref, toValue, type MaybeRefOrGetter } from 'vue'
import { exchangeSemester } from '@/utils/exchangeSemester'
import type { ExchangeSemester } from '@/types/exchange.types'

/** Study semesters a semester type allows: winter → 1, 3; summer → 2, 4; both → pairs (1+2, 3+4). */
const SEMESTERS_BY_TYPE: Record<ExchangeSemester, number[]> = {
  Winter: [1, 3],
  Summer: [2, 4],
  Both: [1, 2, 3, 4],
}

/** The current academic year and the next one ("2025/2026" from September). */
export function academicYearOptions(now = new Date()): string[] {
  const start = now.getMonth() + 1 >= 9 ? now.getFullYear() : now.getFullYear() - 1
  return [`${start}/${start + 1}`, `${start + 1}/${start + 2}`]
}

export interface ExchangePeriodInit {
  academicYear?: string
  semesterType?: ExchangeSemester
  studySemesters?: number[]
  /** Study semesters that already have courses in the LA: a semester type that drops them is not offered. */
  lockedSemesters?: MaybeRefOrGetter<number[]>
}

/** Academic year, semester type and study semesters of an exchange, shared by create and edit. */
export function useExchangePeriod(init: ExchangePeriodInit = {}) {
  const academicYear = ref(init.academicYear ?? academicYearOptions()[0]!)
  const semesterType = ref<ExchangeSemester>(init.semesterType ?? exchangeSemester.Winter)
  const studySemesters = ref<number[]>([...(init.studySemesters ?? [])])

  const academicYearSelectOptions = computed(() => {
    const years = academicYearOptions()
    if (!years.includes(academicYear.value)) years.unshift(academicYear.value)
    return years.map((year) => ({ value: year, label: year }))
  })

  function canSelectSemesterType(type: ExchangeSemester): boolean {
    const locked = toValue(init.lockedSemesters) ?? []
    return locked.every((s) => SEMESTERS_BY_TYPE[type].includes(s))
  }

  const hasBlockedSemesterType = computed(() =>
    (Object.keys(SEMESTERS_BY_TYPE) as ExchangeSemester[]).some((type) => !canSelectSemesterType(type)),
  )

  /** Switching type keeps the selection only while it is still a whole option (one semester, or a pair for "both"). */
  function setSemesterType(type: ExchangeSemester) {
    if (!canSelectSemesterType(type)) return
    semesterType.value = type
    const kept = studySemesters.value.filter((s) => SEMESTERS_BY_TYPE[type].includes(s)).sort((a, b) => a - b)
    const whole = type === exchangeSemester.Both
      ? kept.length === 2 && kept[1] === kept[0]! + 1 && kept[0]! % 2 === 1
      : kept.length === 1
    studySemesters.value = whole ? kept : []
  }

  /** The key of what is missing, or null when the period is complete. */
  function validate(): string | null {
    if (!academicYear.value.trim()) return 'createExchange.errors.academicYearRequired'
    if (studySemesters.value.length === 0) return 'createExchange.errors.studySemesterRequired'
    return null
  }

  return {
    academicYear,
    semesterType,
    studySemesters,
    academicYearSelectOptions,
    canSelectSemesterType,
    hasBlockedSemesterType,
    setSemesterType,
    validate,
  }
}
