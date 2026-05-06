<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import CreateExchangeModal from '@/components/exchange/CreateExchangeModal.vue'
import { useAuthStore } from '@/stores/auth.store'
import { useExchangeStore } from '@/stores/exchange.store'
import { userService } from '@/services/user.service'
import { statusColorClass } from '@/utils/statusColors'

const router = useRouter()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()
const { t } = useI18n()

const showCreateModal = ref(false)
const deletingId = ref<string | null>(null)
const requestingCoordinator = ref(false)

const displayName = computed(() => authStore.name?.trim() || t('common.user'))
const coordinatorRequestStatus = computed(() => authStore.user?.coordinatorRequestStatus ?? null)

onMounted(async () => {
  await exchangeStore.fetchMySummaries()
})

function openCreateModal() {
  showCreateModal.value = true
}

function onExchangeCreated(exchangeId: string) {
  showCreateModal.value = false
  router.push(`/exchange/${exchangeId}`)
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

</script>

<template>
  <main class="min-h-screen bg-dark">
    <section class="page-container">
      <h1 class="text-3xl font-bold text-light">
        {{ t('home.welcome', { name: displayName }) }}
      </h1>

      <!-- Pending coordinator request banner -->
      <div
        v-if="coordinatorRequestStatus === 'Pending'"
        class="mt-6 rounded-lg border border-primary/40 bg-primary/10 px-4 py-3 text-sm text-primary-light"
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
          class="ml-4 rounded-lg border border-primary/50 px-4 py-1.5 text-xs font-semibold text-primary-light transition hover:bg-primary/20 disabled:opacity-50"
          :disabled="requestingCoordinator"
          @click="reRequestCoordinatorRole"
        >
          {{ requestingCoordinator ? t('common.loading') : t('home.reRequestCoordinator') }}
        </button>
      </div>

      <!-- Loading skeleton -->
        <div v-if="exchangeStore.loading" class="mt-8 rounded-xl border border-primary/20 bg-dark-2 p-6">
          <div class="animate-pulse space-y-4">
            <div class="h-5 w-48 rounded bg-primary/20"></div>
            <div class="h-4 w-64 rounded bg-primary/20"></div>
            <div class="h-4 w-40 rounded bg-primary/20"></div>
            <div class="h-10 w-36 rounded bg-primary/20"></div>
          </div>
        </div>

        <template v-else>
          <!-- Header with title + create button -->
          <div class="mt-8 flex items-center justify-between">
            <h2 class="text-xl font-semibold text-light">{{ t('home.myExchanges') }}</h2>
            <button
              type="button"
              class="rounded-lg bg-primary px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
              @click="openCreateModal"
            >
              + {{ t('home.createNew') }}
            </button>
          </div>

          <!-- No exchanges -->
          <div v-if="exchangeStore.summaries.length === 0" class="mt-4 rounded-xl border border-primary/20 bg-dark-2 p-6 text-center">
            <svg class="mx-auto h-12 w-12 text-light/60" viewBox="0 0 24 24" fill="none">
              <path d="M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" stroke="currentColor" stroke-width="1.5"/>
              <path d="M12 8v4m0 4h.01" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
            </svg>
            <p class="mt-3 text-light/60">{{ t('home.noExchange') }}</p>
          </div>

          <!-- Exchange cards -->
          <div v-else class="mt-4 space-y-4">
            <div
              v-for="ex in exchangeStore.summaries"
              :key="ex.id"
              class="rounded-xl border border-primary/20 bg-dark-2 p-6 transition hover:border-primary/40"
            >
              <div class="flex items-start justify-between gap-3">
                <span class="text-xs font-medium text-light/60">{{ ex.academicYear }} &middot; {{ t(`exchangeSemester.${ex.semesterType}`) }}</span>
                <div class="flex flex-wrap gap-1.5">
                  <span
                    class="rounded-full border px-2.5 py-0.5 text-xs font-semibold"
                    :class="statusColorClass[ex.learningAgreementStatus] ?? 'bg-slate-500/20 text-slate-300 border-slate-400'"
                    :title="t('exchange.tabs.learningAgreement')"
                  >
                    {{ t('exchange.tabs.learningAgreement') }}: {{ t(`documentStatus.${ex.learningAgreementStatus}`) }}
                  </span>
                  <span
                    v-if="ex.learningAgreementStatus === 'Approved' || ex.recognitionStatus"
                    class="rounded-full border px-2.5 py-0.5 text-xs font-semibold"
                    :class="statusColorClass[ex.recognitionStatus ?? 'Draft'] ?? 'bg-slate-500/20 text-slate-300 border-slate-400'"
                    :title="t('exchange.tabs.recognition')"
                  >
                    {{ t('exchange.tabs.recognition') }}: {{ t(`documentStatus.${ex.recognitionStatus ?? 'Draft'}`) }}
                  </span>
                </div>
              </div>

              <div class="mt-3 grid gap-3 sm:grid-cols-2">
                <!-- Foreign -->
                <div class="rounded-lg border border-primary/15 bg-dark/50 px-4 py-3">
                  <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-light/60">{{ t('home.foreignInstitution') }}</p>
                  <dl class="space-y-1 text-sm">
                    <div>
                      <dt class="text-light/60">{{ t('exchange.institution') }}</dt>
                      <dd class="font-medium text-light">{{ ex.foreignInstitutionName }}</dd>
                    </div>
                    <div>
                      <dt class="text-light/60">{{ t('exchange.program') }}</dt>
                      <dd class="font-medium text-light">{{ ex.foreignProgramName }}</dd>
                    </div>
                  </dl>
                </div>
                <!-- Home -->
                <div class="rounded-lg border border-primary/15 bg-dark/50 px-4 py-3">
                  <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-light/60">{{ t('exchange.homeInstitution') }}</p>
                  <dl class="space-y-1 text-sm">
                    <div>
                      <dt class="text-light/60">{{ t('exchange.institution') }}</dt>
                      <dd class="font-medium text-light">{{ ex.homeInstitutionName }}</dd>
                    </div>
                    <div>
                      <dt class="text-light/60">{{ t('exchange.program') }}</dt>
                      <dd class="font-medium text-light">{{ ex.studyProgramName }}</dd>
                    </div>
                    <div>
                      <dt class="text-light/60">{{ t('exchange.profile') }}</dt>
                      <dd class="font-medium text-light">{{ ex.studyProfileName }}</dd>
                    </div>
                  </dl>
                </div>
              </div>

              <div class="mt-4 flex items-center gap-3">
                <button
                  type="button"
                  class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
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
    </section>

    <CreateExchangeModal
      v-if="showCreateModal"
      @close="showCreateModal = false"
      @created="onExchangeCreated"
    />
  </main>
</template>
