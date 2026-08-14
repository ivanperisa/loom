import { api } from './api'
import type { PagedParams, PagedResponse } from '@/types/paged.types'

export interface UserListResponse {
  id: string
  name: string
  email: string
  role: string
  institutionName: string | null
  institutionId: string | null
  coordinatorRequestStatus: string | null
  isOnboarded: boolean
  jmbag: string | null
  mentor: string | null
  coordinatorId: string | null
  coordinatorName: string | null
}

export interface AdminUpdateUserRequest {
  name: string
  jmbag: string | null
  mentor: string | null
  coordinatorId: string | null
  institutionId: string | null
}

export interface CoordinatorRequestResponse {
  id: string
  name: string
  email: string
  institutionName: string | null
}

export interface CoordinatorWhitelistEntryResponse {
  id: string
  email: string
  createdAt: string
}

export const adminService = {
  getAllUsers: (params: PagedParams = {}) =>
    api.get<PagedResponse<UserListResponse>>('/api/admin/users', { params }),

  getCoordinatorRequests: () =>
    api.get<CoordinatorRequestResponse[]>('/api/admin/coordinator-requests'),

  makeCoordinator: (userId: string) => api.patch(`/api/admin/users/${userId}/make-coordinator`),

  rejectCoordinatorRequest: (userId: string) =>
    api.patch(`/api/admin/users/${userId}/reject-coordinator-request`),

  removeCoordinator: (userId: string) => api.patch(`/api/admin/users/${userId}/remove-coordinator`),

  getCoordinatorWhitelist: () =>
    api.get<CoordinatorWhitelistEntryResponse[]>('/api/admin/coordinator-whitelist'),

  addToWhitelist: (email: string) =>
    api.post<CoordinatorWhitelistEntryResponse>('/api/admin/coordinator-whitelist', { email }),

  removeFromWhitelist: (email: string) =>
    api.delete(`/api/admin/coordinator-whitelist/${encodeURIComponent(email)}`),

  updateUser: (userId: string, data: AdminUpdateUserRequest) =>
    api.put<UserListResponse>(`/api/admin/users/${userId}`, data),
}
