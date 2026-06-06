import { ref, watch } from 'vue'

type Theme = 'dark' | 'light'

const theme = ref<Theme>((localStorage.getItem('theme') as Theme) ?? 'dark')

watch(theme, (t) => {
  document.documentElement.setAttribute('data-theme', t)
  localStorage.setItem('theme', t)
}, { immediate: true })

export function useTheme() {
  function toggleTheme() {
    theme.value = theme.value === 'dark' ? 'light' : 'dark'
  }
  return { theme, toggleTheme }
}
