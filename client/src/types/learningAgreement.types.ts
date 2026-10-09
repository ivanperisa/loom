import type { slotMode } from '@/utils/slotMode'
import type { DocumentStatus } from './exchange.types'

export type SlotMode = (typeof slotMode)[keyof typeof slotMode]

export interface HomeSlotResponse {
  id: string
  semester: number
  slotPosition: number
  ects: number
  courseTypeId: string
  courseTypeName: string
  courseTypeNameEn: string | null
  color: string
  courseIsvuCode: number | null
  courseName: string | null
  courseNameEn: string | null
  courseGroupIsvuCode: number | null
  courseGroupName: string | null
  courseGroupNameEn: string | null
}

export interface LearningAgreementEntryResponse {
  id: string
  homeSlotId: string
  mode: SlotMode
  partnerCourseId: string | null
  partnerCourseCode: string | null
  partnerCourseName: string | null
  partnerCourseNameHr: string | null
  partnerCourseUrl: string | null
  awardedEcts: number | null
  isDeleted: boolean
  amendmentNumber: number | null
}

export interface LearningAgreementResponse {
  exchangeId: string
  status: DocumentStatus
  message: string | null
  slots: HomeSlotResponse[]
  entries: LearningAgreementEntryResponse[]
  lastModifiedAt: string | null
  lastModifiedByName: string | null
  signedAt: string | null
  signedByName: string | null
  /** Number of approved versions (1 = original only, 2 = up to A1, …). */
  signedCount: number
  /** "Start final recognition" was pressed: the LA can never change again. */
  isConcluded: boolean
  concludedAt: string | null
  concludedByName: string | null
}

export interface UpdateLearningAgreementStatusRequest {
  status: DocumentStatus
  message?: string | null
}

export interface SaveLearningAgreementRequest {
  entries: LearningAgreementEntryUpsertDto[]
}

export interface LearningAgreementEntryUpsertDto {
  homeSlotId: string
  mode: SlotMode
  partnerCourseId?: string | null
  awardedEcts?: number | null
}

export interface LocalSlotState {
  homeSlotId: string
  mode: SlotMode
  mappings: LocalSlotMapping[]
}

export interface LocalSlotMapping {
  localId: string
  partnerCourseId: string
  partnerCourseCode: string
  partnerCourseName: string
  partnerCourseNameHr: string | null
  partnerCourseUrl: string | null
  awardedEcts: number
  amendmentNumber?: number | null
}

export interface MappingExportCourse {
  id: number
  code: string
  name: string
  ects: number
}

export interface MappingExportEntry {
  homeSlotId: number
  homeSlotLabel: string
  homeSlotSemester: number
  homeSlotEcts: number
  mode: SlotMode
  partnerCourse: MappingExportCourse | null
  awardedEcts: number | null
}

export interface MappingExportInstitution {
  id: number
  name: string
  erasmusCode: string | null
}

export interface MappingExportHomeContext {
  profileId: number
  profileName: string
  programName: string
  institutionName: string
}

export interface MappingExportDto {
  format?: string
  version: number
  exportedAt: string
  exportedByName: string
  institution: MappingExportInstitution
  home: MappingExportHomeContext
  mappings: MappingExportEntry[]
}

export interface ImportContextWarning {
  field: 'partnerInstitution' | 'homeProfile'
  fromFile: string
  inExchange: string
}

export interface ImportRow {
  homeSlotId: number
  homeSlotLabel: string
  mode: SlotMode
  partnerCourseId: number | null
  partnerCourseCode: string | null
  partnerCourseName: string | null
  awardedEcts: number | null
  previousEcts: number | null
  previousMode: SlotMode | null
}

export type ImportSkipReason = 'SlotNotInProfile' | 'CourseNotFound' | 'AmbiguousCourse' | 'InvalidMode' | 'Duplicate'

export interface ImportSkip {
  homeSlotId: number
  homeSlotLabel: string
  partnerCourseCode: string | null
  reason: ImportSkipReason
}

/** What importing a file would do. Import replaces the draft: rows missing from the file are removed. */
export interface ImportPreviewResponse {
  canApply: boolean
  blockingCode: string | null
  blockingMessage: string | null
  blockingParams: Record<string, unknown> | null
  contextWarnings: ImportContextWarning[]
  added: ImportRow[]
  removed: ImportRow[]
  changed: ImportRow[]
  unchanged: number
  skipped: ImportSkip[]
}

export interface ImportResult {
  added: number
  removed: number
  changed: number
  skipped: ImportSkip[]
}

export interface RestoreMissing {
  homeSlotId: number
  homeSlotLabel: string
  partnerCourseCode: string | null
  partnerCourseName: string | null
  reason: 'SlotNotInProfile' | 'CourseNotFound'
}

export interface RestoreResult {
  entries: number
  missing: RestoreMissing[]
}
