<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import LanguageSwitcher from '@/components/LanguageSwitcher.vue'

const authStore = useAuthStore()
const { t } = useI18n()

const displayName = computed(() => authStore.name?.trim() || t('common.user'))
const displayEmail = computed(() => authStore.email?.trim() || t('common.na'))
const isCoordinator = computed(() => authStore.role === 'Coordinator')
const exchangeLabel = computed(() => isCoordinator.value ? t('nav.students') : t('nav.exchange'))
const initials = computed(() => {
  const parts = displayName.value
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((value) => value[0]?.toUpperCase() ?? '')
    .join('')

  return parts || 'U'
})


</script>

<template>
  <header class="sticky top-0 z-50 w-full border-b border-[#218CD9]/40 bg-[#071C2C]/95 backdrop-blur">
    <div class="mx-auto flex h-16 w-full max-w-7xl items-center justify-between px-4 sm:px-6">
      <div class="flex items-center gap-8">
        <RouterLink to="/home" class="flex items-center gap-2">
          <svg viewBox="0 0 24 24" class="h-7 w-7 text-[#218CD9]" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.8" />
            <path d="M7 14h3l2-4 2 6 3-4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <span class="text-lg font-bold text-white">{{ t('common.appName') }}</span>
        </RouterLink>

        <nav class="hidden items-center gap-5 md:flex">
          <RouterLink to="/home" class="nav-link">{{ t('nav.home') }}</RouterLink>
          <RouterLink to="/settings" class="nav-link">{{ t('nav.settings') }}</RouterLink>
          <RouterLink to="/exchange" class="nav-link">{{ exchangeLabel }}</RouterLink>
          <RouterLink to="/history" class="nav-link">{{ t('nav.history') }}</RouterLink>
        </nav>
      </div>

      <div class="flex items-center gap-3">
        <LanguageSwitcher variant="dark" />

        <div class="group relative">
          <button
            type="button"
            class="flex h-10 w-10 items-center justify-center rounded-full bg-[#218CD9] text-sm font-bold text-white"
          >
            {{ initials }}
          </button>
          <div
            class="invisible absolute right-0 top-12 w-56 rounded-xl border border-[#218CD9]/40 bg-[#071C2C] p-3 text-sm text-[#CAE4F7] opacity-0 shadow-lg shadow-black/40 transition group-hover:visible group-hover:opacity-100 group-focus-within:visible group-focus-within:opacity-100"
          >
            <p class="font-semibold text-white">{{ displayName }}</p>
            <p class="truncate text-[#8AC4ED]">{{ displayEmail }}</p>
          </div>
        </div>

        <button
          type="button"
          class="text-sm font-semibold text-[#CAE4F7] transition hover:text-red-300"
          @click="authStore.logout()"
        >
          {{ t('common.signOut') }}
        </button>
      </div>
    </div>
  </header>
</template>

<style scoped>
.nav-link {
  border-bottom: 2px solid transparent;
  color: #cae4f7;
  font-weight: 600;
  padding-bottom: 2px;
  transition: color 0.2s ease, border-color 0.2s ease;
}

.nav-link:hover {
  color: #8ac4ed;
}

.router-link-active {
  border-color: #218cd9;
  color: #218cd9;
}
</style>
