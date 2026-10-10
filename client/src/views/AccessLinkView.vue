<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { accessService } from '@/services/access.service'
import { useAuthStore } from '@/stores/auth.store'
import { useNotification } from '@/composables/useNotification'
import type { AccessLinkPreviewResponse } from '@/types/exchange.types'
import ThemeToggleButton from '@/components/common/ThemeToggleButton.vue'
import LocaleSwitcher from '@/components/common/LocaleSwitcher.vue'

type State = 'opening' | 'invalid' | 'claim' | 'notForYou'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const { notifySuccess, notifyError } = useNotification()

// Read once: every successful path replaces the URL, so the token leaves the address bar and history.
const token = route.params.token as string
const state = ref<State>('opening')
const preview = ref<AccessLinkPreviewResponse | null>(null)
const claiming = ref(false)

async function openAsGuest() {
  const res = await accessService.openSession(token)
  await router.replace(`/guest/exchange/${res.data.exchangeGuid}`)
}

async function openSignedIn() {
  const res = await accessService.preview(token)
  preview.value = res.data
  if (res.data.hasAccess) await router.replace(`/exchange/${res.data.exchangeGuid}`)
  else state.value = res.data.canClaim ? 'claim' : 'notForYou'
}

async function claim() {
  claiming.value = true
  try {
    const res = await accessService.claim(token)
    await authStore.init(true)
    notifySuccess(t('exchangeAccess.claimed'))
    await router.replace(`/exchange/${res.data.exchangeGuid}`)
  } catch {
    notifyError(t('exchangeAccess.invalidTitle'))
    state.value = 'invalid'
  } finally {
    claiming.value = false
  }
}

onMounted(async () => {
  await authStore.init()
  try {
    if (authStore.isLoggedIn) await openSignedIn()
    else await openAsGuest()
  } catch {
    state.value = 'invalid'
  }
})
</script>

<template>
  <main class="flex min-h-screen flex-col bg-dark text-light">
    <header class="flex h-16 items-center justify-between border-b border-primary/40 px-6">
      <RouterLink to="/" class="text-lg font-bold text-primary-strong">{{ t('common.appName') }}</RouterLink>
      <div class="flex items-center gap-3">
        <ThemeToggleButton />
        <LocaleSwitcher />
      </div>
    </header>

    <section class="flex flex-1 items-center justify-center px-4 py-12">
      <div class="w-full max-w-lg rounded-2xl border border-hairline bg-dark p-8 shadow-lg" aria-live="polite">
        <p v-if="state === 'opening'" class="text-center text-sm text-light/60">{{ t('exchangeAccess.opening') }}</p>

        <template v-else-if="state === 'claim' && preview">
          <h1 class="text-xl font-bold">{{ t('exchangeAccess.claimTitle') }}</h1>
          <p class="mt-3 text-sm leading-6 text-light/70">
            {{ t('exchangeAccess.claimBody', { student: preview.studentName, partner: preview.partnerInstitutionName, year: preview.academicYear }) }}
          </p>
          <div class="mt-6 flex flex-wrap justify-end gap-3">
            <RouterLink to="/home" class="rounded-lg px-4 py-2 text-sm font-medium text-light/70 hover:text-light">
              {{ t('exchangeAccess.goHome') }}
            </RouterLink>
            <button
              type="button"
              class="rounded-lg bg-primary-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
              :disabled="claiming"
              @click="claim"
            >
              {{ t('exchangeAccess.claimButton') }}
            </button>
          </div>
        </template>

        <template v-else>
          <h1 class="text-xl font-bold">{{ t('exchangeAccess.invalidTitle') }}</h1>
          <p class="mt-3 text-sm leading-6 text-light/70">
            {{ state === 'notForYou' ? t('exchangeAccess.notForYou') : t('exchangeAccess.invalidBody') }}
          </p>
          <div class="mt-6 flex justify-end">
            <RouterLink to="/" class="rounded-lg bg-primary-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark">
              {{ t('exchangeAccess.goHome') }}
            </RouterLink>
          </div>
        </template>
      </div>
    </section>
  </main>
</template>
