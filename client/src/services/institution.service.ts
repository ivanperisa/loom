import { api } from './api'
import type {
  InstitutionResponse,
  HomeProgramResponse,
  PartnerProgramResponse,
  PartnerCourseResponse,
} from '@/types/institution.types'
import type { PartnerInstitutionAdminResponse } from './admin.service'

export const institutionService = {
  getHomeInstitutions: () =>
    api.get<InstitutionResponse[]>('/api/institutions/home'),
  getHomePrograms: () =>
    api.get<HomeProgramResponse[]>('/api/institutions/home-programs'),
  getPartnerInstitutions: () =>
    api.get<PartnerInstitutionAdminResponse[]>('/api/institutions/partner'),
  getPartnerPrograms: () =>
    api.get<PartnerProgramResponse[]>('/api/institutions/partner-programs'),
  getPartnerCourses: (partnerProgramId: string) =>
    api.get<PartnerCourseResponse[]>(`/api/institutions/partner-programs/${partnerProgramId}/courses`),
}
