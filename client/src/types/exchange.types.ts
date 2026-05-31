import type { documentStatus } from '@/utils/documentStatus'
import type { slotMode } from '@/utils/slotMode'
import type { exchangeSemester } from '@/utils/exchangeSemester'
import type { HomeProfileResponse, PartnerProgramResponse } from './institution.types'

export type DocumentStatus = (typeof documentStatus)[keyof typeof documentStatus]
export type ExchangeSemester = (typeof exchangeSemester)[keyof typeof exchangeSemester]
export type SlotMode = (typeof slotMode)[keyof typeof slotMode]

export interface CreateExchangeRequest {
  homeProfileId: string
  partnerProgramId: string
  academicYear: string
  semesterType: ExchangeSemester
  studySemesters: number[]
}

export interface ExchangeResponse {
  id: string
  guid: string
  studentId: string
  studentName: string
  studentJmbag: string | null
  homeInstitutionName: string
  homeProgramName: string
  homeProfile: HomeProfileResponse
  partnerProgram: PartnerProgramResponse
  coordinatorId: string | null
  coordinatorName: string | null
  mentor: string | null
  academicYear: string
  semesterType: ExchangeSemester
  studySemesters: number[]
  coordinatorMessage: string | null
  createdAt: string
  updatedAt: string
}

export interface ExchangeSummaryResponse {
  id: string
  guid: string
  studentId: string
  studentName: string
  studentJmbag: string | null
  partnerInstitutionName: string
  partnerProgramName: string
  homeInstitutionName: string
  homeProgramName: string
  homeProfileName: string
  academicYear: string
  semesterType: ExchangeSemester
  learningAgreementStatus: DocumentStatus
  recognitionStatus: DocumentStatus
}

export interface HomeSlotResponse {
  id: string
  semester: number
  slotPosition: number
  ects: number
  courseTypeId: string
  courseTypeName: string
  courseTypeNameEn: string
  color: string
  // course-based (null when courseGroupId is set)
  courseIsvuCode: number | null
  courseName: string | null
  courseNameEn: string | null
  // course-group-based (null when courseId is set)
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
  partnerCourseNameEn: string | null
  partnerCourseNameHr: string | null
  awardedEcts: number | null
  isDeleted: boolean
}

export interface LearningAgreementResponse {
  exchangeId: string
  status: DocumentStatus
  slots: HomeSlotResponse[]
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
  homeSlotId: string
  mode: SlotMode
  partnerCourseId?: string | null
  awardedEcts?: number | null
}

// Local working copy (localId used only as :key, never sent to server)
export interface LocalSlotState {
  homeSlotId: string
  mode: SlotMode
  mappings: LocalSlotMapping[]
}

export interface LocalSlotMapping {
  localId: string
  partnerCourseId: string
  partnerCourseCode: string
  partnerCourseNameEn: string
  partnerCourseNameHr: string | null
  awardedEcts: number
}
