import { api } from './api'
import type { CoordinatorOption, CoordinatorStudentResponse, CreatePlaceholderStudentRequest, StudentFiltersResponse, UpdateStudentRequest } from '@/types/coordinator.types'
import type { ListParams, PagedResponse } from '@/types/paged.types'

export type StudentListParams = ListParams & { academicYear?: string | null; partnerInstitution?: string | null }

export const coordinatorService = {
  getCoordinators: (signal?: AbortSignal) => api.get<CoordinatorOption[]>('/api/coordinators', { signal }),
  getStudents: (params: StudentListParams, signal?: AbortSignal) =>
    api.get<PagedResponse<CoordinatorStudentResponse>>('/api/coordinator/students', { params, signal }),
  createPlaceholderStudent: (request: CreatePlaceholderStudentRequest) =>
    api.post<CoordinatorStudentResponse>('/api/coordinator/students', request),
  updateStudent: (studentId: string, request: UpdateStudentRequest) =>
    api.put<CoordinatorStudentResponse>(`/api/coordinator/students/${studentId}`, request),
  deleteStudent: (studentId: string) =>
    api.delete(`/api/coordinator/students/${studentId}`, { errorToast: false }),
  /** Years and partner institutions the student list can be filtered by. */
  getStudentFilters: (signal?: AbortSignal) => api.get<StudentFiltersResponse>('/api/coordinator/students/filters', { signal }),
}
