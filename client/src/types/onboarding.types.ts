import type { InstitutionEntryDto, NewInstitutionRequestDto, NewStudyProfileRequestDto } from '@/types/user.types'

export interface LocalInstitutionEntry {
  id: string
  institutionName: string
  programName?: string
  profileName?: string
  existingProgramId?: string
  existingStudyProfileId?: string
  existingInstitutionId?: string
  newStudyProfile?: NewStudyProfileRequestDto
  newInstitution?: NewInstitutionRequestDto
}

export function toInstitutionEntryDto(entry: LocalInstitutionEntry): InstitutionEntryDto {
  return {
    existingStudyProfileId: entry.existingStudyProfileId ?? null,
    existingInstitutionId: entry.existingInstitutionId ?? null,
    newStudyProfile: entry.newStudyProfile ?? null,
    newInstitution: entry.newInstitution ?? null
  }
}
