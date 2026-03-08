import type { UserInstitutionDto } from '@/types/user.types'

export type UserRole = 'Student' | 'Coordinator'

export interface AuthMeResponse {
  isAuthenticated: boolean
  sub?: string
  email?: string
  name?: string
  role?: string
  isOnboarded: boolean
  institutions: UserInstitutionDto[]
}
