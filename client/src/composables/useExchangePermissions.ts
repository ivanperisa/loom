import { computed } from 'vue'
import { useExchangeStore } from '@/stores/exchange.store'
import { useAuthStore } from '@/stores/auth.store'
import { documentStatus } from '@/utils/documentStatus'

export function useExchangePermissions() {
  const exchangeStore = useExchangeStore()
  const authStore = useAuthStore()

  const isCoordinator = computed(
    () => !exchangeStore.guestMode && exchangeStore.exchange?.coordinatorId === authStore.user?.id,
  )
  const isApproved = computed(
    () => exchangeStore.serverLearningAgreement?.status === documentStatus.Approved,
  )
  /** Final recognition started: the LA and table 1 are frozen for good. */
  const isConcluded = computed(() => exchangeStore.serverLearningAgreement?.isConcluded ?? false)
  /** The LA can be edited: a draft, before final recognition. */
  const isEditable = computed(
    () => exchangeStore.serverLearningAgreement?.status === documentStatus.Draft && !isConcluded.value,
  )

  return { isCoordinator, isApproved, isConcluded, isEditable }
}
