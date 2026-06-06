import { api } from './api'
import type { AuthMeResponse } from '@/types/auth.types'
import type { CompleteOnboardingRequest, UpdateProfileRequest } from '@/types/onboarding.types'

export const userService = {
  getMe: () => api.get<AuthMeResponse>('/users/me'),
  completeOnboarding: (request: CompleteOnboardingRequest) =>
    api.post<AuthMeResponse>('/users/me/onboarding', request),
  updateProfile: (request: UpdateProfileRequest) =>
    api.put<AuthMeResponse>('/users/me', request),
  requestCoordinatorRole: () =>
    api.post<AuthMeResponse>('/users/me/coordinator-request'),
}
