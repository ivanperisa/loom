import axios from 'axios'
import router from '@/router'
import { useAuthStore } from '@/stores/auth.store'
import { useNotification } from '@/composables/useNotification'
import { extractApiError } from '@/utils/apiError'
import { i18n } from '@/i18n'

let leavingGuestPage = false

declare module 'axios' {
  export interface AxiosRequestConfig {
    suppressErrorToast?: boolean
  }
}

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true,
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isCancel(error)) return Promise.reject(error)

    const status = error.response?.status

    if (status === 401) {
      // A guest whose link was regenerated or claimed, or whose session expired.
      const wasGuest = router.currentRoute.value.path.startsWith('/guest/')
      useAuthStore().reset()
      if (wasGuest && !leavingGuestPage) {
        leavingGuestPage = true
        useNotification().notifyError(i18n.global.t('exchangeAccess.sessionEnded'))
        router.push('/').finally(() => (leavingGuestPage = false))
      } else if (!wasGuest) {
        router.push('/')
      }
      return Promise.reject(error)
    }

    if (!error.config?.suppressErrorToast) {
      const { notifyError } = useNotification()
      const { title, message } = extractApiError(error)
      notifyError(title, message)
    }

    return Promise.reject(error)
  },
)
