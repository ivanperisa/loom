import { beforeEach, describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useLearningAgreementDraftStore } from '@/stores/learningAgreementDraft.store'
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

const agreement = (entries: LearningAgreementResponse['entries']) => ({ entries }) as LearningAgreementResponse

describe('Learning agreement draft', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  it('loads the saved learning agreement (without removed entries) and sends it back unchanged', () => {
    const draft = useLearningAgreementDraftStore()
    draft.load('ex', agreement([course(1, 5, 10, 100), { ...course(2, 5, 11, 101), isDeleted: true }]))
    expect(draft.isDirty).toBe(false)
    expect(draft.toRequest().entries).toEqual([{ homeSlotId: 10, mode: 'AtExchange', partnerCourseId: 1, awardedEcts: 5 }])
  })

  it('moves part of a course to another slot and merges with what is there', () => {
    const draft = useLearningAgreementDraftStore()
    draft.load('ex', agreement([course(1, 6, 10, 100), course(1, 1, 11, 101)]))
    draft.moveMapping(10, 11, '100', 2)
    expect(draft.toRequest().entries).toEqual([
      { homeSlotId: 10, mode: 'AtExchange', partnerCourseId: 1, awardedEcts: 4 },
      { homeSlotId: 11, mode: 'AtExchange', partnerCourseId: 1, awardedEcts: 3 },
    ])
    expect(draft.isDirty).toBe(true)
  })

  it('keeps unsaved changes when the saved learning agreement comes again, but not across exchanges', () => {
    const draft = useLearningAgreementDraftStore()
    draft.load('ex', agreement([]))
    draft.setSlotMode(10, 'AtHome')
    expect(draft.canReload('ex')).toBe(false)
    expect(draft.canReload('other')).toBe(true)
  })

  it('remembers staged courses per exchange in this browser', () => {
    const draft = useLearningAgreementDraftStore()
    draft.load('ex', agreement([]))
    draft.stagePartnerCourse(42)
    setActivePinia(createPinia())
    const again = useLearningAgreementDraftStore()
    again.load('ex', agreement([]))
    expect([...again.stagedPartnerCourseIds]).toEqual([42])
  })
})
