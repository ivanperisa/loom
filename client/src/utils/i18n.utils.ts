import { useI18n } from 'vue-i18n'

export function localizedName(item: { name: string; nameHr?: string | null }): string {
  const { locale } = useI18n()
  return locale.value === 'hr' && item.nameHr ? item.nameHr : item.name
}

export function localizedHomeName(item: { name: string; nameEn?: string | null }): string {
  const { locale } = useI18n()
  return locale.value === 'en' && item.nameEn ? item.nameEn : item.name
}
