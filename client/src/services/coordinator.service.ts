import { api } from './api'
import type { CoordinatorOption, CoordinatorStudentResponse, CreatePlaceholderStudentRequest, UpdateStudentRequest } from '@/types/coordinator.types'
import type { ExchangeSummaryResponse } from '@/types/exchange.types'
import type { PagedParams, PagedResponse } from '@/types/paged.types'

let coordinatorsCache: ReturnType<typeof api.get<CoordinatorOption[]>> | null = null

export const coordinatorService = {
  getCoordinators: () =>
    (coordinatorsCache ??= api.get<CoordinatorOption[]>('/api/coordinators')),
  getStudents: (params: PagedParams = {}, signal?: AbortSignal) =>
    api.get<PagedResponse<CoordinatorStudentResponse>>('/api/coordinator/students', { params, signal }),
  createPlaceholderStudent: (request: CreatePlaceholderStudentRequest) =>
    api.post<CoordinatorStudentResponse>('/api/coordinator/students', request),
  updateStudent: (studentId: string, request: UpdateStudentRequest) =>
    api.put<CoordinatorStudentResponse>(`/api/coordinator/students/${studentId}`, request),
  deleteStudent: (studentId: string) =>
    api.delete(`/api/coordinator/students/${studentId}`, { errorToast: false }),
  getStudentsExchanges: () =>
    api.get<ExchangeSummaryResponse[]>('/api/coordinator/students/exchanges'),
}

export function invalidateCoordinators() {
  coordinatorsCache = null
}
