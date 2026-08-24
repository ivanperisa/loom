import { api } from './api'
import type { AxiosResponse } from 'axios'
import type {
  InstitutionResponse,
  HomeProgramResponse,
  PartnerCourseResponse,
  PartnerInstitutionAdminResponse,
  PartnerCourseUsage,
} from '@/types/institution.types'
import type { PagedParams, PagedResponse } from '@/types/paged.types'

let homeInstitutionsCache: ReturnType<typeof api.get<InstitutionResponse[]>> | null = null
let homeProgramsCache: ReturnType<typeof api.get<HomeProgramResponse[]>> | null = null

export const institutionService = {
  getHomeInstitutions: () =>
    (homeInstitutionsCache ??= api.get<InstitutionResponse[]>('/api/institutions/home')),
  getHomePrograms: () =>
    (homeProgramsCache ??= api.get<HomeProgramResponse[]>('/api/institutions/home-programs')),
  getPartnerInstitutions: (includeDeleted = false, params: PagedParams = {}, signal?: AbortSignal) =>
    api.get<PagedResponse<PartnerInstitutionAdminResponse>>('/api/institutions/partner', { params: { includeDeleted, ...params }, signal }),
  getPartnerCoursesByInstitution: (institutionId: string, includeDeleted = false, params: PagedParams = {}, signal?: AbortSignal) =>
    api.get<PagedResponse<PartnerCourseResponse>>(`/api/institutions/partner/${institutionId}/courses`, { params: { includeDeleted, ...params }, signal }),

  createPartnerInstitution: (data: { name: string; nameHr: string; country: string; city?: string; erasmusCode?: string }) =>
    api.post<PartnerInstitutionAdminResponse>('/api/institutions/partner', data),

  updatePartnerInstitution: (id: string, data: { name: string; nameHr?: string; country: string; city?: string; erasmusCode?: string }) =>
    api.put<PartnerInstitutionAdminResponse>(`/api/institutions/partner/${id}`, data),

  deletePartnerInstitution: (id: string) =>
    api.delete(`/api/institutions/partner/${id}`),

  restorePartnerInstitution: (id: string) =>
    api.patch(`/api/institutions/partner/${id}/restore`),

  createPartnerCourseByInstitution: (institutionId: string, data: { code: string; nameHr?: string; url?: string; name: string; ects: number; semester: string; level: string; lecturesH?: number; auditoryH?: number; labH?: number }) =>
    api.post<PartnerCourseResponse>(`/api/institutions/partner/${institutionId}/courses`, data),

  updatePartnerCourse: (courseId: string, data: { code: string; nameHr?: string; url?: string; name: string; ects: number; semester: string; level: string; lecturesH?: number; auditoryH?: number; labH?: number }) =>
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

  getPartnerCourseUsage: (courseId: string) =>
    api.get<PartnerCourseUsage>(`/api/institutions/partner/courses/${courseId}/usage`),
}

const DROPDOWN_TTL_MS = 60_000
const PAGE_SIZE = 200

async function fetchAllPages<T>(
  fetchPage: (page: number) => Promise<AxiosResponse<PagedResponse<T>>>,
): Promise<T[]> {
  const first = await fetchPage(1)
  const items = [...first.data.items]
  const totalPages = Math.ceil(first.data.totalCount / first.data.pageSize) || 1
  if (totalPages > 1) {
    const rest = await Promise.all(
      Array.from({ length: totalPages - 1 }, (_, i) => fetchPage(i + 2)),
    )
    for (const res of rest) items.push(...res.data.items)
  }
  return items
}

let allPartnerInstitutions: { at: number; promise: Promise<PartnerInstitutionAdminResponse[]> } | null = null

export function getAllPartnerInstitutions(): Promise<PartnerInstitutionAdminResponse[]> {
  if (!allPartnerInstitutions || Date.now() - allPartnerInstitutions.at > DROPDOWN_TTL_MS) {
    allPartnerInstitutions = {
      at: Date.now(),
      promise: fetchAllPages((page) =>
        institutionService.getPartnerInstitutions(false, { page, pageSize: PAGE_SIZE }),
      ),
    }
  }
  return allPartnerInstitutions.promise
}

const partnerCoursesByInstitution = new Map<string, { at: number; promise: Promise<PartnerCourseResponse[]> }>()

export function getAllPartnerCourses(institutionId: string, force = false): Promise<PartnerCourseResponse[]> {
  const cached = partnerCoursesByInstitution.get(institutionId)
  if (force || !cached || Date.now() - cached.at > DROPDOWN_TTL_MS) {
    const entry = {
      at: Date.now(),
      promise: fetchAllPages((page) =>
        institutionService.getPartnerCoursesByInstitution(institutionId, false, { page, pageSize: PAGE_SIZE }),
      ),
    }
    partnerCoursesByInstitution.set(institutionId, entry)
    return entry.promise
  }
  return cached.promise
}
