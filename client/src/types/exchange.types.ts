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
  nameHr?: string
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
  nameHr?: string
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
