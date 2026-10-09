import { api } from './api'
import type { AccessLinkPreviewResponse } from '@/types/exchange.types'

/** Access links. The token goes in the request body, never in an API URL. */
export const accessService = {
  /** Without a login: swaps the token for a guest cookie scoped to one exchange. */
  openSession: (token: string) =>
    api.post<{ exchangeGuid: string }>('/api/access/session', { token }, { errorToast: false }),
  closeSession: () => api.delete('/api/access/session'),
  /** Signed in: is this exchange already mine to open, or can I claim it? */
  preview: (token: string) =>
    api.post<AccessLinkPreviewResponse>('/api/access/preview', { token }, { errorToast: false }),
  claim: (token: string) =>
    api.post<{ exchangeGuid: string }>('/api/access/claim', { token }, { errorToast: false }),
}
