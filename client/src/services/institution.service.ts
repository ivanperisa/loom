import { api } from './api'
import type { AxiosResponse } from 'axios'
import type {
  InstitutionResponse,
  HomeProgramResponse,
  PartnerCourseResponse,
  PartnerInstitutionAdminResponse,
  PartnerCourseUsage,
  PartnerCourseRequest,
} from '@/types/institution.types'
import type { ListParams, PagedResponse } from '@/types/paged.types'

export const institutionService = {
  getHomeInstitutions: (signal?: AbortSignal) => api.get<InstitutionResponse[]>('/api/institutions/home', { signal }),
  getHomePrograms: (signal?: AbortSignal) => api.get<HomeProgramResponse[]>('/api/institutions/home-programs', { signal }),
  getPartnerInstitutions: (includeDeleted = false, params: Partial<ListParams> & { country?: string | null } = {}, signal?: AbortSignal) =>
    api.get<PagedResponse<PartnerInstitutionAdminResponse>>('/api/institutions/partner', { params: { includeDeleted, ...params }, signal }),
  getPartnerCoursesByInstitution: (institutionId: number, includeDeleted = false, params: Partial<ListParams> & { semester?: string | null; level?: string | null } = {}, signal?: AbortSignal) =>
    api.get<PagedResponse<PartnerCourseResponse>>(`/api/institutions/partner/${institutionId}/courses`, { params: { includeDeleted, ...params }, signal }),

  createPartnerInstitution: (data: { name: string; nameHr: string; country: string; city?: string; erasmusCode?: string }) =>
    api.post<PartnerInstitutionAdminResponse>('/api/institutions/partner', data),

  updatePartnerInstitution: (id: number, data: { name: string; nameHr?: string; country: string; city?: string; erasmusCode?: string }) =>
    api.put<PartnerInstitutionAdminResponse>(`/api/institutions/partner/${id}`, data),

  deletePartnerInstitution: (id: number) =>
    api.delete(`/api/institutions/partner/${id}`),

  restorePartnerInstitution: (id: number) =>
    api.patch(`/api/institutions/partner/${id}/restore`),

  createPartnerCourseByInstitution: (institutionId: number, data: PartnerCourseRequest) =>
    api.post<PartnerCourseResponse>(`/api/institutions/partner/${institutionId}/courses`, data),

  updatePartnerCourse: (courseId: number, data: PartnerCourseRequest) =>
    api.put<PartnerCourseResponse>(`/api/institutions/partner/courses/${courseId}`, data),

  deletePartnerCourse: (courseId: number) =>
    api.delete(`/api/institutions/partner/courses/${courseId}`),

  restorePartnerCourse: (courseId: number) =>
    api.patch(`/api/institutions/partner/courses/${courseId}/restore`),

  mergePartnerCourses: (primaryCourseId: number, duplicateCourseIds: number[]) =>
    api.post<PartnerCourseResponse>('/api/institutions/partner/courses/merge', {
      primaryCourseId,
      duplicateCourseIds,
    }),

  getPartnerCourseUsage: (courseId: number) =>
    api.get<PartnerCourseUsage>(`/api/institutions/partner/courses/${courseId}/usage`),
}

const PAGE_SIZE = 200

/** Loads every page of a paged list (for dropdowns and local filtering of small lists). */
export async function fetchAllPages<T>(
  fetchPage: (page: number, pageSize: number) => Promise<AxiosResponse<PagedResponse<T>>>,
): Promise<T[]> {
  const first = await fetchPage(1, PAGE_SIZE)
  const items = [...first.data.items]
  const totalPages = Math.ceil(first.data.totalCount / first.data.pageSize) || 1
  if (totalPages > 1) {
    const rest = await Promise.all(
      Array.from({ length: totalPages - 1 }, (_, i) => fetchPage(i + 2, PAGE_SIZE)),
    )
    for (const res of rest) items.push(...res.data.items)
  }
  return items
}
