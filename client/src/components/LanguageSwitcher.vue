<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AppLocale } from '@/i18n/index'
import '@/styles/language-switcher.css'

const props = withDefaults(defineProps<{ variant?: 'light' | 'dark' }>(), { variant: 'light' })

const { t, locale } = useI18n()
const isOpen = ref(false)
const rootElement = ref<HTMLElement | null>(null)

const localeItems: Array<{ code: AppLocale; flagClass: string }> = [
  { code: 'hr', flagClass: 'fi fi-hr' },
  { code: 'en', flagClass: 'fi fi-gb' }
]
const defaultLocaleItem = localeItems[0] as { code: AppLocale; flagClass: string }

function setLocale(nextLocale: AppLocale) {
  locale.value = nextLocale
  localStorage.setItem('locale', nextLocale)
  isOpen.value = false
}

const currentLocaleItem = computed(
  () => localeItems.find((item) => item.code === locale.value) ?? defaultLocaleItem
)

function toggleMenu() {
  isOpen.value = !isOpen.value
}

function closeMenu() {
  isOpen.value = false
}

function handleDocumentClick(event: MouseEvent) {
  if (!rootElement.value) {
    return
  }

  if (!rootElement.value.contains(event.target as Node)) {
    closeMenu()
  }
}

function handleEscape(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    closeMenu()
  }
}

onMounted(() => {
  document.addEventListener('click', handleDocumentClick)
  document.addEventListener('keydown', handleEscape)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handleDocumentClick)
  document.removeEventListener('keydown', handleEscape)
})
</script>

<template>
  <section ref="rootElement" class="language-switcher variant-dark" :class="{ 'variant-dark': props.variant === 'dark' }" :aria-label="t('common.language')">
    <button
      type="button"
      class="dropdown-button"
      :aria-expanded="isOpen"
      aria-haspopup="listbox"
      :aria-label="t('languageSwitcher.dropdownLabel')"
      @click="toggleMenu"
    >
      <span class="flag" :class="currentLocaleItem.flagClass" aria-hidden="true"></span>
      <span>{{ t(`languageSwitcher.locales.${currentLocaleItem.code}`) }}</span>
    </button>

    <ul v-if="isOpen" class="dropdown-list" role="listbox">
      <li v-for="item in localeItems" :key="item.code">
        <button
          type="button"
          class="dropdown-item"
          :class="{ active: locale === item.code }"
          @click="setLocale(item.code)"
        >
          <span class="flag" :class="item.flagClass" aria-hidden="true"></span>
          <span>{{ t(`languageSwitcher.locales.${item.code}`) }}</span>
        </button>
      </li>
    </ul>
  </section>
</template>
