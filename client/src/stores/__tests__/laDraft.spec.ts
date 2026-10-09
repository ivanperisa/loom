import { beforeEach, describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useLaDraftStore } from '@/stores/laDraft.store'
import type { LearningAgreementResponse } from '@/types/learningAgreement.types'

const course = (id: number, ects: number, slot: number, entryId: number) => ({
  id: entryId,
  homeSlotId: slot,
  mode: 'AtExchange' as const,
  partnerCourseId: id,
  partnerCourseCode: `C${id}`,
  partnerCourseName: `Course ${id}`,
  partnerCourseNameHr: null,
  partnerCourseUrl: null,
  awardedEcts: ects,
  isDeleted: false,
  amendmentNumber: null,
})

const la = (entries: LearningAgreementResponse['entries']) => ({ entries }) as LearningAgreementResponse

describe('LA draft', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  it('loads the saved LA (without removed entries) and sends it back unchanged', () => {
    const draft = useLaDraftStore()
    draft.load('ex', la([course(1, 5, 10, 100), { ...course(2, 5, 11, 101), isDeleted: true }]))
    expect(draft.isDirty).toBe(false)
    expect(draft.toRequest().entries).toEqual([{ homeSlotId: 10, mode: 'AtExchange', partnerCourseId: 1, awardedEcts: 5 }])
  })

  it('moves part of a course to another slot and merges with what is there', () => {
    const draft = useLaDraftStore()
    draft.load('ex', la([course(1, 6, 10, 100), course(1, 1, 11, 101)]))
    draft.moveMapping(10, 11, '100', 2)
    expect(draft.toRequest().entries).toEqual([
      { homeSlotId: 10, mode: 'AtExchange', partnerCourseId: 1, awardedEcts: 4 },
      { homeSlotId: 11, mode: 'AtExchange', partnerCourseId: 1, awardedEcts: 3 },
    ])
    expect(draft.isDirty).toBe(true)
  })

  it('keeps unsaved changes when the saved LA comes again, but not across exchanges', () => {
    const draft = useLaDraftStore()
    draft.load('ex', la([]))
    draft.setSlotMode(10, 'AtHome')
    expect(draft.canReload('ex')).toBe(false)
    expect(draft.canReload('other')).toBe(true)
  })

  it('remembers staged courses per exchange in this browser', () => {
    const draft = useLaDraftStore()
    draft.load('ex', la([]))
    draft.stagePartnerCourse(42)
    setActivePinia(createPinia())
    const again = useLaDraftStore()
    again.load('ex', la([]))
    expect([...again.stagedPartnerCourseIds]).toEqual([42])
  })
})
