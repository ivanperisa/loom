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
    api.get<InstitutionResponse[]>('/institutions/home'),
  getHomePrograms: () =>
    api.get<HomeProgramResponse[]>('/institutions/home-programs'),
  getPartnerPrograms: () =>
    api.get<PartnerProgramResponse[]>('/institutions/partner-programs'),
  getPartnerCourses: (partnerProgramId: string) =>
    api.get<PartnerCourseResponse[]>(`/institutions/partner-programs/${partnerProgramId}/courses`),
  getCoordinators: () =>
    api.get<AuthMeResponse[]>('/institutions/coordinators'),
}
