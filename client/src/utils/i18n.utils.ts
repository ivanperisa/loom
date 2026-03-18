import { useI18n } from 'vue-i18n'

export function localizedName(item: { name: string; nameEn?: string | null }): string {
  const { locale } = useI18n()
  return locale.value === 'en' && item.nameEn ? item.nameEn : item.name
}
