import type { ExchangeSummaryResponse } from './exchange.types'

export interface CoordinatorStudentResponse {
  id: string
  name: string
  jmbag: string | null
  institutionName: string | null
  isPlaceholder: boolean
  institutionId: string | null
  isMyStudent: boolean
  /** This coordinator's exchanges of the student (matching the list filters), newest first. */
  exchanges: ExchangeSummaryResponse[]
}

export interface StudentFiltersResponse {
  academicYears: string[]
  partnerInstitutions: string[]
}

export interface CreatePlaceholderStudentRequest {
  name: string
  jmbag: string
  institutionId: string
}

export type UpdateStudentRequest = CreatePlaceholderStudentRequest

export interface CoordinatorOption {
  id: string
  name: string
}
