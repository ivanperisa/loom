import { describe, expect, it } from 'vitest'
import { academicYearOptions, useExchangePeriod } from '@/composables/useExchangePeriod'

describe('useExchangePeriod', () => {
  it('offers the current and the next academic year (it starts in September)', () => {
    expect(academicYearOptions(new Date(2026, 8, 1))).toEqual(['2026/2027', '2027/2028'])
    expect(academicYearOptions(new Date(2026, 7, 31))).toEqual(['2025/2026', '2026/2027'])
  })

  it('keeps the study semester when the new type still allows it', () => {
    const period = useExchangePeriod({ semesterType: 'Both', studySemesters: [1, 2] })
    period.setSemesterType('Winter')
    expect(period.studySemesters.value).toEqual([1])   // the winter half of the pair

    const single = useExchangePeriod({ semesterType: 'Winter', studySemesters: [3] })
    single.setSemesterType('Both')
    expect(single.studySemesters.value).toEqual([])   // 3 is not a pair
    single.studySemesters.value = [3, 4]
    single.setSemesterType('Summer')
    expect(single.studySemesters.value).toEqual([4])
  })

  it('keeps a whole pair when switching to both', () => {
    const period = useExchangePeriod({ semesterType: 'Both', studySemesters: [3, 4] })
    period.setSemesterType('Both')
    expect(period.studySemesters.value).toEqual([3, 4])
  })

  it('does not offer a type that would drop semesters the LA already uses', () => {
    const period = useExchangePeriod({ semesterType: 'Both', studySemesters: [1, 2], lockedSemesters: [2] })
    expect(period.canSelectSemesterType('Winter')).toBe(false)
    expect(period.canSelectSemesterType('Summer')).toBe(true)
    expect(period.hasBlockedSemesterType.value).toBe(true)
    period.setSemesterType('Winter')
    expect(period.semesterType.value).toBe('Both')
  })

  it('names what is missing', () => {
    const period = useExchangePeriod({ academicYear: '2026/2027' })
    expect(period.validate()).toBe('createExchange.errors.studySemesterRequired')
    period.studySemesters.value = [1]
    expect(period.validate()).toBeNull()
  })
})
