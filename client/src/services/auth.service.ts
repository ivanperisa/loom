import { api } from '@/services/api'
import type { SessionResponse } from '@/types/auth.types'

export const authService = {
  session: () => api.get<SessionResponse>('/api/auth/session'),
  login: () => {
    window.location.href = `${import.meta.env.VITE_API_URL}/api/auth/login?returnUrl=/home`
  },
  logout: async (): Promise<void> => {
    await api.post('/api/auth/logout')
    window.location.href = import.meta.env.VITE_BASE_PATH
  },
}
