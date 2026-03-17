import axios from 'axios'
import router from '@/router'
import { useAuthStore } from '@/stores/auth.store'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore().reset()
      router.push('/')
    }

    return Promise.reject(error)
  }
)
