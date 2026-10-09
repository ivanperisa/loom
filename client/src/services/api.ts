import axios from 'axios'
import router from '@/router'
import { useAuthStore } from '@/stores/auth.store'
import { useNotification } from '@/composables/useNotification'
import { describeApiError } from '@/utils/apiError'
import { queryClient } from '@/queries/queryClient'
import { i18n } from '@/i18n'

let leavingGuestPage = false

declare module 'axios' {
  export interface AxiosRequestConfig {
    /**
     * Show a toast when the request fails. Defaults to true for actions (POST/PUT/PATCH/DELETE)
     * and false for reads, whose screen shows the error itself (<ErrorAlert>).
     */
    errorToast?: boolean
  }
}

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true,
})

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (axios.isCancel(error)) return Promise.reject(error)

    if (error.response?.status === 401) {
      // The session ended: logged out elsewhere, expired, or a guest link was regenerated or claimed.
      const route = router.currentRoute.value
      const wasGuest = route.path.startsWith('/guest/')
      useAuthStore().reset()
      queryClient.clear()
      if (wasGuest && !leavingGuestPage) {
        leavingGuestPage = true
        useNotification().notifyError(i18n.global.t('exchangeAccess.sessionEnded'))
        router.push('/').finally(() => (leavingGuestPage = false))
      } else if (!wasGuest && route.meta.requiresAuth) {
        router.push({ path: '/', query: { redirect: route.fullPath } })
      }
      return Promise.reject(error)
    }

    const config = error.config
    const isRead = (config?.method ?? 'get').toLowerCase() === 'get'
    if (config?.errorToast ?? !isRead) {
      // A failed blob download carries its ProblemDetails as a Blob; read it so the message is right.
      if (error.response?.data instanceof Blob && error.response.data.type.includes('json')) {
        try {
          error.response.data = JSON.parse(await error.response.data.text())
        } catch {
          /* keep the generic message */
        }
      }
      const { title, message } = describeApiError(error)
      useNotification().notifyError(title, message)
    }

    return Promise.reject(error)
  },
)
