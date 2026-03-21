export interface CompleteOnboardingRequest {
  institutionId: string
  jmbag?: string | null
}

export interface UpdateProfileRequest {
  name: string
  jmbag: string | null
  institutionId: string
  mentor: string | null
  coordinatorId: string | null
}
