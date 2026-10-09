<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { adminService, type CoordinatorRequestResponse } from '@/services/admin.service'
import { userRole } from '@/utils/userRole'
import Pagination from '@/components/common/Pagination.vue'
import ErrorAlert from '@/components/common/ErrorAlert.vue'
import { useListQuery } from '@/composables/useListQuery'
import { useRefreshAfterUserChange } from '@/queries/admin.queries'
import { queryKeys } from '@/queries/keys'

/** Students asking to become coordinators: approve (role change) or reject. */
const { t } = useI18n()
const refreshAfterUserChange = useRefreshAfterUserChange()
const actionLoadingId = ref<number | null>(null)

const requestList = useListQuery<CoordinatorRequestResponse>({
  key: queryKeys.coordinatorRequests,
  fetch: (params, signal) => adminService.getCoordinatorRequests(params, signal),
  pageSize: 10,
  defaultSort: 'name',
  syncToUrl: 'requests',
})

async function decide(userId: number, approve: boolean) {
  actionLoadingId.value = userId
  try {
    if (approve) await adminService.setUserRole(userId, userRole.Coordinator)
    else await adminService.rejectCoordinatorRequest(userId)
    await refreshAfterUserChange()
  } finally {
    actionLoadingId.value = null
  }
}
</script>

<template>
<section class="rounded-2xl border border-primary/20 bg-dark-2 p-6">
  <h2 class="mb-5 text-base font-semibold text-light">{{ t('admin.requests.title') }}</h2>

  <div v-if="requestList.isPending.value" class="space-y-3">
    <div v-for="i in 2" :key="i" class="h-14 animate-pulse rounded-xl bg-dark"></div>
  </div>
  <ErrorAlert v-else-if="requestList.error.value" :error="requestList.error.value" @retry="requestList.refetch()" />
  <p v-else-if="requestList.items.value.length === 0" class="text-sm text-light/50">
    {{ t('admin.requests.empty') }}
  </p>
  <div v-else class="space-y-2">
    <div
      v-for="req in requestList.items.value"
      :key="req.id"
      class="flex items-center justify-between rounded-xl bg-dark px-5 py-3"
    >
      <div>
        <p class="text-sm font-medium text-light">{{ req.name }}</p>
        <p class="text-xs text-light/50">{{ req.email }}<template v-if="req.institutionName"> · {{ req.institutionName }}</template></p>
      </div>
      <div class="flex gap-2">
        <button
          type="button"
          class="rounded-lg bg-primary-strong px-4 py-1.5 text-xs font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
          :disabled="actionLoadingId === req.id"
          @click="decide(req.id, true)"
        >{{ t('admin.requests.approve') }}</button>
        <button
          type="button"
          class="rounded-lg border border-danger-text/35 px-4 py-1.5 text-xs font-medium text-danger-text transition hover:bg-danger-fill disabled:opacity-50"
          :disabled="actionLoadingId === req.id"
          @click="decide(req.id, false)"
        >{{ t('admin.requests.reject') }}</button>
      </div>
    </div>
  </div>
  <Pagination
    :page="requestList.page.value"
    :total-pages="requestList.totalPages.value"
    :total="requestList.totalCount.value"
    :per-page="requestList.pageSize"
    @update:page="requestList.page.value = $event"
  />
</section>
</template>
