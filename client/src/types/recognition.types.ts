export type RecognitionStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected'

export interface RecognitionEntryResponse {
  id: string
  slotMappingId: string
  foreignCourseCode: string
  foreignCourseNameEn: string
  foreignCourseNameHr: string | null
  foreignCourseEcts: number
  foreignCourseHours: string | null
  awardedEcts: number
  courseSlotName: string
  courseSlotCode: string | null
  courseSlotCategoryCode: string
  courseSlotCategoryName: string
  courseSlotColor: string
  courseSlotSemester: number
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

export interface SaveRecognitionRequest {
  entries: UpsertRecognitionEntryRequest[]
}
