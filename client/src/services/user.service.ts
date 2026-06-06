import { api } from './api'
import type { AuthMeResponse } from '@/types/auth.types'
import type { CompleteOnboardingRequest, UpdateProfileRequest } from '@/types/onboarding.types'

export const userService = {
  getMe: () => api.get<AuthMeResponse>('/api/users/me'),
  completeOnboarding: (request: CompleteOnboardingRequest) =>
    api.post<AuthMeResponse>('/api/users/me/onboarding', request),
  updateProfile: (request: UpdateProfileRequest) =>
    api.put<AuthMeResponse>('/api/users/me', request),
  requestCoordinatorRole: () =>
    api.post<AuthMeResponse>('/api/users/me/coordinator-request'),
}
