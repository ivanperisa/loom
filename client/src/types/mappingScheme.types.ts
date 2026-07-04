import type { RecognitionEntryFields } from './recognitionEntryFields.types'

export interface MappingSchemeEntryResponse extends RecognitionEntryFields {
  homeSlotId: string
  partnerCourseId: string | null
}

export interface MappingSchemeResponse {
  exchangeId: string
  entries: MappingSchemeEntryResponse[]
}

export interface SaveMappingSchemeEntryRequest {
  id: number
  homeSlotId: number
  partnerCourseId: number | null
  awardedEcts: number
  enrollmentStatus: string | null
  originalGrade: string | null
  ectsGrade: string | null
  hrGrade: string | null
  examDate: string | null
}

export interface SaveMappingSchemeRequest {
  entries: SaveMappingSchemeEntryRequest[]
}
