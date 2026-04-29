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
}

export interface CourseSlotResponse {
  id: string
  semester: number
  colStart: number
  ects: number
  categoryCode: string
  categoryName: string
  categoryNameEn: string
  color: string
  courseCode: string | null
  courseName: string
  courseNameEn: string | null
}

export interface SlotMappingResponse {
  id: string
  foreignCourseId: string
  foreignCourseCode: string
  foreignCourseNameEn: string
  foreignCourseNameHr: string | null
  awardedEcts: number
}

export interface SlotStateResponse {
  id: string
  courseSlotId: string
  mode: SlotMode
  mappings: SlotMappingResponse[]
}

export interface LearningAgreementResponse {
  exchangeId: string
  status: ExchangeStatus
  slots: CourseSlotResponse[]
  slotStates: SlotStateResponse[]
}

export interface SetSlotModeRequest {
  courseSlotId: string
  mode: SlotMode
}

export interface AddSlotMappingRequest {
  courseSlotId: string
  foreignCourseId: string
  awardedEcts: number
}

export interface RemoveSlotMappingRequest {
  slotMappingId: string
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
  phase: SnapshotPhase
  changedByName: string
  createdAt: string
}
