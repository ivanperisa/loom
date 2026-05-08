import type { documentStatus } from '@/utils/documentStatus'
import type { slotMode } from '@/utils/slotMode'
import type { exchangeSemester } from '@/utils/exchangeSemester'
import type { StudyProfileResponse, ForeignProgramResponse } from './institution.types'

export type DocumentStatus = (typeof documentStatus)[keyof typeof documentStatus]
export type ExchangeSemester = (typeof exchangeSemester)[keyof typeof exchangeSemester]
export type SlotMode = (typeof slotMode)[keyof typeof slotMode]
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
  learningAgreementStatus: DocumentStatus
  recognitionStatus: DocumentStatus
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
  isDeleted: boolean
}

export interface LearningAgreementResponse {
  exchangeId: string
  status: DocumentStatus
  slots: CourseSlotResponse[]
  entries: LearningAgreementEntryResponse[]
}

export interface UpdateLearningAgreementStatusRequest {
  status: DocumentStatus
  message?: string | null
}

export interface UpdateCoordinatorMessageRequest {
  message: string | null
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
