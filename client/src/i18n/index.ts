import { createI18n } from 'vue-i18n'
import en from './locales/en'
import hr from './locales/hr'
import './schema'

export const supportedLocales = ['hr', 'en'] as const
export type AppLocale = (typeof supportedLocales)[number]

const savedLocale = (localStorage.getItem('locale') ?? 'hr') as AppLocale

export const i18n = createI18n({
  legacy: false,
  locale: savedLocale,
  fallbackLocale: 'en',
  messages: { en, hr }
})
