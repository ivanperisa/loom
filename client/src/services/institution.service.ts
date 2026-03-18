import { api } from './api'
import type {
  InstitutionResponse,
  StudyProgramResponse,
  ForeignProgramResponse,
  ForeignCourseResponse,
} from '@/types/institution.types'
import type { AuthMeResponse } from '@/types/auth.types'

export const institutionService = {
  getHomeInstitutions: () =>
    api.get<InstitutionResponse[]>('/api/institutions/home'),
  getStudyPrograms: () =>
    api.get<StudyProgramResponse[]>('/api/institutions/study-programs'),
  getForeignPrograms: () =>
    api.get<ForeignProgramResponse[]>('/api/institutions/foreign-programs'),
  getForeignCourses: (foreignProgramId: string) =>
    api.get<ForeignCourseResponse[]>(`/api/institutions/foreign-programs/${foreignProgramId}/courses`),
  getCoordinators: () =>
    api.get<AuthMeResponse[]>('/api/institutions/coordinators'),
}
