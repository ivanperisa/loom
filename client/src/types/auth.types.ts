import type { UserInstitutionResponse } from '@/types/user.types'

export type UserRole = 'Student' | 'Coordinator' | 'Admin'

export interface AuthMeResponse {
  isAuthenticated: boolean
  sub?: string
  email?: string
  name?: string
  role?: string
  jmbag?: string
  isOnboarded: boolean
  institutions: UserInstitutionResponse[]
}
