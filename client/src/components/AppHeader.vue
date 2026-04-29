<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import LanguageSwitcher from '@/components/LanguageSwitcher.vue'

const authStore = useAuthStore()
const { t } = useI18n()

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
          <RouterLink v-if="authStore.isAdmin" to="/admin" class="nav-link">{{ t('nav.admin') }}</RouterLink>
          <RouterLink to="/settings" class="nav-link">{{ t('nav.settings') }}</RouterLink>
        </nav>
      </div>

      <div class="flex items-center gap-3">
        <LanguageSwitcher variant="dark" />

        <div class="group relative">
          <button
            type="button"
            class="flex h-10 w-10 items-center justify-center rounded-full bg-primary text-sm font-bold text-white"
          >
            {{ initials }}
          </button>
          <div
            class="invisible absolute right-0 top-12 w-56 rounded-xl border border-primary/40 bg-dark p-3 text-sm text-light opacity-0 shadow-lg shadow-black/40 transition group-hover:visible group-hover:opacity-100 group-focus-within:visible group-focus-within:opacity-100"
          >
            <p class="font-semibold text-white">{{ displayName }}</p>
            <p class="truncate text-primary-light">{{ displayEmail }}</p>
          </div>
        </div>

        <button
          type="button"
          class="text-sm font-semibold text-light transition hover:text-red-300"
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
