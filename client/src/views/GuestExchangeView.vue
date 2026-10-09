<script setup lang="ts">
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import ExchangeDetailPanel from '@/components/exchange/ExchangeDetailPanel.vue'
import ThemeToggleButton from '@/components/common/ThemeToggleButton.vue'
import LocaleSwitcher from '@/components/common/LocaleSwitcher.vue'

const route = useRoute()
const { t } = useI18n()

const exchangeId = route.params.exchangeId as string
</script>

<template>
  <main class="min-h-screen bg-dark">
    <header class="sticky top-0 z-50 w-full border-b border-primary/40 bg-dark/95 backdrop-blur">
      <div class="page-container flex h-16 items-center justify-between !py-0">
        <RouterLink to="/" class="text-lg font-bold text-primary-strong">{{ t('common.appName') }}</RouterLink>

        <div class="flex items-center gap-3">
          <ThemeToggleButton />
          <LocaleSwitcher />

          <RouterLink
            to="/"
            class="rounded-lg bg-primary-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
          >
            {{ t('exchangeAccess.registerCta') }}
          </RouterLink>
        </div>
      </div>
    </header>

    <section class="page-container page-container--wide">
      <p class="mb-4 text-center text-xs text-light/40">{{ t('exchangeAccess.readOnlyNotice') }}</p>
      <ExchangeDetailPanel :exchange-id="exchangeId" guest />
    </section>
  </main>
</template>
