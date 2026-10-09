import type en from './locales/en'

/** English is the reference: Croatian must have exactly the same keys (`satisfies MessageSchema` in hr.ts). */
export type MessageSchema = typeof en

declare module 'vue-i18n' {
  // Key completion for t('...') everywhere. (Declaration merging needs the interface form.)
  // eslint-disable-next-line @typescript-eslint/no-empty-object-type
  export interface DefineLocaleMessage extends MessageSchema {}
}
