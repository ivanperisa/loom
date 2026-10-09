import { api } from './api'
import type { ListParams, PagedResponse } from '@/types/paged.types'

export interface UserListResponse {
  id: string
  name: string
  email: string
  role: string
  institutionName: string | null
  institutionCity: string | null
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
  getAllUsers: (
    params: ListParams & { role?: string | null; institutionId?: string | null; registered?: boolean | null },
    signal?: AbortSignal,
  ) => api.get<PagedResponse<UserListResponse>>('/api/admin/users', { params, signal }),

  getCoordinatorRequests: (params: ListParams, signal?: AbortSignal) =>
    api.get<PagedResponse<CoordinatorRequestResponse>>('/api/admin/coordinator-requests', { params, signal }),

  setUserRole: (userId: string, role: string) =>
    api.patch<UserListResponse>(`/api/admin/users/${userId}/role`, { role }),

  rejectCoordinatorRequest: (userId: string) =>
    api.patch(`/api/admin/users/${userId}/reject-coordinator-request`),

  getCoordinatorWhitelist: (params: ListParams, signal?: AbortSignal) =>
    api.get<PagedResponse<CoordinatorWhitelistEntryResponse>>('/api/admin/coordinator-whitelist', { params, signal }),

  addToWhitelist: (email: string) =>
    api.post<CoordinatorWhitelistEntryResponse>('/api/admin/coordinator-whitelist', { email }),

  removeFromWhitelist: (email: string) =>
    api.delete(`/api/admin/coordinator-whitelist/${encodeURIComponent(email)}`),

  updateUser: (userId: string, data: AdminUpdateUserRequest) =>
    api.put<UserListResponse>(`/api/admin/users/${userId}`, data),
}
