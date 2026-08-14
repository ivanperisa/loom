export interface CoordinatorStudentResponse {
  id: string
  name: string
  jmbag: string | null
  institutionName: string | null
  isPlaceholder: boolean
  institutionId: string | null
  isMyStudent: boolean
}

export interface CreatePlaceholderStudentRequest {
  name: string
  jmbag: string
  institutionId: string
}

export type UpdateStudentRequest = CreatePlaceholderStudentRequest

export interface CoordinatorOption {
  id: string
  name: string
}
