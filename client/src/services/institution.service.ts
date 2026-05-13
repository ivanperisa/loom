import { api } from './api'
import type {
  InstitutionResponse,
  HomeProgramResponse,
  PartnerProgramResponse,
  PartnerCourseResponse,
} from '@/types/institution.types'
import type { AuthMeResponse } from '@/types/auth.types'

export const institutionService = {
  getHomeInstitutions: () =>
    api.get<InstitutionResponse[]>('/api/institutions/home'),
  getHomePrograms: () =>
    api.get<HomeProgramResponse[]>('/api/institutions/home-programs'),
  getPartnerPrograms: () =>
    api.get<PartnerProgramResponse[]>('/api/institutions/partner-programs'),
  getPartnerCourses: (partnerProgramId: string) =>
    api.get<PartnerCourseResponse[]>(`/api/institutions/partner-programs/${partnerProgramId}/courses`),
  getCoordinators: () =>
    api.get<AuthMeResponse[]>('/api/institutions/coordinators'),
}
