import { api } from './api'
import type { Schemas } from '@/api'
import type { ListParams, PagedResponse } from '@/types/paged.types'

export type UserListResponse = Schemas['UserListResponse']
export type AdminUpdateUserRequest = Schemas['AdminUpdateUserRequest']
export type CoordinatorRequestResponse = Schemas['CoordinatorRequestResponse']
export type CoordinatorWhitelistEntryResponse = Schemas['CoordinatorWhitelistEntryResponse']

export const adminService = {
  getAllUsers: (
    params: ListParams & { role?: string | null; institutionId?: number | null; registered?: boolean | null },
    signal?: AbortSignal,
  ) => api.get<PagedResponse<UserListResponse>>('/api/admin/users', { params, signal }),

  getCoordinatorRequests: (params: ListParams, signal?: AbortSignal) =>
    api.get<PagedResponse<CoordinatorRequestResponse>>('/api/admin/coordinator-requests', { params, signal }),

  setUserRole: (userId: number, role: string) =>
    api.patch<UserListResponse>(`/api/admin/users/${userId}/role`, { role }),

  /** Approving makes the student a coordinator; rejecting lets them ask again later. */
  decideCoordinatorRequest: (userId: number, status: 'Approved' | 'Rejected') =>
    api.patch(`/api/admin/coordinator-requests/${userId}`, { status }),

  getCoordinatorWhitelist: (params: ListParams, signal?: AbortSignal) =>
    api.get<PagedResponse<CoordinatorWhitelistEntryResponse>>('/api/admin/coordinator-whitelist', { params, signal }),

  addToWhitelist: (email: string) =>
    api.post<CoordinatorWhitelistEntryResponse>('/api/admin/coordinator-whitelist', { email }),

  removeFromWhitelist: (email: string) =>
    api.delete(`/api/admin/coordinator-whitelist/${encodeURIComponent(email)}`),

  updateUser: (userId: number, data: AdminUpdateUserRequest) =>
    api.put<UserListResponse>(`/api/admin/users/${userId}`, data),
}
