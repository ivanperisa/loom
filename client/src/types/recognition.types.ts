import type { DocumentStatus } from './exchange.types'

export interface RecognitionEntryResponse {
  id: string
  learningAgreementEntryId: string
  partnerCourseCode: string
  partnerCourseName: string
  partnerCourseNameHr: string | null
  partnerCourseHours: string | null
  partnerCourseEcts: number
  homeSlotCourseIsvuCode: number | null
  homeSlotCourseName: string
  homeSlotCourseGroupIsvuCode: number | null
  homeSlotCourseGroupName: string
  homeSlotColor: string
  homeSlotSemester: number
  awardedEcts: number
  recognizedAsCourseId: string | null
  recognizedAsCourseName: string | null
  enrollmentStatus: string | null
  originalGrade: string | null
  ectsGrade: string | null
  hrGrade: string | null
  examDate: string | null
  isRecognized: boolean | null
}

export interface RecognitionResponse {
  id: string
  exchangeId: string
  status: DocumentStatus
  entries: RecognitionEntryResponse[]
  createdAt: string
  updatedAt: string
}

export interface UpsertRecognitionEntryRequest {
  learningAgreementEntryId: string
  enrollmentStatus: string | null
  originalGrade: string | null
  ectsGrade: string | null
  hrGrade: string | null
  examDate: string | null
}

export interface SaveRecognitionRequest {
  entries: UpsertRecognitionEntryRequest[]
}

export interface UpdateRecognitionStatusRequest {
  status: DocumentStatus
}
