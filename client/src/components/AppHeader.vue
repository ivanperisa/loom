<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { useTheme } from '@/composables/useTheme'
import type { AppLocale } from '@/i18n/index'

const authStore = useAuthStore()
const { t, locale } = useI18n()
const router = useRouter()
const { theme, toggleTheme } = useTheme()

const locales: Array<{ code: AppLocale; flag: string; label: string }> = [
  { code: 'hr', flag: 'fi fi-hr', label: 'Hrvatski' },
  { code: 'en', flag: 'fi fi-gb', label: 'English' },
]

function setLocale(code: AppLocale) {
  locale.value = code
  localStorage.setItem('locale', code)
}

const dropdownOpen = ref(false)
const avatarRef = ref<HTMLElement | null>(null)

function onDocClick(e: MouseEvent) {
  if (!avatarRef.value?.contains(e.target as Node)) dropdownOpen.value = false
}

onMounted(() => document.addEventListener('mousedown', onDocClick))
onUnmounted(() => document.removeEventListener('mousedown', onDocClick))

const displayName = computed(() => authStore.user?.name?.trim() || t('common.user'))
const displayEmail = computed(() => authStore.user?.email?.trim() || t('common.na'))
const homeRoute = computed(() => authStore.canActAsCoordinator ? '/coordinator' : '/home')

const initials = computed(() => {
  const parts = displayName.value
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((value) => value[0]?.toUpperCase() ?? '')
    .join('')
  return parts || 'U'
})

function toggleDropdown() {
  dropdownOpen.value = !dropdownOpen.value
}

function goAdmin() {
  dropdownOpen.value = false
  router.push('/admin')
}

function logout() {
  dropdownOpen.value = false
  authStore.logout()
}
</script>

<template>
  <header class="sticky top-0 z-50 w-full border-b border-primary/40 bg-dark/95 backdrop-blur">
    <div class="page-container flex h-16 items-center justify-between !py-0">
      <div class="flex items-center gap-8">
        <RouterLink :to="homeRoute" class="text-lg font-bold text-primary">
          {{ t('common.appName') }}
        </RouterLink>

        <nav class="hidden items-center gap-5 md:flex">
          <RouterLink v-if="authStore.isStudent" to="/home" class="nav-link">{{ t('nav.home') }}</RouterLink>
          <RouterLink v-if="authStore.canActAsCoordinator" to="/coordinator" class="nav-link">{{ t('nav.students') }}</RouterLink>
          <RouterLink to="/settings" class="nav-link">{{ t('nav.settings') }}</RouterLink>
        </nav>
      </div>

      <div class="flex items-center gap-3">
        <!-- Avatar + dropdown -->
        <div ref="avatarRef" class="relative">
          <button
            type="button"
            class="flex h-10 w-10 items-center justify-center rounded-full bg-primary text-sm font-bold text-white transition hover:bg-primary-light hover:text-dark"
            :aria-expanded="dropdownOpen"
            @click="toggleDropdown"
          >
            {{ initials }}
          </button>

          <div
            v-if="dropdownOpen"
            class="absolute right-0 top-12 w-56 rounded-xl border border-primary/40 bg-dark p-3 text-sm text-light shadow-lg shadow-black/40"
          >
            <p class="font-semibold text-light">{{ displayName }}</p>
            <p class="truncate text-xs text-primary-light">{{ displayEmail }}</p>

            <div class="my-2 border-t border-white/10"></div>

            <!-- Language -->
            <button
              v-for="loc in locales.filter(l => l.code !== locale)"
              :key="loc.code"
              type="button"
              class="flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm text-light/80 transition hover:bg-white/5 hover:text-white"
              @click="setLocale(loc.code)"
            >
              <span :class="[loc.flag, 'text-base']" aria-hidden="true"></span>
              {{ loc.label }}
            </button>

            <!-- Theme toggle -->
            <button
              type="button"
              class="flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm text-light/80 transition hover:bg-white/5 hover:text-white"
              @click="toggleTheme"
            >
              <svg v-if="theme === 'dark'" class="h-4 w-4 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364-6.364l-.707.707M6.343 17.657l-.707.707M17.657 17.657l-.707-.707M6.343 6.343l-.707-.707M12 8a4 4 0 100 8 4 4 0 000-8z" />
              </svg>
              <svg v-else class="h-4 w-4 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z" />
              </svg>
              {{ theme === 'dark' ? t('common.lightTheme') : t('common.darkTheme') }}
            </button>

            <button
              v-if="authStore.isAdmin"
              type="button"
              class="flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm text-light/80 transition hover:bg-white/5 hover:text-white"
              @click="goAdmin"
            >
              <svg class="h-4 w-4 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              {{ t('admin.title') }}
            </button>

            <button
              type="button"
              class="flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm text-red-300 transition hover:bg-red-500/10"
              @click="logout"
            >
              <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
              </svg>
              {{ t('common.signOut') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </header>
</template>

<style scoped>
.nav-link {
  border-bottom: 2px solid transparent;
  color: var(--color-light);
  font-weight: 600;
  padding-bottom: 2px;
  transition: color 0.2s ease, border-color 0.2s ease;
}
.nav-link:hover {
  color: var(--color-primary-light);
}
.router-link-active {
  border-color: var(--color-primary);
  color: var(--color-primary);
}
</style>
