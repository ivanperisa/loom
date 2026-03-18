export interface AuthMeResponse {
  id: string
  email: string
  name: string
  jmbag: string | null
  role: UserRole
  isOnboarded: boolean
  institutionId: string | null
  institutionName: string | null
}

export type UserRole = 'Student' | 'Coordinator' | 'Admin'
