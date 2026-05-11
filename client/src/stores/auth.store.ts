import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { authService } from '@/services/auth.service'
import { userService } from '@/services/user.service'
import { api } from '@/services/api'
import router from '@/router'
import type { AuthMeResponse, UserRole } from '@/types/auth.types'
import type { CompleteOnboardingRequest, UpdateProfileRequest } from '@/types/onboarding.types'
import { userRole } from '../utils/userRole'

export const useAuthStore = defineStore('auth', () => {
  let initPromise: Promise<void> | null = null
  const user = ref<AuthMeResponse | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const isLoggedIn = computed(() => user.value !== null)
  const isOnboarded = computed(() => user.value?.isOnboarded ?? false)
  const role = computed<UserRole | null>(() => user.value?.role ?? null)
  const email = computed(() => user.value?.email ?? null)
  const name = computed(() => user.value?.name ?? null)
  const jmbag = computed(() => user.value?.jmbag ?? null)
  const isAdmin = computed(() => user.value?.role === userRole.Admin)
  const isStudent = computed(() => user.value?.role === userRole.Student)
  const canActAsCoordinator = computed(() => user.value?.role === userRole.Coordinator || user.value?.role === userRole.Admin)

  async function init(force = false) {
    if (initPromise && !force) return initPromise

    initPromise = (async () => {
      loading.value = true
      error.value = null
      try {
        const response = await api.get<AuthMeResponse | { isAuthenticated: false }>('/auth/me')
        const data = response.data
        if ('isAuthenticated' in data && data.isAuthenticated === false) {
          reset()
          return
        }
        user.value = data as AuthMeResponse
      } catch {
        reset()
      } finally {
        loading.value = false
      }
    })()

    return initPromise
  }

  function reset() {
    user.value = null
    error.value = null
    initPromise = null
  }

  function login() {
    authService.login()
  }

  async function logout() {
    try {
      await authService.logout()
    } finally {
      reset()
      router.push('/')
    }
  }

  async function completeOnboarding(request: CompleteOnboardingRequest) {
    loading.value = true
    error.value = null
    try {
      const response = await userService.completeOnboarding(request)
      user.value = response.data
    } catch (e: unknown) {
      throw e
    } finally {
      loading.value = false
    }
  }

  async function updateProfile(request: UpdateProfileRequest) {
    loading.value = true
    error.value = null
    try {
      const response = await userService.updateProfile(request)
      user.value = response.data
    } catch (e: unknown) {
      throw e
    } finally {
      loading.value = false
    }
  }

  return {
    user,
    loading,
    error,
    isLoggedIn,
    isOnboarded,
    role,
    email,
    name,
    jmbag,
    isAdmin,
    isStudent,
    canActAsCoordinator,
    init,
    login,
    logout,
    reset,
    completeOnboarding,
    updateProfile,
  }
})
