import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { authService } from '@/services/auth.service'
import { userService } from '@/services/user.service'
import { api } from '@/services/api'
import router from '@/router'
import type { AuthMeResponse, UserRole } from '@/types/auth.types'
import type { InstitutionEntryDto, UserInstitutionResponse } from '@/types/user.types'

export const useAuthStore = defineStore('auth', () => {
  let initPromise: Promise<void> | null = null
  const user = ref<AuthMeResponse | null>(null)
  const isOnboarded = ref<boolean>(false)
  const role = ref<UserRole | null>(null)
  const institutions = ref<UserInstitutionResponse[]>([])

  const isLoggedIn = computed(() => user.value?.isAuthenticated === true)
  const email = computed(() => user.value?.email ?? null)
  const name = computed(() => user.value?.name ?? null)
  const sub = computed(() => user.value?.sub ?? null)
  const jmbag = computed(() => user.value?.jmbag ?? null)

  async function init(force = false) {
    if (initPromise && !force) return initPromise

    initPromise = (async () => {
      try {
        const response = await api.get<AuthMeResponse>('/auth/me')
        const data = response.data
        if (data.isAuthenticated) {
          user.value = data
          isOnboarded.value = data.isOnboarded
          role.value = (data.role as UserRole) ?? null
          institutions.value = data.institutions ?? []
          return
        }
      } catch {
        // keep defaults below
      }

      reset()
    })()

    return initPromise
  }

  function reset() {
    user.value = null
    isOnboarded.value = false
    role.value = null
    institutions.value = []
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

  async function addInstitution(request: InstitutionEntryDto): Promise<void> {
    await userService.addInstitution(request)
    await init(true)
  }

  async function updateInstitution(userInstitutionId: string, request: InstitutionEntryDto): Promise<void> {
    await userService.updateInstitution(userInstitutionId, request)
    await init(true)
  }

  async function removeInstitution(userInstitutionId: string): Promise<void> {
    await userService.removeInstitution(userInstitutionId)
    await init(true)
  }

  async function updateJmbag(value: string | null): Promise<void> {
    await userService.updateProfile({ jmbag: value || null })
    await init(true)
  }

  return {
    user,
    isOnboarded,
    role,
    institutions,
    email,
    name,
    sub,
    jmbag,
    isLoggedIn,
    init,
    login,
    logout,
    reset,
    addInstitution,
    updateInstitution,
    removeInstitution,
    updateJmbag
  }
})
