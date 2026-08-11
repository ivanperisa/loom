<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { adminService, type CoordinatorRequestResponse, type CoordinatorWhitelistEntryResponse, type UserListResponse } from '@/services/admin.service'
import { coordinatorService } from '@/services/coordinator.service'
import { institutionService } from '@/services/institution.service'
import { userRole } from '@/utils/userRole'
import SearchInput from '@/components/common/SearchInput.vue'
import { useConfirm } from '@/composables/useConfirm'
import AdminEditUserModal from '@/components/admin/AdminEditUserModal.vue'
import type { AuthMeResponse } from '@/types/auth.types'
import type { InstitutionResponse } from '@/types/institution.types'

const { t } = useI18n()
const { confirm } = useConfirm()

const requests = ref<CoordinatorRequestResponse[]>([])
const whitelist = ref<CoordinatorWhitelistEntryResponse[]>([])
const users = ref<UserListResponse[]>([])
const newEmail = ref('')
const coordinatorsList = ref<AuthMeResponse[]>([])
const institutionsList = ref<InstitutionResponse[]>([])

const loadingRequests = ref(true)
const loadingWhitelist = ref(true)
const loadingUsers = ref(true)
const actionLoadingId = ref<string | null>(null)
const whitelistActionEmail = ref<string | null>(null)
const addingEmail = ref(false)
const errorMessage = ref<string | null>(null)
const userActionId = ref<string | null>(null)
const openMenuId = ref<string | null>(null)

const editingUser = ref<UserListResponse | null>(null)

function openEditDialog(user: UserListResponse) {
  editingUser.value = user
  openMenuId.value = null
}

function onUserSaved(updated: UserListResponse) {
  const idx = users.value.findIndex(u => u.id === updated.id)
  if (idx !== -1) users.value[idx] = updated
  editingUser.value = null
}

const admins = computed(() => users.value.filter(u => u.role === userRole.Admin))
const coordinators = computed(() => users.value.filter(u => u.role === userRole.Coordinator))
const allStudents = computed(() => users.value.filter(u => u.role === userRole.Student))

const studentSearch = ref('')
const studentPage = ref(1)
const STUDENTS_PER_PAGE = 10

const filteredStudents = computed(() => {
  const q = studentSearch.value.trim().toLowerCase()
  if (!q) return allStudents.value
  return allStudents.value.filter(u =>
    u.name.toLowerCase().includes(q) ||
    u.email.toLowerCase().includes(q) ||
    (u.jmbag && u.jmbag.includes(q))
  )
})

const totalStudentPages = computed(() => Math.max(1, Math.ceil(filteredStudents.value.length / STUDENTS_PER_PAGE)))

const students = computed(() => {
  const start = (studentPage.value - 1) * STUDENTS_PER_PAGE
  return filteredStudents.value.slice(start, start + STUDENTS_PER_PAGE)
})

function onStudentSearch() { studentPage.value = 1 }

function toggleMenu(userId: string) {
  openMenuId.value = openMenuId.value === userId ? null : userId
}

function handleOutsideClick(e: MouseEvent) {
  if (!(e.target as HTMLElement).closest('[data-menu-anchor]'))
    openMenuId.value = null
}

onMounted(async () => {
  await Promise.all([fetchRequests(), fetchWhitelist(), fetchUsers(), fetchCoordinators(), fetchInstitutions()])
  document.addEventListener('click', handleOutsideClick)
})

onUnmounted(() => {
  document.removeEventListener('click', handleOutsideClick)
})

async function fetchUsers() {
  loadingUsers.value = true
  try {
    const res = await adminService.getAllUsers()
    users.value = res.data
  } finally {
    loadingUsers.value = false
  }
}

async function fetchCoordinators() {
  try {
    const res = await coordinatorService.getCoordinators()
    coordinatorsList.value = res.data
  } catch { /* non-critical */ }
}

async function fetchInstitutions() {
  try {
    const res = await institutionService.getHomeInstitutions()
    institutionsList.value = res.data
  } catch { /* non-critical */ }
}

async function makeCoordinatorFromList(userId: string) {
  userActionId.value = userId
  try {
    await adminService.makeCoordinator(userId)
    const u = users.value.find((x: { id: string }) => x.id === userId)
    if (u) { u.role = userRole.Coordinator; u.coordinatorRequestStatus = null }
  } finally {
    userActionId.value = null
  }
}

async function removeCoordinatorFromList(userId: string) {
  if (!await confirm({ title: t('admin.users.removeCoordinatorConfirm') })) return
  userActionId.value = userId
  try {
    await adminService.removeCoordinator(userId)
    const u = users.value.find(x => x.id === userId)
    if (u) { u.role = userRole.Student; u.coordinatorRequestStatus = null }
  } finally {
    userActionId.value = null
  }
}

async function fetchRequests() {
  loadingRequests.value = true
  try {
    const res = await adminService.getCoordinatorRequests()
    requests.value = res.data
  } finally {
    loadingRequests.value = false
  }
}

async function fetchWhitelist() {
  loadingWhitelist.value = true
  try {
    const res = await adminService.getCoordinatorWhitelist()
    whitelist.value = res.data
  } finally {
    loadingWhitelist.value = false
  }
}

async function approve(userId: string) {
  actionLoadingId.value = userId
  try {
    await adminService.makeCoordinator(userId)
    requests.value = requests.value.filter(r => r.id !== userId)
  } finally {
    actionLoadingId.value = null
  }
}

async function reject(userId: string) {
  actionLoadingId.value = userId
  try {
    await adminService.rejectCoordinatorRequest(userId)
    requests.value = requests.value.filter(r => r.id !== userId)
  } finally {
    actionLoadingId.value = null
  }
}

async function addEmail() {
  errorMessage.value = null
  const email = newEmail.value.trim()
  if (!email) return
  addingEmail.value = true
  try {
    const res = await adminService.addToWhitelist(email)
    whitelist.value.push(res.data)
    whitelist.value.sort((a, b) => a.email.localeCompare(b.email))
    newEmail.value = ''
  } catch (e: unknown) {
    const err = e as { response?: { data?: { detail?: string } } }
    errorMessage.value = err.response?.data?.detail ?? t('admin.whitelist.addError')
  } finally {
    addingEmail.value = false
  }
}

async function removeEmail(email: string) {
  if (!await confirm({ title: t('admin.whitelist.removeConfirm') })) return
  whitelistActionEmail.value = email
  try {
    await adminService.removeFromWhitelist(email)
    whitelist.value = whitelist.value.filter(e => e.email !== email)
  } finally {
    whitelistActionEmail.value = null
  }
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, { day: '2-digit', month: '2-digit', year: 'numeric' })
}
</script>

<template>
  <div class="space-y-6">

    <!-- Pending coordinator requests -->
    <section class="rounded-2xl border border-primary/20 bg-dark-2 p-6">
      <h2 class="mb-5 text-base font-semibold text-light">{{ t('admin.requests.title') }}</h2>

      <div v-if="loadingRequests" class="space-y-3">
        <div v-for="i in 2" :key="i" class="h-14 animate-pulse rounded-xl bg-dark"></div>
      </div>
      <p v-else-if="requests.length === 0" class="text-sm text-light/50">
        {{ t('admin.requests.empty') }}
      </p>
      <div v-else class="space-y-2">
        <div
          v-for="req in requests"
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
              class="rounded-lg bg-primary px-4 py-1.5 text-xs font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
              :disabled="actionLoadingId === req.id"
              @click="approve(req.id)"
            >{{ t('admin.requests.approve') }}</button>
            <button
              type="button"
              class="rounded-lg border border-red-400/40 px-4 py-1.5 text-xs font-medium text-red-300 transition hover:bg-red-500/20 disabled:opacity-50"
              :disabled="actionLoadingId === req.id"
              @click="reject(req.id)"
            >{{ t('admin.requests.reject') }}</button>
          </div>
        </div>
      </div>
    </section>

    <!-- User management -->
    <section class="rounded-2xl border border-primary/20 bg-dark-2 p-6">
      <h2 class="mb-5 text-base font-semibold text-light">{{ t('admin.users.title') }}</h2>

      <div v-if="loadingUsers" class="space-y-4">
        <div v-for="i in 3" :key="i" class="h-12 animate-pulse rounded-xl bg-dark"></div>
      </div>

      <div v-else class="space-y-6">

        <!-- Admins -->
        <div>
          <p class="mb-2 text-xs font-semibold uppercase tracking-wider text-light/40">
            {{ t('admin.users.role.Admin') }}
            <span class="ml-1 font-normal normal-case tracking-normal text-light/30">({{ admins.length }})</span>
          </p>
          <p v-if="admins.length === 0" class="text-sm text-light/40">{{ t('admin.users.empty') }}</p>
          <div v-else class="divide-y divide-white/5 rounded-xl bg-dark">
            <div v-for="u in admins" :key="u.id" class="flex items-center justify-between px-4 py-3">
              <div>
                <span class="text-sm font-medium text-light">{{ u.name }}</span>
                <span class="ml-2 text-xs text-light/50">{{ u.email }}</span>
              </div>
              <span class="rounded-full border border-purple-400/40 bg-purple-500/10 px-2.5 py-0.5 text-xs font-semibold text-purple-300">
                {{ t('admin.users.role.Admin') }}
              </span>
            </div>
          </div>
        </div>

        <!-- Coordinators -->
        <div>
          <p class="mb-2 text-xs font-semibold uppercase tracking-wider text-light/40">
            {{ t('admin.users.role.Coordinator') }}
            <span class="ml-1 font-normal normal-case tracking-normal text-light/30">({{ coordinators.length }})</span>
          </p>
          <p v-if="coordinators.length === 0" class="text-sm text-light/40">{{ t('admin.users.empty') }}</p>
          <div v-else class="divide-y divide-white/5 rounded-xl bg-dark">
            <div v-for="u in coordinators" :key="u.id" class="flex items-center justify-between px-4 py-3">
              <div>
                <span class="text-sm font-medium text-light">{{ u.name }}</span>
                <span class="ml-2 text-xs text-light/50">{{ u.email }}</span>
              </div>
              <div class="relative" data-menu-anchor>
                <button
                  type="button"
                  class="flex h-7 w-7 items-center justify-center rounded-lg text-lg leading-none text-light/40 transition hover:bg-white/10 hover:text-light"
                  :aria-expanded="openMenuId === u.id"
                  @click.stop="toggleMenu(u.id)"
                >⋯</button>
                <div
                  v-if="openMenuId === u.id"
                  class="absolute right-0 top-full z-50 mt-1 w-56 overflow-hidden rounded-xl border border-primary/20 bg-dark-2 px-1 py-1 shadow-2xl shadow-black/50"
                >
                  <button
                    type="button"
                    class="flex w-full items-center gap-2 rounded-lg px-3 py-1.5 text-xs font-medium text-light transition hover:bg-primary/20"
                    @click.stop="openEditDialog(u)"
                  >
                    <svg class="h-3.5 w-3.5 text-light/50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                    {{ t('admin.users.editUser') }}
                  </button>
                  <button
                    type="button"
                    class="flex w-full items-center gap-2 rounded-lg px-3 py-1.5 text-xs font-medium text-red-300 transition hover:bg-red-500/20 disabled:opacity-50"
                    :disabled="userActionId === u.id"
                    @click.stop="removeCoordinatorFromList(u.id); openMenuId = null"
                  >
                    <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m0 0l-6-6m6 6l6-6" />
                    </svg>
                    {{ t('admin.users.demoteToStudent') }}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Students -->
        <div>
          <div class="mb-3 flex items-center justify-between">
            <p class="text-xs font-semibold uppercase tracking-wider text-light/40">
              {{ t('admin.users.role.Student') }}
              <span class="ml-1 font-normal normal-case tracking-normal text-light/30">({{ allStudents.length }})</span>
            </p>
          </div>
          <SearchInput
            v-model="studentSearch"
            :placeholder="t('admin.users.searchPlaceholder')"
            class="mb-3"
            @update:model-value="onStudentSearch"
          />
          <p v-if="filteredStudents.length === 0" class="text-sm text-light/40">{{ t('admin.users.empty') }}</p>
          <div v-else class="divide-y divide-white/5 rounded-xl bg-dark">
            <div v-for="u in students" :key="u.id" class="flex items-center justify-between px-4 py-3">
              <div class="min-w-0">
                <div class="flex items-baseline gap-2">
                  <span class="text-sm font-medium text-light">{{ u.name }}</span>
                  <span v-if="u.email" class="text-xs text-light/35">{{ u.email }}</span>
                </div>
              </div>
              <div class="flex items-center gap-2">
                <span
                  v-if="u.coordinatorRequestStatus === 'Pending'"
                  class="whitespace-nowrap rounded-full border border-primary/30 bg-primary/10 px-2.5 py-0.5 text-[11px] font-medium text-primary-light"
                >{{ t('admin.users.coordinatorRequest') }}</span>
                <span
                  v-else-if="!u.email"
                  class="whitespace-nowrap rounded-full border border-yellow-400/30 bg-yellow-500/10 px-2.5 py-0.5 text-[11px] font-medium text-yellow-300"
                >{{ t('admin.users.notOnboarded') }}</span>
              <div class="relative" data-menu-anchor>
                <button
                  type="button"
                  class="flex h-7 w-7 items-center justify-center rounded-lg text-lg leading-none text-light/40 transition hover:bg-white/10 hover:text-light"
                  :aria-expanded="openMenuId === u.id"
                  @click.stop="toggleMenu(u.id)"
                >⋯</button>
                <div
                  v-if="openMenuId === u.id"
                  class="absolute right-0 top-full z-50 mt-1 w-56 overflow-hidden rounded-xl border border-primary/20 bg-dark-2 px-1 py-1 shadow-2xl shadow-black/50"
                >
                  <button
                    type="button"
                    class="flex w-full items-center gap-2 rounded-lg px-3 py-1.5 text-xs font-medium text-light transition hover:bg-primary/20"
                    @click.stop="openEditDialog(u)"
                  >
                    <svg class="h-3.5 w-3.5 text-light/50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                    {{ t('admin.users.editUser') }}
                  </button>
                  <button
                    type="button"
                    class="flex w-full items-center gap-2 rounded-lg px-3 py-1.5 text-xs font-medium text-light transition hover:bg-primary/20 disabled:opacity-50"
                    :disabled="userActionId === u.id"
                    @click.stop="makeCoordinatorFromList(u.id); openMenuId = null"
                  >
                    <svg class="h-3.5 w-3.5 text-light/50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 20V4m0 0l-6 6m6-6l6 6" />
                    </svg>
                    {{ t('admin.users.makeCoordinator') }}
                  </button>
                </div>
              </div>
              </div>
            </div>
          </div>

          <div v-if="totalStudentPages > 1" class="mt-3 flex items-center justify-between text-xs text-light/40">
            <span>{{ (studentPage - 1) * STUDENTS_PER_PAGE + 1 }}–{{ Math.min(studentPage * STUDENTS_PER_PAGE, filteredStudents.length) }} / {{ filteredStudents.length }}</span>
            <div class="flex gap-1">
              <button
                type="button"
                class="rounded-lg border border-white/10 px-3 py-1.5 transition hover:bg-white/5 disabled:opacity-30"
                :disabled="studentPage === 1"
                @click="studentPage--"
              >←</button>
              <span class="flex items-center px-2">{{ studentPage }} / {{ totalStudentPages }}</span>
              <button
                type="button"
                class="rounded-lg border border-white/10 px-3 py-1.5 transition hover:bg-white/5 disabled:opacity-30"
                :disabled="studentPage === totalStudentPages"
                @click="studentPage++"
              >→</button>
            </div>
          </div>
        </div>

      </div>
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
          class="rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
          :disabled="addingEmail || !newEmail.trim()"
          @click="addEmail"
        >{{ addingEmail ? t('common.loading') : t('admin.whitelist.add') }}</button>
      </div>

      <p v-if="errorMessage" class="mb-4 rounded-xl border border-red-400/40 bg-red-500/10 px-4 py-3 text-sm text-red-300">
        {{ errorMessage }}
      </p>

      <div v-if="loadingWhitelist" class="space-y-2">
        <div v-for="i in 3" :key="i" class="h-12 animate-pulse rounded-xl bg-dark"></div>
      </div>
      <p v-else-if="whitelist.length === 0" class="text-sm text-light/50">{{ t('admin.whitelist.empty') }}</p>
      <div v-else class="divide-y divide-white/5 rounded-xl bg-dark">
        <div
          v-for="entry in whitelist"
          :key="entry.id"
          class="flex items-center justify-between px-4 py-3"
        >
          <div>
            <p class="text-sm font-medium text-light">{{ entry.email }}</p>
          </div>
          <button
            type="button"
            class="flex h-7 w-7 items-center justify-center rounded-lg border border-red-400/20 text-red-400/60 transition hover:border-red-400/50 hover:bg-red-500/10 hover:text-red-300 disabled:opacity-40"
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
    </section>

    <!-- Edit user modal -->
    <AdminEditUserModal
      v-if="editingUser"
      :user="editingUser"
      :coordinators="coordinatorsList"
      :institutions="institutionsList"
      @close="editingUser = null"
      @saved="onUserSaved"
    />
  </div>
</template>
