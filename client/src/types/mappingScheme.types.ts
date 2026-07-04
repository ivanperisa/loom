export interface MappingSchemeEntryResponse {
  id: string
  homeSlotId: string
  partnerCourseId: string | null
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
