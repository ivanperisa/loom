import { api } from './api'
import type {
  InstitutionResponse,
  HomeProgramResponse,
  PartnerCourseResponse,
  PartnerInstitutionAdminResponse,
} from '@/types/institution.types'

export const institutionService = {
  getHomeInstitutions: () =>
    api.get<InstitutionResponse[]>('/api/institutions/home'),
  getHomePrograms: () =>
    api.get<HomeProgramResponse[]>('/api/institutions/home-programs'),
  getPartnerInstitutions: (includeDeleted = false) =>
    api.get<PartnerInstitutionAdminResponse[]>('/api/institutions/partner', { params: { includeDeleted } }),
  getPartnerCoursesByInstitution: (institutionId: string, includeDeleted = false) =>
    api.get<PartnerCourseResponse[]>(`/api/institutions/partner/${institutionId}/courses`, { params: { includeDeleted } }),

  createPartnerInstitution: (data: { name: string; nameHr: string; country: string; city?: string; erasmusCode?: string }) =>
    api.post<PartnerInstitutionAdminResponse>('/api/institutions/partner', data),

  updatePartnerInstitution: (id: string, data: { name: string; nameHr?: string; country: string; city?: string; erasmusCode?: string }) =>
    api.put<PartnerInstitutionAdminResponse>(`/api/institutions/partner/${id}`, data),

  deletePartnerInstitution: (id: string) =>
    api.delete(`/api/institutions/partner/${id}`),

  restorePartnerInstitution: (id: string) =>
    api.patch(`/api/institutions/partner/${id}/restore`),

  createPartnerCourseByInstitution: (institutionId: string, data: { code: string; nameHr?: string; name: string; ects: number; semester: string; level: string; lecturesH?: number; auditoryH?: number; labH?: number }) =>
    api.post<PartnerCourseResponse>(`/api/institutions/partner/${institutionId}/courses`, data),

  updatePartnerCourse: (courseId: string, data: { code: string; nameHr?: string; name: string; ects: number; semester: string; level: string; lecturesH?: number; auditoryH?: number; labH?: number }) =>
    api.put<PartnerCourseResponse>(`/api/institutions/partner/courses/${courseId}`, data),

  deletePartnerCourse: (courseId: string) =>
    api.delete(`/api/institutions/partner/courses/${courseId}`),

  restorePartnerCourse: (courseId: string) =>
    api.patch(`/api/institutions/partner/courses/${courseId}/restore`),

  mergePartnerCourses: (primaryCourseId: string, duplicateCourseIds: string[]) =>
    api.post<PartnerCourseResponse>('/api/institutions/partner/courses/merge', {
      primaryCourseId,
      duplicateCourseIds,
    }),
}
