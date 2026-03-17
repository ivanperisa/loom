import { api } from '@/services/api'
import type { InstitutionResponse, StudyProgramResponse, StudyProfileResponse } from '@/types/institution.types'

export const institutionService = {
  getHomeInstitutions: () =>
    api.get<InstitutionResponse[]>('/institutions'),

  getProgramsByInstitution: (institutionId: string) =>
    api.get<StudyProgramResponse[]>(`/institutions/${institutionId}/programs`),

  getProfilesByProgram: (institutionId: string, programId: string) =>
    api.get<StudyProfileResponse[]>(`/institutions/${institutionId}/programs/${programId}/profiles`)
}
