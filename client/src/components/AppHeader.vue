<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { useTheme } from '@/composables/useTheme'
import LocaleSwitcher from '@/components/common/LocaleSwitcher.vue'
import UserAvatar from '@/components/common/UserAvatar.vue'
import { ExternalLinkIcon, LogOutIcon, MoonIcon, SettingsIcon, SunIcon } from '@lucide/vue'

/** Someone on an access link: no account, so the menu offers sign-up instead of the account actions. */
const props = defineProps<{ guest?: boolean }>()

const authStore = useAuthStore()
const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const { theme, toggleTheme } = useTheme()

const dropdownOpen = ref(false)
const avatarRef = ref<HTMLElement | null>(null)

function onDocClick(e: MouseEvent) {
  if (!avatarRef.value?.contains(e.target as Node)) dropdownOpen.value = false
}

onMounted(() => document.addEventListener('mousedown', onDocClick))
onUnmounted(() => document.removeEventListener('mousedown', onDocClick))

const displayName = computed(() => props.guest ? t('exchangeAccess.guest') : authStore.user?.name?.trim() || t('common.user'))
const displayEmail = computed(() => props.guest ? t('exchangeAccess.readOnlyNotice') : authStore.user?.email?.trim() || t('common.na'))
const homeRoute = computed(() => props.guest ? '/' : authStore.canActAsCoordinator ? '/coordinator' : '/home')

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
    <div class="page-container flex h-16 items-center justify-between !py-0" :class="{ 'page-container--wide': route.meta.wide }">
      <div class="flex items-center gap-8">
        <RouterLink :to="homeRoute" class="text-lg font-bold text-primary-strong">
          {{ t('common.appName') }}
        </RouterLink>

        <nav v-if="!guest && authStore.isOnboarded" class="hidden items-center gap-5 md:flex">
          <RouterLink v-if="!guest && authStore.isStudent" to="/home" class="nav-link">{{ t('nav.home') }}</RouterLink>
          <RouterLink v-if="!guest && authStore.canActAsCoordinator" to="/coordinator" class="nav-link">{{ t('nav.students') }}</RouterLink>
          <RouterLink v-if="!guest" to="/settings" class="nav-link">{{ t('nav.settings') }}</RouterLink>
          <a
            href="https://ewp-dashboard.eu/"
            target="_blank"
            rel="noopener noreferrer"
            class="nav-link flex items-center gap-1"
          >
            {{ t('nav.erasmusDashboard') }}
            <ExternalLinkIcon class="h-3 w-3 opacity-60" />
          </a>
        </nav>
      </div>

      <div class="flex items-center gap-3">
        <!-- Avatar + dropdown -->
        <div ref="avatarRef" class="relative">
          <button
            type="button"
            class="rounded-full transition hover:ring-2 hover:ring-primary/40"
            :aria-expanded="dropdownOpen"
            aria-haspopup="true"
            :aria-label="displayName"
            @click="toggleDropdown"
          >
            <UserAvatar :name="displayName" :role="guest ? '' : authStore.role ?? ''" size="md" />
          </button>

          <div
            v-if="dropdownOpen"
            class="absolute right-0 top-12 w-56 rounded-xl border border-primary/40 bg-dark p-3 text-sm text-light shadow-lg shadow-black/40"
          >
            <p class="font-semibold text-light">{{ displayName }}</p>
            <p class="text-xs text-primary-text" :class="{ truncate: !guest }">{{ displayEmail }}</p>

            <div class="my-2 border-t border-hairline"></div>

            <!-- Language -->
            <LocaleSwitcher variant="list" />

            <!-- Theme toggle -->
            <button
              type="button"
              class="flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm text-light/80 transition hover:bg-fill-soft hover:text-light"
              @click="toggleTheme"
            >
              <SunIcon v-if="theme === 'dark'" class="h-4 w-4 text-primary-strong" />
              <MoonIcon v-else class="h-4 w-4 text-primary-strong" />
              {{ theme === 'dark' ? t('common.lightTheme') : t('common.darkTheme') }}
            </button>

            <button
              v-if="!guest && authStore.isAdmin"
              type="button"
              class="flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm text-light/80 transition hover:bg-fill-soft hover:text-light"
              @click="goAdmin"
            >
              <SettingsIcon class="h-4 w-4 text-primary-strong" />
              {{ t('admin.title') }}
            </button>

            <RouterLink
              v-if="guest"
              to="/"
              class="mt-1 flex w-full items-center justify-center rounded-lg bg-primary-strong px-2 py-1.5 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
            >
              {{ t('exchangeAccess.registerCta') }}
            </RouterLink>
            <button
              v-else
              type="button"
              class="flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm text-danger-text transition hover:bg-danger-fill"
              @click="logout"
            >
              <LogOutIcon class="h-4 w-4" />
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
