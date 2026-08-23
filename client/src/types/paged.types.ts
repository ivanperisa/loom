export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  hasDeleted: boolean
}

export interface PagedParams {
  page?: number
  pageSize?: number
  search?: string
  sortDir?: 'asc' | 'desc'
  academicYear?: string | null
  partnerInstitution?: string | null
}
