<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { adminService, type CoordinatorWhitelistEntryResponse } from '@/services/admin.service'
import Pagination from '@/components/common/Pagination.vue'
import ErrorAlert from '@/components/common/ErrorAlert.vue'
import { useConfirm } from '@/composables/useConfirm'
import { useListQuery } from '@/composables/useListQuery'
import { queryKeys } from '@/queries/keys'
import { describeApiError } from '@/utils/apiError'

/** Emails that become coordinators on their first login. */
const { t } = useI18n()
const { confirm } = useConfirm()
const queryClient = useQueryClient()

const newEmail = ref('')
const addingEmail = ref(false)
const errorMessage = ref<string | null>(null)
const whitelistActionEmail = ref<string | null>(null)

const whitelistList = useListQuery<CoordinatorWhitelistEntryResponse>({
  key: queryKeys.coordinatorWhitelist,
  fetch: (params, signal) => adminService.getCoordinatorWhitelist(params, signal),
  pageSize: 10,
  defaultSort: 'email',
  syncToUrl: 'whitelist',
})

const refreshWhitelist = () => queryClient.invalidateQueries({ queryKey: queryKeys.coordinatorWhitelist })

async function addEmail() {
  errorMessage.value = null
  const email = newEmail.value.trim()
  if (!email) return
  addingEmail.value = true
  try {
    await adminService.addToWhitelist(email)
    newEmail.value = ''
    await refreshWhitelist()
  } catch (e: unknown) {
    errorMessage.value = describeApiError(e).message
  } finally {
    addingEmail.value = false
  }
}

async function removeEmail(email: string) {
  if (!await confirm({ title: t('admin.whitelist.removeConfirm') })) return
  whitelistActionEmail.value = email
  try {
    await adminService.removeFromWhitelist(email)
    await refreshWhitelist()
  } finally {
    whitelistActionEmail.value = null
  }
}
</script>

<template>
<section class="rounded-2xl border border-primary/20 bg-dark-2 p-6">
  <h2 class="mb-1 text-base font-semibold text-light">{{ t('admin.whitelist.title') }}</h2>
  <p class="mb-5 text-sm text-light/50">{{ t('admin.whitelist.description') }}</p>

  <div class="mb-5 flex gap-3">
    <input
      v-model="newEmail"
      type="email"
      :placeholder="t('admin.whitelist.emailPlaceholder')"
      class="flex-1 rounded-xl border border-primary/20 bg-dark px-4 py-2.5 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
      @keydown.enter.prevent="addEmail"
    />
    <button
      type="button"
      class="rounded-xl bg-primary-strong px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
      :disabled="addingEmail || !newEmail.trim()"
      @click="addEmail"
    >{{ addingEmail ? t('common.loading') : t('admin.whitelist.add') }}</button>
  </div>

  <p v-if="errorMessage" class="mb-4 rounded-xl border border-danger-text/35 bg-danger-fill px-4 py-3 text-sm text-danger-text">
    {{ errorMessage }}
  </p>

  <div v-if="whitelistList.isPending.value" class="space-y-2">
    <div v-for="i in 3" :key="i" class="h-12 animate-pulse rounded-xl bg-dark"></div>
  </div>
  <ErrorAlert v-else-if="whitelistList.error.value" :error="whitelistList.error.value" @retry="whitelistList.refetch()" />
  <p v-else-if="whitelistList.items.value.length === 0" class="text-sm text-light/50">{{ t('admin.whitelist.empty') }}</p>
  <div v-else class="divide-y divide-hairline-soft rounded-xl bg-dark">
    <div
      v-for="entry in whitelistList.items.value"
      :key="entry.id"
      class="flex items-center justify-between px-4 py-3"
    >
      <div>
        <p class="text-sm font-medium text-light">{{ entry.email }}</p>
      </div>
      <button
        type="button"
        class="flex h-7 w-7 items-center justify-center rounded-lg border border-danger/20 text-danger transition hover:border-danger-text/50 hover:bg-danger-fill hover:text-danger-text disabled:opacity-40"
        :disabled="whitelistActionEmail === entry.email"
        :title="t('admin.whitelist.remove')"
        @click="removeEmail(entry.email)"
      >
        <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
    </div>
  </div>
  <Pagination
    :page="whitelistList.page.value"
    :total-pages="whitelistList.totalPages.value"
    :total="whitelistList.totalCount.value"
    :per-page="whitelistList.pageSize"
    @update:page="whitelistList.page.value = $event"
  />
</section>
</template>
