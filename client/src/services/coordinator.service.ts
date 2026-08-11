import { api } from './api'
import type { CoordinatorStudentResponse, CreatePlaceholderStudentRequest, UpdateStudentRequest } from '@/types/coordinator.types'
import type { ExchangeSummaryResponse } from '@/types/exchange.types'
import type { AuthMeResponse } from '@/types/auth.types'

export const coordinatorService = {
  getCoordinators: () =>
    api.get<AuthMeResponse[]>('/api/coordinators'),
  getStudents: () =>
    api.get<CoordinatorStudentResponse[]>('/api/coordinator/students'),
  createPlaceholderStudent: (request: CreatePlaceholderStudentRequest) =>
    api.post<CoordinatorStudentResponse>('/api/coordinator/students', request),
  updateStudent: (studentId: string, request: UpdateStudentRequest) =>
    api.put<CoordinatorStudentResponse>(`/api/coordinator/students/${studentId}`, request),
  deleteStudent: (studentId: string) =>
    api.delete(`/api/coordinator/students/${studentId}`, { suppressErrorToast: true }),
  getStudentsExchanges: () =>
    api.get<ExchangeSummaryResponse[]>('/api/coordinator/students/exchanges'),
}
