import type { documentStatus } from '@/utils/documentStatus'
import type { exchangeSemester } from '@/utils/exchangeSemester'
import type { HomeProfileResponse, PartnerProgramResponse } from './institution.types'

export type DocumentStatus = (typeof documentStatus)[keyof typeof documentStatus]
export type ExchangeSemester = (typeof exchangeSemester)[keyof typeof exchangeSemester]

export interface CreateExchangeRequest {
  homeProfileId: string
  partnerProgramId: string
  academicYear: string
  semesterType: ExchangeSemester
  studySemesters: number[]
  coordinatorId?: string | null
  targetStudentId?: string | null
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

export interface UpdateCoordinatorMessageRequest {
  message: string | null
}
