import { QueryClient } from '@tanstack/vue-query'
import { toApiError } from '@/utils/apiError'

/**
 * Server state lives here, not in Pinia. Reads show their own error (<ErrorAlert>);
 * failed actions get a toast from the axios interceptor.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      // A 4xx will not fix itself; only retry what might (network, 5xx), once.
      retry: (failureCount, error) => {
        const status = toApiError(error).status
        return failureCount < 1 && (status === null || status >= 500)
      },
    },
    mutations: { retry: false },
  },
})
