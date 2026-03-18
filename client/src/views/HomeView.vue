<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import AppHeader from '@/components/AppHeader.vue'
import CreateExchangeModal from '@/components/exchange/CreateExchangeModal.vue'
import { useAuthStore } from '@/stores/auth.store'
import { useExchangeStore } from '@/stores/exchange.store'

const router = useRouter()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()
const { t } = useI18n()

const showCreateModal = ref(false)

const displayName = computed(() => authStore.name?.trim() || t('common.user'))
const isCoordinator = computed(() => authStore.role === 'Coordinator')
const hasExchange = computed(() => exchangeStore.summary !== null)

onMounted(async () => {
  if (!isCoordinator.value) {
    await exchangeStore.fetchMySummary()
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

      <!-- Coordinator -->
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
          <button
            type="button"
            class="mt-4 rounded-lg bg-[#218CD9] px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-[#8AC4ED] hover:text-[#071C2C]"
            @click="goToCoordinatorDashboard"
          >
            {{ t('home.coordinatorDashboard') }}
          </button>
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

        <!-- No exchange -->
        <div v-else-if="!hasExchange" class="mt-8 rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-6 text-center">
          <svg class="mx-auto h-12 w-12 text-[#5A8AAD]" viewBox="0 0 24 24" fill="none">
            <path d="M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" stroke="currentColor" stroke-width="1.5"/>
            <path d="M12 8v4m0 4h.01" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
          </svg>
          <p class="mt-3 text-[#5A8AAD]">{{ t('home.noExchange') }}</p>
          <button
            type="button"
            class="mt-5 rounded-lg bg-[#218CD9] px-6 py-2.5 font-semibold text-white transition hover:bg-[#8AC4ED] hover:text-[#071C2C]"
            @click="openCreateModal"
          >
            {{ t('home.startExchange') }}
          </button>
        </div>

        <!-- Exchange summary -->
        <div v-else class="mt-8 rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-6">
          <div class="flex items-start justify-between">
            <h2 class="text-lg font-semibold text-[#CAE4F7]">{{ t('exchange.title') }}</h2>
            <span
              class="rounded-full border px-3 py-0.5 text-xs font-semibold"
              :class="statusColorClass[exchangeStore.summary!.status] ?? 'bg-slate-500/20 text-slate-300 border-slate-400'"
            >
              {{ t(`exchangeStatus.${exchangeStore.summary!.status}`) }}
            </span>
          </div>

          <dl class="mt-4 grid grid-cols-2 gap-x-6 gap-y-3 text-sm">
            <div>
              <dt class="text-[#5A8AAD]">{{ t('home.foreignInstitution') }}</dt>
              <dd class="font-medium text-[#CAE4F7]">{{ exchangeStore.summary!.foreignInstitutionName }}</dd>
            </div>
            <div>
              <dt class="text-[#5A8AAD]">{{ t('home.foreignProgram') }}</dt>
              <dd class="font-medium text-[#CAE4F7]">{{ exchangeStore.summary!.foreignProgramName }}</dd>
            </div>
            <div>
              <dt class="text-[#5A8AAD]">{{ t('home.academicYear') }}</dt>
              <dd class="font-medium text-[#CAE4F7]">{{ exchangeStore.summary!.academicYear }}</dd>
            </div>
            <div>
              <dt class="text-[#5A8AAD]">{{ t('home.semester') }}</dt>
              <dd class="font-medium text-[#CAE4F7]">{{ t(`exchangeSemester.${exchangeStore.summary!.semesterType}`) }}</dd>
            </div>
          </dl>

          <button
            type="button"
            class="mt-5 rounded-lg bg-[#218CD9] px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-[#8AC4ED] hover:text-[#071C2C]"
            @click="router.push(`/exchange/${exchangeStore.summary!.id}`)"
          >
            {{ t('home.viewExchange') }}
          </button>
        </div>
      </template>
    </section>

    <CreateExchangeModal
      v-if="showCreateModal"
      @close="showCreateModal = false"
      @created="onExchangeCreated"
    />
  </main>
</template>
