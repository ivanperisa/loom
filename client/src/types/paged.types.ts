export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  hasDeleted: boolean
}

/** What every list endpoint understands (server `ListQuery`): `sort=name` / `sort=-name`. */
export interface ListParams {
  page: number
  pageSize: number
  search?: string
  sort?: string
}
