import { api } from './api'
import type { CoordinatorStudentResponse, CreatePlaceholderStudentRequest } from '@/types/coordinator.types'

export const coordinatorService = {
  getStudents: () =>
    api.get<CoordinatorStudentResponse[]>('/api/coordinator/students'),
  createPlaceholderStudent: (request: CreatePlaceholderStudentRequest) =>
    api.post<CoordinatorStudentResponse>('/api/coordinator/students', request),
}
