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
  awardedEcts: number | null
  isDeleted: boolean
}

export interface LearningAgreementResponse {
  exchangeId: string
  status: DocumentStatus
  message: string | null
  slots: HomeSlotResponse[]
  entries: LearningAgreementEntryResponse[]
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
  awardedEcts: number
}

export interface LaSnapshotEntry {
  homeSlotId: number
  homeSlotLabel: string
  homeSlotSemester: number
  homeSlotEcts: number
  mode: SlotMode
  partnerCourseId: number | null
  partnerCourseCode: string | null
  partnerCourseName: string | null
  awardedEcts: number | null
}

export interface LaSnapshotEntryChange {
  before: LaSnapshotEntry
  after: LaSnapshotEntry
}

export interface LaSnapshotDiff {
  added: LaSnapshotEntry[]
  removed: LaSnapshotEntry[]
  modified: LaSnapshotEntryChange[]
}

export interface LaSnapshotSummary {
  id: number
  approvedAt: string
  approvedByName: string
  entryCount: number
  diff: LaSnapshotDiff | null
}

export type SnapshotType = 'Auto' | 'PreImport'

export interface SnapshotListItem {
  id: number
  type: SnapshotType
  createdAt: string
  createdByName: string
  entryCount: number
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
  version: number
  exportedAt: string
  exportedByName: string
  institution: MappingExportInstitution
  home: MappingExportHomeContext
  mappings: MappingExportEntry[]
}

export interface MappingImportSkip {
  homeSlotId: number
  homeSlotLabel: string
  reason: string
}

export interface MappingImportResult {
  appliedCount: number
  skipped: MappingImportSkip[]
}
