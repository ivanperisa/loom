<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import AppHeader from '@/components/AppHeader.vue'
import CreateExchangeModal from '@/components/exchange/CreateExchangeModal.vue'
import { useAuthStore } from '@/stores/auth.store'
import { useExchangeStore } from '@/stores/exchange.store'
import { userService } from '@/services/user.service'

const router = useRouter()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()
const { t } = useI18n()

const showCreateModal = ref(false)
const deletingId = ref<string | null>(null)
const requestingCoordinator = ref(false)

const displayName = computed(() => authStore.name?.trim() || t('common.user'))
const isCoordinator = computed(() => authStore.role === 'Coordinator' || authStore.role === 'Admin')
const isAdmin = computed(() => authStore.role === 'Admin')
const coordinatorRequestStatus = computed(() => authStore.user?.coordinatorRequestStatus ?? null)

onMounted(async () => {
  if (!isCoordinator.value) {
    await exchangeStore.fetchMySummaries()
  }
})

function openCreateModal() {
  showCreateModal.value = true
}

function onExchangeCreated(exchangeId: string) {
  showCreateModal.value = false
  router.push(`/exchange/${exchangeId}`)
}

function goToCoordinatorDashboard() {
  router.push('/coordinator')
}

async function reRequestCoordinatorRole() {
  requestingCoordinator.value = true
  try {
    const res = await userService.requestCoordinatorRole()
    authStore.user = res.data
  } finally {
    requestingCoordinator.value = false
  }
}

async function confirmDelete(exchangeId: string) {
  if (!confirm(t('home.deleteConfirm'))) return
  deletingId.value = exchangeId
  try {
    await exchangeStore.deleteExchange(exchangeId)
  } finally {
    deletingId.value = null
  }
}

const statusColorClass: Record<string, string> = {
  Draft: 'bg-slate-500/20 text-slate-300 border-slate-400',
  Submitted: 'bg-yellow-500/20 text-yellow-300 border-yellow-400',
  Approved: 'bg-green-500/20 text-green-300 border-green-400',
  Rejected: 'bg-red-500/20 text-red-300 border-red-400',
}
</script>

<template>
  <main class="min-h-screen bg-[#071C2C]">
    <AppHeader />

    <section class="page-container">
      <h1 class="text-3xl font-bold text-[#CAE4F7]">
        {{ t('home.welcome', { name: displayName }) }}
      </h1>

      <!-- Pending coordinator request banner -->
      <div
        v-if="coordinatorRequestStatus === 'Pending'"
        class="mt-6 rounded-lg border border-[#218CD9]/40 bg-[#218CD9]/10 px-4 py-3 text-sm text-[#8AC4ED]"
      >
        {{ t('home.coordinatorRequestPending') }}
      </div>

      <!-- Rejected coordinator request banner -->
      <div
        v-if="coordinatorRequestStatus === 'Rejected'"
        class="mt-6 flex items-center justify-between rounded-lg border border-red-400/40 bg-red-500/10 px-4 py-3"
      >
        <span class="text-sm text-red-300">{{ t('home.coordinatorRequestRejected') }}</span>
        <button
          type="button"
          class="ml-4 rounded-lg border border-[#218CD9]/50 px-4 py-1.5 text-xs font-semibold text-[#8AC4ED] transition hover:bg-[#218CD9]/20 disabled:opacity-50"
          :disabled="requestingCoordinator"
          @click="reRequestCoordinatorRole"
        >
          {{ requestingCoordinator ? t('common.loading') : t('home.reRequestCoordinator') }}
        </button>
      </div>

      <!-- Coordinator / Admin -->
      <template v-if="isCoordinator">
        <div class="mt-8 rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-6">
          <div class="flex items-center gap-3">
            <svg class="h-8 w-8 text-[#218CD9]" viewBox="0 0 24 24" fill="none">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
              <circle cx="9" cy="7" r="4" stroke="currentColor" stroke-width="1.8"/>
              <path d="M23 21v-2a4 4 0 0 0-3-3.87" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M16 3.13a4 4 0 0 1 0 7.75" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <h2 class="text-xl font-semibold text-[#CAE4F7]">{{ t('home.coordinatorDashboard') }}</h2>
          </div>
          <p class="mt-2 text-sm text-[#5A8AAD]">{{ t('coordinator.title') }}</p>
          <div class="mt-4 flex gap-3">
            <button
              type="button"
              class="rounded-lg bg-[#218CD9] px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-[#8AC4ED] hover:text-[#071C2C]"
              @click="goToCoordinatorDashboard"
            >
              {{ t('home.coordinatorDashboard') }}
            </button>
            <button
              v-if="isAdmin"
              type="button"
              class="rounded-lg border border-[#1E4A6E] px-5 py-2.5 text-sm font-semibold text-[#CAE4F7] transition hover:border-[#218CD9]/50 hover:bg-[#218CD9]/10"
              @click="$router.push('/admin')"
            >
              {{ t('home.adminPanel') }}
            </button>
          </div>
        </div>
      </template>

      <!-- Student -->
      <template v-else>
        <!-- Loading skeleton -->
        <div v-if="exchangeStore.loading" class="mt-8 rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-6">
          <div class="animate-pulse space-y-4">
            <div class="h-5 w-48 rounded bg-[#1E4A6E]"></div>
            <div class="h-4 w-64 rounded bg-[#1E4A6E]"></div>
            <div class="h-4 w-40 rounded bg-[#1E4A6E]"></div>
            <div class="h-10 w-36 rounded bg-[#1E4A6E]"></div>
          </div>
        </div>

        <template v-else>
          <!-- Header with title + create button -->
          <div class="mt-8 flex items-center justify-between">
            <h2 class="text-xl font-semibold text-[#CAE4F7]">{{ t('home.myExchanges') }}</h2>
            <button
              type="button"
              class="rounded-lg bg-[#218CD9] px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-[#8AC4ED] hover:text-[#071C2C]"
              @click="openCreateModal"
            >
              + {{ t('home.createNew') }}
            </button>
          </div>

          <!-- No exchanges -->
          <div v-if="exchangeStore.summaries.length === 0" class="mt-4 rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-6 text-center">
            <svg class="mx-auto h-12 w-12 text-[#5A8AAD]" viewBox="0 0 24 24" fill="none">
              <path d="M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" stroke="currentColor" stroke-width="1.5"/>
              <path d="M12 8v4m0 4h.01" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
            </svg>
            <p class="mt-3 text-[#5A8AAD]">{{ t('home.noExchange') }}</p>
          </div>

          <!-- Exchange cards -->
          <div v-else class="mt-4 space-y-4">
            <div
              v-for="ex in exchangeStore.summaries"
              :key="ex.id"
              class="rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-6 transition hover:border-[#218CD9]/40"
            >
              <div class="flex items-start justify-between">
                <span class="text-xs font-medium text-[#5A8AAD]">{{ ex.academicYear }} &middot; {{ t(`exchangeSemester.${ex.semesterType}`) }}</span>
                <span
                  class="rounded-full border px-3 py-0.5 text-xs font-semibold"
                  :class="statusColorClass[ex.status] ?? 'bg-slate-500/20 text-slate-300 border-slate-400'"
                >
                  {{ t(`exchangeStatus.${ex.status}`) }}
                </span>
              </div>

              <div class="mt-3 grid gap-3 sm:grid-cols-2">
                <!-- Foreign -->
                <div class="rounded-lg border border-[#1E4A6E]/60 bg-[#071C2C]/50 px-4 py-3">
                  <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-[#5A8AAD]">{{ t('home.foreignInstitution') }}</p>
                  <dl class="space-y-1 text-sm">
                    <div>
                      <dt class="text-[#5A8AAD]">{{ t('exchange.institution') }}</dt>
                      <dd class="font-medium text-[#CAE4F7]">{{ ex.foreignInstitutionName }}</dd>
                    </div>
                    <div>
                      <dt class="text-[#5A8AAD]">{{ t('exchange.program') }}</dt>
                      <dd class="font-medium text-[#CAE4F7]">{{ ex.foreignProgramName }}</dd>
                    </div>
                  </dl>
                </div>
                <!-- Home -->
                <div class="rounded-lg border border-[#1E4A6E]/60 bg-[#071C2C]/50 px-4 py-3">
                  <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-[#5A8AAD]">{{ t('exchange.homeInstitution') }}</p>
                  <dl class="space-y-1 text-sm">
                    <div>
                      <dt class="text-[#5A8AAD]">{{ t('exchange.institution') }}</dt>
                      <dd class="font-medium text-[#CAE4F7]">{{ ex.homeInstitutionName }}</dd>
                    </div>
                    <div>
                      <dt class="text-[#5A8AAD]">{{ t('exchange.program') }}</dt>
                      <dd class="font-medium text-[#CAE4F7]">{{ ex.studyProgramName }}</dd>
                    </div>
                    <div>
                      <dt class="text-[#5A8AAD]">{{ t('exchange.profile') }}</dt>
                      <dd class="font-medium text-[#CAE4F7]">{{ ex.studyProfileName }}</dd>
                    </div>
                  </dl>
                </div>
              </div>

              <div class="mt-4 flex items-center gap-3">
                <button
                  type="button"
                  class="rounded-lg bg-[#218CD9] px-5 py-2 text-sm font-semibold text-white transition hover:bg-[#8AC4ED] hover:text-[#071C2C]"
                  @click="router.push(`/exchange/${ex.id}`)"
                >
                  {{ t('home.viewExchange') }}
                </button>
                <button
                  v-if="ex.status === 'Draft'"
                  type="button"
                  class="rounded-lg border border-red-400/50 px-4 py-2 text-sm font-medium text-red-300 transition hover:bg-red-500/20 disabled:opacity-50"
                  :disabled="deletingId === ex.id"
                  @click="confirmDelete(ex.id)"
                >
                  {{ deletingId === ex.id ? t('common.loading') : t('home.deleteExchange') }}
                </button>
              </div>
            </div>
          </div>
        </template>
      </template>
    </section>

    <CreateExchangeModal
      v-if="showCreateModal"
      @close="showCreateModal = false"
      @created="onExchangeCreated"
    />
  </main>
</template>
