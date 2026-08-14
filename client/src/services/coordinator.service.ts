import { api } from './api'
import type { CoordinatorOption, CoordinatorStudentResponse, CreatePlaceholderStudentRequest, UpdateStudentRequest } from '@/types/coordinator.types'
import type { ExchangeSummaryResponse } from '@/types/exchange.types'
import type { PagedParams, PagedResponse } from '@/types/paged.types'

export const coordinatorService = {
  getCoordinators: () =>
    api.get<CoordinatorOption[]>('/api/coordinators'),
  getStudents: (params: PagedParams = {}) =>
    api.get<PagedResponse<CoordinatorStudentResponse>>('/api/coordinator/students', { params }),
  createPlaceholderStudent: (request: CreatePlaceholderStudentRequest) =>
    api.post<CoordinatorStudentResponse>('/api/coordinator/students', request),
  updateStudent: (studentId: string, request: UpdateStudentRequest) =>
    api.put<CoordinatorStudentResponse>(`/api/coordinator/students/${studentId}`, request),
  deleteStudent: (studentId: string) =>
    api.delete(`/api/coordinator/students/${studentId}`, { suppressErrorToast: true }),
  getStudentsExchanges: () =>
    api.get<ExchangeSummaryResponse[]>('/api/coordinator/students/exchanges'),
}
