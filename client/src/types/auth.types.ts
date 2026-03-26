export interface AuthMeResponse {
  id: string
  email: string
  name: string
  jmbag: string | null
  mentor: string | null
  role: UserRole
  isOnboarded: boolean
  institutionId: string | null
  institutionName: string | null
  coordinatorId: string | null
  coordinatorName: string | null
  coordinatorRequestStatus: 'Pending' | 'Rejected' | null
}

export type UserRole = 'Student' | 'Coordinator' | 'Admin'
