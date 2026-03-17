import type { InstitutionResponse, StudyProgramResponse, StudyProfileResponse } from '@/types/institution.types'

export interface UserInstitutionResponse {
  userInstitutionId: string
  hasActiveExchanges: boolean
  institution: InstitutionResponse
  studyProgram?: StudyProgramResponse
  studyProfile?: StudyProfileResponse
}

export interface NewInstitutionRequestDto {
  name: string
  nameEn?: string | null
  country: string
  city?: string | null
  erasmusCode?: string | null
  iscedCode?: string | null
  programName?: string | null
  programNameEn?: string | null
  profileName?: string | null
  profileNameEn?: string | null
  level?: string | null
  durationSemesters?: number | null
}

export interface NewStudyProfileRequestDto {
  studyProgramId: string
  profileName: string
  profileNameEn?: string | null
}

export interface OnboardingRequestDto {
  role: 'Student' | 'Coordinator' | 'Admin'
  jmbag?: string
  institutions: InstitutionEntryDto[]
}

export interface UpdateProfileRequest {
  jmbag?: string | null
}

export interface InstitutionEntryDto {
  existingStudyProfileId?: string | null
  existingInstitutionId?: string | null
  newStudyProfile?: NewStudyProfileRequestDto | null
  newInstitution?: NewInstitutionRequestDto | null
}
