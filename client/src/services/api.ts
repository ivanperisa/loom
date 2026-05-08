import axios from 'axios'
import router from '@/router'
import { useAuthStore } from '@/stores/auth.store'
import { useNotification } from '@/composables/useNotification'
import { extractApiError } from '@/utils/apiError'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status

    if (status === 401) {
      useAuthStore().reset()
      router.push('/')
      return Promise.reject(error)
    }

    const { notifyError } = useNotification()
    const { title, message } = extractApiError(error)
    notifyError(title, message)

    return Promise.reject(error)
  }
)
