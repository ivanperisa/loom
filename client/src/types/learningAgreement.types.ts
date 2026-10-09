import type { Schemas } from '@/api'

export type SlotMode = Schemas['SlotMode']
export type HomeSlotResponse = Schemas['HomeSlotResponse']
export type LearningAgreementEntryResponse = Schemas['LearningAgreementEntryResponse']
export type LearningAgreementResponse = Schemas['LearningAgreementResponse']
export type UpdateLearningAgreementStatusRequest = Schemas['UpdateLearningAgreementStatusRequest']
export type SaveLearningAgreementRequest = Schemas['SaveLearningAgreementRequest']
export type LearningAgreementEntryUpsertDto = Schemas['LearningAgreementEntryUpsertDto']
export type MappingExportCourse = Schemas['MappingExportCourse']
export type MappingExportEntry = Schemas['MappingExportEntry']
export type MappingExportInstitution = Schemas['MappingExportInstitution']
export type MappingExportHomeContext = Schemas['MappingExportHomeContext']
export type MappingExportDto = Schemas['MappingExportDto']
export type ImportContextWarning = Schemas['ImportContextWarning']
export type ImportRow = Schemas['ImportRow']
export type ImportSkip = Schemas['ImportSkip']
export type ImportPreviewResponse = Schemas['ImportPreviewResponse']
export type ImportResult = Schemas['ImportResult']
export type RestoreMissing = Schemas['RestoreMissing']
export type RestoreResult = Schemas['RestoreResult']

/** The draft being edited (client only): one slot's mode and the courses placed in it. */
export interface LocalSlotState {
  homeSlotId: number
  mode: SlotMode
  mappings: LocalSlotMapping[]
}

export interface LocalSlotMapping {
  /** The saved entry's id, or a new random id for a mapping placed in this draft. */
  localId: string
  partnerCourseId: number
  partnerCourseCode: string
  partnerCourseName: string
  partnerCourseNameHr: string | null
  partnerCourseUrl: string | null
  awardedEcts: number
  amendmentNumber?: number | null
}

export type ImportSkipReason = 'SlotNotInProfile' | 'CourseNotFound' | 'AmbiguousCourse' | 'InvalidMode' | 'Duplicate'
