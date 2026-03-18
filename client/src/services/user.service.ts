import { api } from './api'
import type { AuthMeResponse } from '@/types/auth.types'
import type { CompleteOnboardingRequest } from '@/types/onboarding.types'

export const userService = {
  getMe: () => api.get<AuthMeResponse>('/api/users/me'),
  completeOnboarding: (request: CompleteOnboardingRequest) =>
    api.post<AuthMeResponse>('/api/users/me/onboarding', request),
}
