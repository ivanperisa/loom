import type { StudyProfileResponse, ForeignProgramResponse } from './institution.types'

export type ExchangeStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected'
export type ExchangeSemester = 'Winter' | 'Summer'
export type SlotMode = 'AtHome' | 'AtExchange' | 'AfterExchange'
export type CourseSlotCategory =
  | 'Mandatory'
  | 'CoreElective'
  | 'ProfileElective'
  | 'FreeElective'
  | 'Seminar'
  | 'ResearchSeminar'
  | 'Transversal'
  | 'Thesis'

export interface CreateExchangeRequest {
  studyProfileId: string
  foreignProgramId: string
  academicYear: string
  semesterType: ExchangeSemester
  studySemester: number
}

export interface ExchangeResponse {
  id: string
  studentId: string
  studentName: string
  studentJmbag: string | null
  homeInstitutionName: string
  studyProgramName: string
  studyProfile: StudyProfileResponse
  foreignProgram: ForeignProgramResponse
  coordinatorId: string | null
  coordinatorName: string | null
  mentor: string | null
  academicYear: string
  semesterType: ExchangeSemester
  studySemester: number
  status: ExchangeStatus
  coordinatorMessage: string | null
  createdAt: string
  updatedAt: string
}

export interface ExchangeSummaryResponse {
  id: string
  studentId: string
  studentName: string
  studentJmbag: string | null
  foreignInstitutionName: string
  foreignProgramName: string
  homeInstitutionName: string
  studyProgramName: string
  studyProfileName: string
  academicYear: string
  semesterType: ExchangeSemester
  status: ExchangeStatus
  recognitionStatus: string | null
}

export interface CourseSlotResponse {
  id: string
  semester: number
  slotPosition: number
  ects: number
  categoryCode: string
  categoryName: string
  categoryNameEn: string
  color: string
  courseCode: string | null
  courseName: string
  courseNameEn: string | null
}

export interface LearningAgreementEntryResponse {
  id: string
  courseSlotId: string
  mode: SlotMode
  foreignCourseId: string | null
  foreignCourseCode: string | null
  foreignCourseNameEn: string | null
  foreignCourseNameHr: string | null
  awardedEcts: number | null
}

export interface LearningAgreementResponse {
  exchangeId: string
  status: ExchangeStatus
  slots: CourseSlotResponse[]
  entries: LearningAgreementEntryResponse[]
}

export interface UpdateExchangeStatusRequest {
  status: ExchangeStatus
  message?: string | null
}

export interface UpdateCoordinatorMessageRequest {
  message: string | null
}

export type SnapshotPhase = 'LearningAgreement' | 'Recognition'

export interface ExchangeSnapshotResponse {
  id: string
  exchangeId: string
  phase: SnapshotPhase
  changedById: string
  changedByName: string
  createdAt: string
  data: LearningAgreementSnapshotData | null
}

export interface LearningAgreementSnapshotData {
  entries: LearningAgreementEntryResponse[]
}

export interface SaveLearningAgreementRequest {
  entries: LearningAgreementEntryUpsertDto[]
}

export interface LearningAgreementEntryUpsertDto {
  courseSlotId: string
  mode: SlotMode
  foreignCourseId?: string | null
  awardedEcts?: number | null
}

// Local working copy (localId used only as :key, never sent to server)
export interface LocalSlotState {
  courseSlotId: string
  mode: SlotMode
  mappings: LocalSlotMapping[]
}

export interface LocalSlotMapping {
  localId: string
  foreignCourseId: string
  foreignCourseCode: string
  foreignCourseNameEn: string
  foreignCourseNameHr: string | null
  awardedEcts: number
}
