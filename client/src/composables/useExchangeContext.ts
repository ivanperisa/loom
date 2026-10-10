import { computed, inject, provide, type ComputedRef, type InjectionKey, type Ref } from 'vue'
import { useAuthStore } from '@/stores/auth.store'
import { documentStatus } from '@/utils/documentStatus'
import { useExchangeQuery, useLearningAgreementQuery, useRecognitionQuery } from '@/queries/exchange.queries'
import type { ExchangeResponse } from '@/types/exchange.types'
import type { LearningAgreementResponse } from '@/types/learningAgreement.types'
import type { RecognitionResponse } from '@/types/recognition.types'

export interface ExchangeContext {
  exchangeId: Ref<string>
  /** Opened through an access link, without an account. */
  guest: Ref<boolean>
  exchange: Ref<ExchangeResponse | undefined>
  learningAgreement: Ref<LearningAgreementResponse | undefined>
  recognition: Ref<RecognitionResponse | undefined>
  exchangeQuery: ReturnType<typeof useExchangeQuery>
  learningAgreementQuery: ReturnType<typeof useLearningAgreementQuery>
  recognitionQuery: ReturnType<typeof useRecognitionQuery>
  /** The signed-in user is this exchange's coordinator. */
  isCoordinator: ComputedRef<boolean>
  isApproved: ComputedRef<boolean>
  /** Final recognition started: the LA and table 1 are frozen for good. */
  isConcluded: ComputedRef<boolean>
  /** The LA can be edited: a draft, before final recognition. */
  isEditable: ComputedRef<boolean>
}

const key: InjectionKey<ExchangeContext> = Symbol('exchange')

/** Called once by the exchange page; every panel below reads the same queries through `useExchangeContext`. */
export function provideExchangeContext(exchangeId: Ref<string>, guest: Ref<boolean>): ExchangeContext {
  const auth = useAuthStore()
  const exchangeQuery = useExchangeQuery(exchangeId)
  const learningAgreementQuery = useLearningAgreementQuery(exchangeId)
  const recognitionQuery = useRecognitionQuery(exchangeId)
  const learningAgreement = learningAgreementQuery.data

  const isConcluded = computed(() => learningAgreement.value?.isConcluded ?? false)
  const context: ExchangeContext = {
    exchangeId,
    guest,
    exchange: exchangeQuery.data,
    learningAgreement,
    recognition: recognitionQuery.data,
    exchangeQuery,
    learningAgreementQuery,
    recognitionQuery,
    isCoordinator: computed(() => !guest.value && exchangeQuery.data.value?.coordinatorId === auth.user?.id),
    isApproved: computed(() => learningAgreement.value?.status === documentStatus.Approved),
    isConcluded,
    isEditable: computed(() => learningAgreement.value?.status === documentStatus.Draft && !isConcluded.value),
  }
  provide(key, context)
  return context
}

export function useExchangeContext(): ExchangeContext {
  const context = inject(key)
  if (!context) throw new Error('useExchangeContext() needs an exchange page above it (provideExchangeContext).')
  return context
}
