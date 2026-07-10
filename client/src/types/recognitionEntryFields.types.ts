export interface RecognitionEntryFields {
  id: string
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
  enrollmentStatus: string | null
  originalGrade: string | null
  ectsGrade: string | null
  hrGrade: string | null
  examDate: string | null
}
