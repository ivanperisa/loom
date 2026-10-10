<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { AppLocale } from '@/i18n/index'
// Only the two flags the app shows (the full flag-icons set was ~4 MB of SVGs and 470 KB of CSS).
import hrFlag from '@/assets/flags/hr.svg'
import gbFlag from '@/assets/flags/gb.svg'

withDefaults(defineProps<{ variant?: 'compact' | 'list' }>(), { variant: 'compact' })

const { locale } = useI18n()

const locales: Array<{ code: AppLocale; flag: string; label: string }> = [
  { code: 'hr', flag: hrFlag, label: 'Hrvatski' },
  { code: 'en', flag: gbFlag, label: 'English' },
]

function setLocale(code: AppLocale) {
  locale.value = code
  localStorage.setItem('locale', code)
}
</script>

<template>
  <template v-for="loc in locales.filter((l) => l.code !== locale)" :key="loc.code">
    <button
      v-if="variant === 'compact'"
      type="button"
      class="flex h-9 items-center gap-1.5 rounded-lg px-2.5 text-xs font-medium text-light/60 transition hover:bg-fill hover:text-light"
      :aria-label="loc.label"
      :lang="loc.code"
      @click="setLocale(loc.code)"
    >
      <img :src="loc.flag" alt="" class="h-3 w-4 object-cover" />
      {{ loc.code.toUpperCase() }}
    </button>
    <button
      v-else
      type="button"
      class="flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm text-light/80 transition hover:bg-fill-soft hover:text-light"
      :lang="loc.code"
      @click="setLocale(loc.code)"
    >
      <img :src="loc.flag" alt="" class="h-3.5 w-[1.2rem] object-cover" />
      {{ loc.label }}
    </button>
  </template>
</template>
