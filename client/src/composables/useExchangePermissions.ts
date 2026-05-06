import { computed } from 'vue'
import { useExchangeStore } from '@/stores/exchange.store'
import { useAuthStore } from '@/stores/auth.store'

export function useExchangePermissions() {
  const exchangeStore = useExchangeStore()
  const authStore = useAuthStore()

  const isCoordinator = computed(
    () =>
      exchangeStore.exchange?.coordinatorId === authStore.user?.id ||
      authStore.isAdmin,
  )
  const isApproved = computed(() => exchangeStore.serverLearningAgreement?.status === 'Approved')
  const isEditable = computed(() => exchangeStore.serverLearningAgreement?.status === 'Draft')

  return { isCoordinator, isApproved, isEditable }
}
