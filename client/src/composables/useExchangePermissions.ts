import { computed } from 'vue'
import { useExchangeStore } from '@/stores/exchange.store'
import { useAuthStore } from '@/stores/auth.store'
import { documentStatus } from '@/utils/documentStatus'

export function useExchangePermissions() {
  const exchangeStore = useExchangeStore()
  const authStore = useAuthStore()

  const isCoordinator = computed(
    () => exchangeStore.exchange?.coordinatorId === authStore.user?.id || authStore.isAdmin,
  )
  const isApproved = computed(
    () => exchangeStore.serverLearningAgreement?.status === documentStatus.Approved,
  )
  const isEditable = computed(
    () => exchangeStore.serverLearningAgreement?.status === documentStatus.Draft,
  )

  return { isCoordinator, isApproved, isEditable }
}
