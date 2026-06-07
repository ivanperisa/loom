import { api } from './api'
import type { CoordinatorStudentResponse, CreatePlaceholderStudentRequest } from '@/types/coordinator.types'
import type { ExchangeSummaryResponse } from '@/types/exchange.types'
import type { AuthMeResponse } from '@/types/auth.types'

export const coordinatorService = {
  getCoordinators: () =>
    api.get<AuthMeResponse[]>('/api/coordinators'),
  getStudents: () =>
    api.get<CoordinatorStudentResponse[]>('/api/coordinator/students'),
  createPlaceholderStudent: (request: CreatePlaceholderStudentRequest) =>
    api.post<CoordinatorStudentResponse>('/api/coordinator/students', request),
  getStudentsExchanges: () =>
    api.get<ExchangeSummaryResponse[]>('/api/coordinator/students/exchanges'),
}
