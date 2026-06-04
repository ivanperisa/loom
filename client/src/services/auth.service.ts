import { api } from '@/services/api'

export const authService = {
  login: () => {
    window.location.href = `${import.meta.env.VITE_API_URL}/auth/login?returnUrl=/home`
  },
  logout: async (): Promise<void> => {
    await api.post('/auth/logout')
    window.location.href = import.meta.env.VITE_BASE_PATH
  },
}
