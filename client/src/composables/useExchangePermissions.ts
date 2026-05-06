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
  const isApproved = computed(() => exchangeStore.exchange?.status === 'Approved')
  const isEditable = computed(() => exchangeStore.exchange?.status === 'Draft')

  return { isCoordinator, isApproved, isEditable }
}
