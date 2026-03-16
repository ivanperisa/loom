import { api } from '@/services/api'
import type {
  ExchangeResponse,
  ExchangeCourseResponse,
  CourseMappingResponse,
  MappingHistoryResponse,
  StudentExchangeSummaryResponse,
  CreateExchangeRequest,
  UpsertExchangeCourseRequest,
  UpdateGradesRequest,
  ProposeMappingRequest,
  ReviewMappingRequest
} from '@/types/exchange.types'
import type { InstitutionResponse } from '@/types/institution.types'
import type { CourseResponse } from '@/types/course.types'

export const exchangeService = {
  getMyExchange: () =>
    api.get<ExchangeResponse>('/exchange/my'),

  createExchange: (data: CreateExchangeRequest) =>
    api.post<ExchangeResponse>('/exchange', data),

  deleteExchange: (exchangeId: string) =>
    api.delete(`/exchange/${exchangeId}`),

  addCourse: (exchangeId: string, data: UpsertExchangeCourseRequest) =>
    api.post<ExchangeCourseResponse>(`/exchange/${exchangeId}/courses`, data),

  updateCourse: (exchangeId: string, courseId: string, data: UpsertExchangeCourseRequest) =>
    api.put<ExchangeCourseResponse>(`/exchange/${exchangeId}/courses/${courseId}`, data),

  removeCourse: (exchangeId: string, courseId: string) =>
    api.delete(`/exchange/${exchangeId}/courses/${courseId}`),

  updateGrades: (exchangeId: string, courseId: string, data: UpdateGradesRequest) =>
    api.put<ExchangeCourseResponse>(`/exchange/${exchangeId}/courses/${courseId}/grades`, data),

  proposeMapping: (exchangeId: string, courseId: string, data: ProposeMappingRequest) =>
    api.post<ExchangeCourseResponse>(`/exchange/${exchangeId}/courses/${courseId}/mappings`, data),

  reviewMapping: (exchangeId: string, courseId: string, mappingId: string, data: ReviewMappingRequest) =>
    api.put<CourseMappingResponse>(`/exchange/${exchangeId}/courses/${courseId}/mappings/${mappingId}`, data),

  deleteMapping: (exchangeId: string, courseId: string, mappingId: string) =>
    api.delete(`/exchange/${exchangeId}/courses/${courseId}/mappings/${mappingId}`),

  getMyHistory: () =>
    api.get<MappingHistoryResponse[]>('/exchange/my/history'),

  getExchangeHistory: (exchangeId: string) =>
    api.get<MappingHistoryResponse[]>(`/exchange/${exchangeId}/history`),

  getStudentsWithExchange: () =>
    api.get<StudentExchangeSummaryResponse[]>('/exchange/students'),

  retract: (exchangeId: string) =>
    api.post<ExchangeResponse>(`/exchange/${exchangeId}/retract`),

  submitForReview: (exchangeId: string) =>
    api.post<ExchangeResponse>(`/exchange/${exchangeId}/submit`),

  getForeignInstitutions: () =>
    api.get<InstitutionResponse[]>('/exchange/foreign-institutions'),

  getAvailableCourses: (q?: string) =>
    api.get<CourseResponse[]>('/exchange/available-courses', { params: q ? { q } : undefined })
}
