export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface PagedParams {
  page?: number
  pageSize?: number
  search?: string
  sortDir?: 'asc' | 'desc'
}
