<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { adminService, type CoordinatorRequestResponse, type CoordinatorWhitelistEntryResponse, type UserListResponse } from '@/services/admin.service'
import { useAuthStore } from '@/stores/auth.store'
import { userRole } from '@/utils/userRole'
import { ROLE_CHIP_CLASS } from '@/utils/roleColors'
import SearchInput from '@/components/common/SearchInput.vue'
import SearchableSelect from '@/components/common/SearchableSelect.vue'
import Pagination from '@/components/common/Pagination.vue'
import SortableHeader from '@/components/common/SortableHeader.vue'
import UserAvatar from '@/components/common/UserAvatar.vue'
import ErrorAlert from '@/components/common/ErrorAlert.vue'
import { useConfirm } from '@/composables/useConfirm'
import { useListQuery } from '@/composables/useListQuery'
import { useCoordinatorsQuery, useHomeInstitutionsQuery } from '@/queries/catalog.queries'
import { queryKeys } from '@/queries/keys'
import { describeApiError } from '@/utils/apiError'
import AdminEditUserModal from '@/components/admin/AdminEditUserModal.vue'
import AdminChangeRoleModal from '@/components/admin/AdminChangeRoleModal.vue'

const { t } = useI18n()
const { confirm } = useConfirm()
const auth = useAuthStore()
const queryClient = useQueryClient()

const newEmail = ref('')
const actionLoadingId = ref<number | null>(null)
const whitelistActionEmail = ref<string | null>(null)
const addingEmail = ref(false)
const errorMessage = ref<string | null>(null)

const editingUser = ref<UserListResponse | null>(null)
const roleChangeUser = ref<UserListResponse | null>(null)

// Users

const roleFilter = ref<string | null>(null)
const institutionFilter = ref<number | null>(null)
const registered = ref<boolean | null>(null)
/** The select works with names; the API filter is a boolean. */
const statusFilter = computed({
  get: () => (registered.value === null ? null : registered.value ? 'registered' : 'unregistered'),
  set: (value: string | null) => (registered.value = value === null ? null : value === 'registered'),
})

const userList = useListQuery<UserListResponse, { role: string | null; institutionId: number | null; registered: boolean | null }>({
  key: queryKeys.adminUsers,
  fetch: (params, signal) => adminService.getAllUsers(params, signal),
  filters: { role: roleFilter, institutionId: institutionFilter, registered },
  pageSize: 10,
  defaultSort: 'name',
  syncToUrl: 'users',
})
const users = userList.items
const search = userList.search

const institutionsQuery = useHomeInstitutionsQuery()
const coordinatorsQuery = useCoordinatorsQuery()

const roleOptions = computed(() => [
  { value: null, label: t('admin.users.allRoles') },
  ...[userRole.Student, userRole.Coordinator, userRole.Admin].map((r) => ({
    value: r as string,
    label: t(`admin.users.role.${r}`),
  })),
])

const institutionOptions = computed(() => [
  { value: null, label: t('admin.users.allInstitutions') },
  ...(institutionsQuery.data.value ?? []).map((i) => ({ value: i.id, label: i.name, sublabel: i.city ?? undefined })),
])

const statusOptions = computed(() => [
  { value: null, label: t('admin.users.allStatuses') },
  { value: 'registered', label: t('admin.users.status.registered') },
  { value: 'unregistered', label: t('admin.users.status.unregistered') },
])

function openEditDialog(user: UserListResponse) {
  editingUser.value = user
}

function openRoleDialog(user: UserListResponse) {
  roleChangeUser.value = user
}

/** A role or user change can affect every list here and the coordinator pickers. */
function refreshAfterUserChange() {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: queryKeys.adminUsers }),
    queryClient.invalidateQueries({ queryKey: queryKeys.coordinatorRequests }),
    queryClient.invalidateQueries({ queryKey: queryKeys.coordinators }),
  ])
}

function onUserSaved() {
  editingUser.value = null
  refreshAfterUserChange()
}

function onRoleSaved() {
  roleChangeUser.value = null
  refreshAfterUserChange()
}

// Coordinator requests

const requestList = useListQuery<CoordinatorRequestResponse>({
  key: queryKeys.coordinatorRequests,
  fetch: (params, signal) => adminService.getCoordinatorRequests(params, signal),
  pageSize: 10,
  defaultSort: 'name',
  syncToUrl: 'requests',
})

async function approve(userId: number) {
  actionLoadingId.value = userId
  try {
    await adminService.setUserRole(userId, userRole.Coordinator)
    await refreshAfterUserChange()
  } finally {
    actionLoadingId.value = null
  }
}

async function reject(userId: number) {
  actionLoadingId.value = userId
  try {
    await adminService.rejectCoordinatorRequest(userId)
    await refreshAfterUserChange()
  } finally {
    actionLoadingId.value = null
  }
}

// Coordinator whitelist

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
  <div class="space-y-6">

    <!-- Pending coordinator requests -->
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
              @click="approve(req.id)"
            >{{ t('admin.requests.approve') }}</button>
            <button
              type="button"
              class="rounded-lg border border-danger-text/35 px-4 py-1.5 text-xs font-medium text-danger-text transition hover:bg-danger-fill disabled:opacity-50"
              :disabled="actionLoadingId === req.id"
              @click="reject(req.id)"
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

    <!-- User management -->
    <section class="rounded-2xl border border-primary/20 bg-dark-2 p-6">
      <div class="mb-5 flex items-baseline gap-2">
        <h2 class="text-base font-semibold text-light">{{ t('admin.users.title') }}</h2>
        <span class="text-xs text-light/30">({{ userList.totalCount.value }})</span>
      </div>

      <div class="mb-4 flex flex-wrap gap-3">
        <SearchInput v-model="search" :placeholder="t('admin.users.searchPlaceholder')" class="min-w-[200px] flex-1" />
        <SearchableSelect
          v-model="institutionFilter"
          :options="institutionOptions"
          :placeholder="t('admin.users.allInstitutions')"
          :search-placeholder="t('admin.users.searchInstitution')"
          :no-results-label="t('admin.users.noInstitutionResults')"
          class="min-w-[180px] max-w-[280px] flex-none"
        />
        <SearchableSelect
          v-model="roleFilter"
          :options="roleOptions"
          :placeholder="t('admin.users.allRoles')"
          :searchable="false"
          class="min-w-[150px] max-w-[220px] flex-none"
        />
        <SearchableSelect
          v-model="statusFilter"
          :options="statusOptions"
          :placeholder="t('admin.users.allStatuses')"
          :searchable="false"
          class="min-w-[150px] max-w-[220px] flex-none"
        />
      </div>

      <div v-if="userList.isPending.value" class="space-y-2">
        <div v-for="i in 5" :key="i" class="h-14 animate-pulse rounded-xl bg-dark"></div>
      </div>

      <ErrorAlert v-else-if="userList.error.value" :error="userList.error.value" @retry="userList.refetch()" />

      <template v-else>
        <div class="overflow-x-auto rounded-xl border border-primary/20 bg-dark">
          <div class="min-w-[980px]">
            <div class="admin-user-grid gap-4 border-b border-primary/20 px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-light/40">
              <SortableHeader
                :label="t('admin.users.columns.user')"
                sort-key="name"
                :active-key="userList.sortKey.value"
                :dir="userList.sortDir.value"
                @sort="userList.toggleSort"
              />
              <SortableHeader
                :label="t('admin.users.columns.jmbag')"
                sort-key="jmbag"
                :active-key="userList.sortKey.value"
                :dir="userList.sortDir.value"
                @sort="userList.toggleSort"
              />
              <span>{{ t('admin.users.columns.institution') }}</span>
              <span>{{ t('admin.users.columns.mentor') }}</span>
              <span>{{ t('admin.users.columns.coordinator') }}</span>
              <SortableHeader
                :label="t('admin.users.columns.role')"
                sort-key="role"
                :active-key="userList.sortKey.value"
                :dir="userList.sortDir.value"
                @sort="userList.toggleSort"
              />
              <span>{{ t('admin.users.columns.status') }}</span>
              <span></span>
            </div>

            <p v-if="users.length === 0" class="px-4 py-6 text-sm text-light/40">{{ t('admin.users.empty') }}</p>

            <div v-else class="divide-y divide-hairline-soft">
              <div
                v-for="u in users"
                :key="u.id"
                class="admin-user-grid gap-4 px-4 py-3 text-sm transition hover:bg-dark-2"
              >
                <!-- User -->
                <div class="flex min-w-0 items-center gap-3">
                  <UserAvatar :name="u.name" :role="u.role" />
                  <div class="min-w-0">
                    <p class="truncate font-medium text-light">{{ u.name }}</p>
                    <p class="truncate text-xs text-light/40">{{ u.email || '—' }}</p>
                  </div>
                </div>

                <span class="truncate text-light/70">{{ u.jmbag || '—' }}</span>

                <!-- Institution: name may wrap to two lines, city is secondary -->
                <div class="min-w-0">
                  <p class="line-clamp-2 text-light/70">{{ u.institutionName || '—' }}</p>
                  <p v-if="u.institutionCity" class="truncate text-xs text-light/30">{{ u.institutionCity }}</p>
                </div>

                <span class="truncate text-light/70">{{ u.mentor || '—' }}</span>
                <span class="truncate text-light/70">{{ u.coordinatorName || '—' }}</span>

                <!-- Role -->
                <span>
                  <span
                    class="whitespace-nowrap rounded-full border px-2.5 py-0.5 text-[11px] font-medium"
                    :class="ROLE_CHIP_CLASS[u.role]"
                  >{{ t(`admin.users.role.${u.role}`) }}</span>
                </span>

                <div class="min-w-0">
                  <span class="flex items-center gap-2 text-xs">
                    <span
                      class="h-1.5 w-1.5 shrink-0 rounded-full"
                      :class="u.email ? 'bg-success' : 'bg-warning'"
                    ></span>
                    <span :class="u.email ? 'text-light/70' : 'text-light/40'">
                      {{ u.email ? t('admin.users.status.registered') : t('admin.users.status.unregistered') }}
                    </span>
                  </span>
                  <span
                    v-if="u.coordinatorRequestStatus === 'Pending'"
                    class="mt-1 inline-block whitespace-nowrap rounded-full border border-warning-text/35 bg-warning-fill px-2 py-0.5 text-[10px] font-medium text-warning-text"
                  >{{ t('admin.users.coordinatorRequest') }}</span>
                </div>

                <!-- Actions -->
                <div class="flex items-center justify-end gap-1">
                  <button
                    v-if="u.id !== auth.user?.id"
                    type="button"
                    class="flex h-7 w-7 items-center justify-center rounded-lg text-light/40 transition hover:bg-fill hover:text-light"
                    :title="t('admin.users.changeRole')"
                    @click="openRoleDialog(u)"
                  >
                    <!-- person with a badge -->
                    <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 7a3 3 0 11-6 0 3 3 0 016 0zM3 20a6 6 0 0112 0M18 4.5l2.5 1V8c0 1.6-1.1 3-2.5 3.5-1.4-.5-2.5-1.9-2.5-3.5V5.5l2.5-1z" />
                    </svg>
                  </button>
                  <button
                    type="button"
                    class="flex h-7 w-7 items-center justify-center rounded-lg text-light/40 transition hover:bg-fill hover:text-light"
                    :title="t('admin.users.editUser')"
                    @click="openEditDialog(u)"
                  >
                    <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <Pagination
          :page="userList.page.value"
          :total-pages="userList.totalPages.value"
          :total="userList.totalCount.value"
          :per-page="userList.pageSize"
          @update:page="userList.page.value = $event"
        />
      </template>
    </section>

    <!-- Coordinator whitelist -->
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

    <!-- Edit user modal -->
    <AdminEditUserModal
      v-if="editingUser"
      :user="editingUser"
      :coordinators="coordinatorsQuery.data.value ?? []"
      :institutions="institutionsQuery.data.value ?? []"
      @close="editingUser = null"
      @saved="onUserSaved"
    />

    <!-- Change role modal -->
    <AdminChangeRoleModal
      v-if="roleChangeUser"
      :user="roleChangeUser"
      @close="roleChangeUser = null"
      @saved="onRoleSaved"
    />
  </div>
</template>

<style scoped>
.admin-user-grid {
  display: grid;
  grid-template-columns:
    minmax(190px, 1.5fr) 100px minmax(150px, 1.6fr) minmax(90px, 0.9fr)
    minmax(110px, 1.1fr) minmax(105px, 0.7fr) minmax(105px, 0.7fr) 68px;
  align-items: center;
}
</style>
