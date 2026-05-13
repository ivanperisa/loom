<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { adminService, type CoordinatorRequestResponse, type CoordinatorWhitelistEntryResponse, type UserListResponse } from '@/services/admin.service'
import { userRole } from '../utils/userRole'

const { t } = useI18n()

const requests = ref<CoordinatorRequestResponse[]>([])
const whitelist = ref<CoordinatorWhitelistEntryResponse[]>([])
const users = ref<UserListResponse[]>([])
const newEmail = ref('')

const loadingRequests = ref(true)
const loadingWhitelist = ref(true)
const loadingUsers = ref(true)
const actionLoadingId = ref<string | null>(null)
const whitelistActionEmail = ref<string | null>(null)
const addingEmail = ref(false)
const errorMessage = ref<string | null>(null)
const userActionId = ref<string | null>(null)
const openMenuId = ref<string | null>(null)

const admins = computed(() => users.value.filter(u => u.role === userRole.Admin))
const coordinators = computed(() => users.value.filter(u => u.role === userRole.Coordinator))
const students = computed(() => users.value.filter(u => u.role === userRole.Student))

function toggleMenu(userId: string) {
  openMenuId.value = openMenuId.value === userId ? null : userId
}

function handleOutsideClick(e: MouseEvent) {
  if (!(e.target as HTMLElement).closest('[data-menu-anchor]'))
    openMenuId.value = null
}

onMounted(async () => {
  await Promise.all([fetchRequests(), fetchWhitelist(), fetchUsers()])
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
  <main class="min-h-screen bg-dark">
    <section class="page-container space-y-10">
      <h1 class="text-3xl font-bold text-light">{{ t('admin.title') }}</h1>

      <!-- Section A: Pending coordinator requests -->
      <div>
        <h2 class="mb-4 text-xl font-semibold text-light">{{ t('admin.requests.title') }}</h2>

        <div v-if="loadingRequests" class="space-y-3">
          <div v-for="i in 2" :key="i" class="h-16 animate-pulse rounded-xl bg-dark-2"></div>
        </div>

        <div
          v-else-if="requests.length === 0"
          class="rounded-xl border border-primary/20 bg-dark-2 p-6 text-center text-light/60"
        >
          {{ t('admin.requests.empty') }}
        </div>

        <div v-else class="space-y-3">
          <div
            v-for="req in requests"
            :key="req.id"
            class="flex items-center justify-between rounded-xl border border-primary/20 bg-dark-2 px-5 py-4"
          >
            <div>
              <p class="font-semibold text-light">{{ req.name }}</p>
              <p class="text-sm text-light/60">{{ req.email }}</p>
              <p v-if="req.institutionName" class="text-xs text-light/60">{{ req.institutionName }}</p>
            </div>
            <div class="flex gap-2">
              <button
                type="button"
                class="rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
                :disabled="actionLoadingId === req.id"
                @click="approve(req.id)"
              >
                {{ t('admin.requests.approve') }}
              </button>
              <button
                type="button"
                class="rounded-lg border border-red-400/50 px-4 py-2 text-sm font-medium text-red-300 transition hover:bg-red-500/20 disabled:opacity-50"
                :disabled="actionLoadingId === req.id"
                @click="reject(req.id)"
              >
                {{ t('admin.requests.reject') }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Section B: User management -->
      <div>
        <h2 class="mb-4 text-xl font-semibold text-light">{{ t('admin.users.title') }}</h2>

        <!-- Loading skeleton -->
        <div v-if="loadingUsers" class="space-y-6">
          <div v-for="section in 3" :key="section">
            <div class="mb-2 h-3 w-24 animate-pulse rounded bg-dark-2"></div>
            <div class="rounded-xl border border-primary/20 bg-dark-2">
              <div v-for="i in 3" :key="i" class="h-12 animate-pulse border-b border-white/5 last:border-b-0"></div>
            </div>
          </div>
        </div>

        <div v-else class="space-y-6">

          <!-- Admins -->
          <div>
            <h3 class="mb-2 text-xs font-semibold uppercase tracking-wider text-light/40">
              {{ t('admin.users.role.Admin') }}
              <span class="ml-1.5 font-normal normal-case tracking-normal text-light/30">({{ admins.length }})</span>
            </h3>
            <div v-if="admins.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 px-5 py-3 text-sm text-light/40">
              {{ t('admin.users.empty') }}
            </div>
            <div v-else class="rounded-xl border border-primary/20 bg-dark-2">
              <div
                v-for="u in admins"
                :key="u.id"
                class="flex items-center justify-between border-b border-white/5 px-5 py-3 last:border-b-0"
              >
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
            <h3 class="mb-2 text-xs font-semibold uppercase tracking-wider text-light/40">
              {{ t('admin.users.role.Coordinator') }}
              <span class="ml-1.5 font-normal normal-case tracking-normal text-light/30">({{ coordinators.length }})</span>
            </h3>
            <div v-if="coordinators.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 px-5 py-3 text-sm text-light/40">
              {{ t('admin.users.empty') }}
            </div>
            <div v-else class="rounded-xl border border-primary/20 bg-dark-2">
              <div
                v-for="u in coordinators"
                :key="u.id"
                class="flex items-center justify-between border-b border-white/5 px-5 py-3 last:border-b-0"
              >
                <div>
                  <span class="text-sm font-medium text-light">{{ u.name }}</span>
                  <span class="ml-2 text-xs text-light/50">{{ u.email }}</span>
                  <span v-if="u.institutionName" class="ml-2 text-xs text-light/40">{{ u.institutionName }}</span>
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
                    class="absolute right-0 top-full z-50 mt-1 min-w-[11rem] overflow-hidden rounded-xl border border-primary/20 bg-dark-2 px-1 py-1 shadow-2xl shadow-black/50"
                  >
                    <button
                      type="button"
                      class="flex w-full items-center gap-2 rounded-lg px-3 py-1.5 text-xs font-medium text-red-300 transition hover:bg-red-500/20 disabled:opacity-50"
                      :disabled="userActionId === u.id"
                      @click.stop="removeCoordinatorFromList(u.id); openMenuId = null"
                    >
                      {{ t('admin.users.demoteToStudent') }}
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Students -->
          <div>
            <h3 class="mb-2 text-xs font-semibold uppercase tracking-wider text-light/40">
              {{ t('admin.users.role.Student') }}
              <span class="ml-1.5 font-normal normal-case tracking-normal text-light/30">({{ students.length }})</span>
            </h3>
            <div v-if="students.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 px-5 py-3 text-sm text-light/40">
              {{ t('admin.users.empty') }}
            </div>
            <div v-else class="rounded-xl border border-primary/20 bg-dark-2">
              <div
                v-for="u in students"
                :key="u.id"
                class="flex items-center justify-between border-b border-white/5 px-5 py-3 last:border-b-0"
              >
                <div>
                  <span class="text-sm font-medium text-light">{{ u.name }}</span>
                  <span class="ml-2 text-xs text-light/50">{{ u.email }}</span>
                  <span
                    v-if="u.coordinatorRequestStatus"
                    class="ml-2 rounded-full border border-yellow-400/40 bg-yellow-500/10 px-2 py-0.5 text-xs text-yellow-300"
                  >{{ t('admin.users.pending') }}</span>
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
                    class="absolute right-0 top-full z-50 mt-1 min-w-[11rem] overflow-hidden rounded-xl border border-primary/20 bg-dark-2 px-1 py-1 shadow-2xl shadow-black/50"
                  >
                    <button
                      type="button"
                      class="flex w-full items-center gap-2 rounded-lg px-3 py-1.5 text-xs font-medium text-primary-light transition hover:bg-primary/20 disabled:opacity-50"
                      :disabled="userActionId === u.id"
                      @click.stop="makeCoordinatorFromList(u.id); openMenuId = null"
                    >
                      {{ t('admin.users.makeCoordinator') }}
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

        </div>
      </div>

      <!-- Section C: Coordinator whitelist -->
      <div>
        <h2 class="mb-4 text-xl font-semibold text-light">{{ t('admin.whitelist.title') }}</h2>
        <p class="mb-4 text-sm text-light/60">{{ t('admin.whitelist.description') }}</p>

        <!-- Add email form -->
        <div class="mb-6 flex gap-3">
          <input
            v-model="newEmail"
            type="email"
            :placeholder="t('admin.whitelist.emailPlaceholder')"
            class="flex-1 rounded-xl border border-primary/20 bg-dark px-4 py-2.5 text-light focus:border-primary focus:outline-none"
            @keydown.enter.prevent="addEmail"
          />
          <button
            type="button"
            class="rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
            :disabled="addingEmail || !newEmail.trim()"
            @click="addEmail"
          >
            {{ addingEmail ? t('common.loading') : t('admin.whitelist.add') }}
          </button>
        </div>

        <p v-if="errorMessage" class="mb-4 rounded-lg border border-red-400/50 bg-red-500/10 px-4 py-2 text-sm text-red-200">
          {{ errorMessage }}
        </p>

        <div v-if="loadingWhitelist" class="space-y-2">
          <div v-for="i in 3" :key="i" class="h-12 animate-pulse rounded-xl bg-dark-2"></div>
        </div>

        <div
          v-else-if="whitelist.length === 0"
          class="rounded-xl border border-primary/20 bg-dark-2 p-6 text-center text-light/60"
        >
          {{ t('admin.whitelist.empty') }}
        </div>

        <div v-else class="space-y-2">
          <div
            v-for="entry in whitelist"
            :key="entry.id"
            class="flex items-center justify-between rounded-xl border border-primary/20 bg-dark-2 px-5 py-3"
          >
            <div>
              <p class="text-sm font-medium text-light">{{ entry.email }}</p>
              <p class="text-xs text-light/60">{{ t('admin.whitelist.addedOn') }} {{ formatDate(entry.createdAt) }}</p>
            </div>
            <button
              type="button"
              class="rounded-lg border border-red-400/40 px-3 py-1.5 text-xs font-medium text-red-300 transition hover:bg-red-500/20 disabled:opacity-50"
              :disabled="whitelistActionEmail === entry.email"
              @click="removeEmail(entry.email)"
            >
              {{ t('admin.whitelist.remove') }}
            </button>
          </div>
        </div>
      </div>
    </section>
  </main>
</template>
