export interface CoordinatorStudentResponse {
  id: string
  name: string
  jmbag: string | null
  institutionName: string | null
  isPlaceholder: boolean
}

export interface CreatePlaceholderStudentRequest {
  name: string
  jmbag: string
  institutionId: string
}
