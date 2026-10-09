import { api } from '@/services/api'
import type { SessionResponse } from '@/types/auth.types'

export const authService = {
  session: () => api.get<SessionResponse>('/api/auth/session'),
  /** Google sign-in; the API sends the browser back to `returnUrl` (a path inside the app). */
  login: (returnUrl = '/home') => {
    window.location.href = `${import.meta.env.VITE_API_URL}/api/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`
  },
  logout: async (): Promise<void> => {
    await api.post('/api/auth/logout')
    window.location.href = import.meta.env.VITE_BASE_PATH
  },
}
