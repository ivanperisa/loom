export type RecognitionStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected'

export interface RecognitionEntryResponse {
  id: string
  slotMappingId: string
  foreignCourseCode: string
  foreignCourseNameEn: string
  awardedEcts: number
  courseSlotName: string
  enrollmentStatus: string | null
  originalGrade: string | null
  ectsGrade: string | null
  hrGrade: string | null
  examDate: string | null
}

export interface RecognitionResponse {
  id: string
  exchangeId: string
  status: RecognitionStatus
  entries: RecognitionEntryResponse[]
  createdAt: string
  updatedAt: string
}

export interface UpsertRecognitionEntryRequest {
  slotMappingId: string
  enrollmentStatus: string | null
  originalGrade: string | null
  ectsGrade: string | null
  hrGrade: string | null
  examDate: string | null
}
