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
  getPartnerInstitutions: () =>
    api.get<PartnerInstitutionAdminResponse[]>('/api/institutions/partner'),
  getPartnerCoursesByInstitution: (institutionId: string) =>
    api.get<PartnerCourseResponse[]>(`/api/institutions/partner/${institutionId}/courses`),

  createPartnerInstitution: (data: { name: string; nameHr: string; country: string; city?: string; erasmusCode?: string }) =>
    api.post<PartnerInstitutionAdminResponse>('/api/institutions/partner', data),

  deletePartnerInstitution: (id: string) =>
    api.delete(`/api/institutions/partner/${id}`),

  createPartnerCourseByInstitution: (institutionId: string, data: { code: string; nameHr?: string; name: string; ects: number; semester: string; level: string; lecturesH?: number; auditoryH?: number; labH?: number }) =>
    api.post<PartnerCourseResponse>(`/api/institutions/partner/${institutionId}/courses`, data),

  updatePartnerCourse: (courseId: string, data: { code: string; nameHr?: string; name: string; ects: number; semester: string; level: string; lecturesH?: number; auditoryH?: number; labH?: number }) =>
    api.put<PartnerCourseResponse>(`/api/institutions/partner/courses/${courseId}`, data),

  deletePartnerCourse: (courseId: string) =>
    api.delete(`/api/institutions/partner/courses/${courseId}`),

  mergePartnerCourses: (primaryCourseId: string, duplicateCourseIds: string[]) =>
    api.post<PartnerCourseResponse>('/api/institutions/partner/courses/merge', {
      primaryCourseId,
      duplicateCourseIds,
    }),
}
