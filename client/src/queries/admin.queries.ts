import { useQueryClient } from '@tanstack/vue-query'
import { queryKeys } from '@/queries/keys'

/** A role or profile change can affect the user list, the pending requests and every coordinator picker. */
export function useRefreshAfterUserChange() {
  const client = useQueryClient()
  return () =>
    Promise.all([
      client.invalidateQueries({ queryKey: queryKeys.adminUsers }),
      client.invalidateQueries({ queryKey: queryKeys.coordinatorRequests }),
      client.invalidateQueries({ queryKey: queryKeys.coordinators }),
      client.invalidateQueries({ queryKey: queryKeys.coordinatorStudents }),
    ])
}
