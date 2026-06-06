import { api } from './api'
import type { PartnerCourseResponse } from '@/types/institution.types'

export interface UserListResponse {
  id: string
  name: string
  email: string
  role: string
  institutionName: string | null
  coordinatorRequestStatus: string | null
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

export interface PartnerProgramAdminResponse {
  id: string
  name: string
  nameEn: string | null
  level: string
  courseCount: number
}

export interface PartnerInstitutionAdminResponse {
  id: string
  name: string
  nameEn: string
  country: string
  city: string | null
  erasmusCode: string | null
  programs: PartnerProgramAdminResponse[]
}

export const adminService = {
  getAllUsers: () =>
    api.get<UserListResponse[]>('/api/admin/users'),

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

  getPartnerInstitutions: () =>
    api.get<PartnerInstitutionAdminResponse[]>('/api/admin/institutions'),

  createPartnerInstitution: (data: { name: string; nameEn: string; country: string; city?: string; erasmusCode?: string }) =>
    api.post<PartnerInstitutionAdminResponse>('/api/admin/institutions', data),

  deletePartnerInstitution: (id: string) =>
    api.delete(`/api/admin/institutions/${id}`),

  createPartnerProgram: (institutionId: string, data: { name: string; nameEn?: string; level: string }) =>
    api.post<PartnerProgramAdminResponse>(`/api/admin/institutions/${institutionId}/programs`, data),

  deletePartnerProgram: (programId: string) =>
    api.delete(`/api/admin/institutions/programs/${programId}`),

  createPartnerCourse: (programId: string, data: { code: string; nameEn: string; nameHr?: string; ects: number; lecturesH?: number; auditoryH?: number; labH?: number }) =>
    api.post<PartnerCourseResponse>(`/api/admin/institutions/programs/${programId}/courses`, data),

  deletePartnerCourse: (courseId: string) =>
    api.delete(`/api/admin/institutions/programs/courses/${courseId}`),
}
