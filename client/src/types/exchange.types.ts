import type { InstitutionResponse } from './institution.types'

export interface ExchangeResponse {
  id: string
  academicYear: string
  semester: ExchangeSemester
  durationMonths?: number
  mentor?: string
  status: ExchangeStatus
  foreignInstitution: InstitutionResponse
  courses: ExchangeCourseResponse[]
}

export interface ExchangeCourseResponse {
  id: string
  code?: string
  name: string
  nameEn: string
  ects?: number
  status: ExchangeCourseStatus
  lecturesHours?: number
  auditoryHours?: number
  labHours?: number
  originalGrade?: string
  ectsGrade?: string
  examDate?: string
  mappings: CourseMappingResponse[]
}

export interface CourseMappingResponse {
  id: string
  courseId: string
  courseName: string
  courseCode?: string
  awardedEcts?: number
  convertedGrade?: string
  status: MappingStatus
  coordinatorNote?: string
}

export interface MappingHistoryResponse {
  id: string
  changedByName: string
  createdAt: string
  exchangeCourseName: string
  exchangeCourseCode?: string
  snapshot: MappingSnapshotResponse
}

export interface StudentExchangeSummaryResponse {
  exchangeId: string
  studentId: string
  studentName: string
  studentEmail: string
  studentJmbag?: string
  academicYear: string
  semester: ExchangeSemester
  status: ExchangeStatus
  foreignInstitutionName: string
}

export interface MappingSnapshotResponse {
  courseId: string
  courseName: string
  courseCode?: string
  awardedEcts?: number
  convertedGrade?: string
  status: MappingStatus
  coordinatorNote?: string
}

export interface CreateExchangeRequest {
  foreignInstitutionId: string
  academicYear: string
  semester: ExchangeSemester
  durationMonths?: number
  mentor?: string
}

export interface UpsertExchangeCourseRequest {
  code?: string
  name: string
  nameEn: string
  ects?: number
  status: ExchangeCourseStatus
  lecturesHours?: number
  auditoryHours?: number
  labHours?: number
}

export interface UpdateGradesRequest {
  originalGrade?: string
  ectsGrade?: string
  examDate?: string
}

export interface ProposeMappingRequest {
  mappings: { courseId: string; awardedEcts?: number }[]
}

export interface ReviewMappingRequest {
  status: 'Approved' | 'Rejected'
  coordinatorNote?: string
  awardedEcts?: number
  convertedGrade?: string
}

export type ExchangeStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected' | 'Completed'
export type ExchangeSemester = 'Winter' | 'Summer'
export type ExchangeCourseStatus = 'OriginallyEnrolled' | 'Additional'
export type MappingStatus = 'Pending' | 'Approved' | 'Rejected'

// Coordinator dashboard
export interface CoordinatorStudentSummaryResponse {
  exchangeId: string
  studentName: string
  studentEmail: string
  studentJmbag?: string
  academicYear: string
  semester: ExchangeSemester
  status: ExchangeStatus
  foreignInstitutionName: string
  foreignInstitutionCountry?: string
  totalCourses: number
  pendingMappings: number
  approvedMappings: number
}

// Mapping board
export interface MappingBoardResponse {
  ferCourseGroups: FerCourseGroupResponse[]
  exchangeCourses: ExchangeCourseWithMappingsResponse[]
}

export interface FerCourseGroupResponse {
  type: string
  courses: FerCourseResponse[]
}

export interface FerCourseResponse {
  id: string
  code?: string
  name: string
  nameEn: string
  ects: number
  type: string
}

export interface ExchangeCourseWithMappingsResponse {
  id: string
  code?: string
  name: string
  nameEn: string
  ects?: number
  status: ExchangeCourseStatus
  mappings: MappingRowResponse[]
}

export interface MappingRowResponse {
  id: string
  ferCourseId: string
  ferCourseName: string
  ferCourseCode?: string
  awardedEcts?: number
  convertedGrade?: string
  status: MappingStatus
  coordinatorNote?: string
}

export interface ProposeBoardMappingRequest {
  courses: {
    exchangeCourseId: string
    mappings: {
      ferCourseId: string
      awardedEcts?: number
      convertedGrade?: string
      coordinatorNote?: string
    }[]
  }[]
}
