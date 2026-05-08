<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import CreateExchangeModal from '@/components/exchange/CreateExchangeModal.vue'
import { useAuthStore } from '@/stores/auth.store'
import { useExchangeStore } from '@/stores/exchange.store'
import { userService } from '@/services/user.service'
import { statusColorClass } from '@/utils/statusColors'
import { documentStatus } from '@/utils/documentStatus'

const router = useRouter()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()
const { t } = useI18n()

const showCreateModal = ref(false)
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
        v-if="coordinatorRequestStatus === documentStatus.Rejected"
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
      <div
        v-if="exchangeStore.loading"
        class="mt-8 rounded-xl border border-primary/20 bg-dark-2 p-6"
      >
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
        <div
          v-if="exchangeStore.summaries.length === 0"
          class="mt-4 rounded-xl border border-primary/20 bg-dark-2 p-6 text-center"
        >
          <svg class="mx-auto h-12 w-12 text-light/60" viewBox="0 0 24 24" fill="none">
            <path
              d="M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
              stroke="currentColor"
              stroke-width="1.5"
            />
            <path
              d="M12 8v4m0 4h.01"
              stroke="currentColor"
              stroke-width="1.8"
              stroke-linecap="round"
            />
          </svg>
          <p class="mt-3 text-light/60">{{ t('home.noExchange') }}</p>
        </div>

        <!-- Exchange cards -->
        <div v-else class="mt-4 space-y-3">
          <div
            v-for="ex in exchangeStore.summaries"
            :key="ex.id"
            class="flex cursor-pointer items-center justify-between rounded-xl border border-primary/20 bg-dark-2 px-5 py-4 transition hover:border-primary/50 hover:bg-dark-2/80"
            @click="router.push(`/exchange/${ex.id}`)"
          >
            <div class="flex-1">
              <!-- Row 1: meta + badges -->
              <div class="flex flex-wrap items-center gap-2">
                <span class="text-xs font-medium text-light/50">
                  {{ ex.academicYear }} &middot; {{ t(`exchangeSemester.${ex.semesterType}`) }}
                </span>
                <span
                  class="rounded-full border px-2 py-0.5 text-xs font-semibold"
                  :class="statusColorClass[ex.learningAgreementStatus]"
                >
                  {{ t('exchange.tabs.learningAgreement') }}:
                  {{ t(`documentStatus.${ex.learningAgreementStatus}`) }}
                </span>
                <span
                  class="rounded-full border px-2 py-0.5 text-xs font-semibold"
                  :class="statusColorClass[ex.recognitionStatus]"
                >
                  {{ t('exchange.tabs.recognition') }}:
                  {{ t(`documentStatus.${ex.recognitionStatus}`) }}
                </span>
              </div>

              <!-- Row 2: strani fakultet -->
              <p class="mt-2.5 text-sm font-semibold text-light">{{ ex.foreignInstitutionName }}</p>
              <p class="text-xs text-light/60">{{ ex.foreignProgramName }}</p>

              <!-- Row 3: studij · profil -->
              <p class="mt-1.5 text-xs text-light/40">
                {{ ex.studyProgramName
                }}<span v-if="ex.studyProfileName"> &middot; {{ ex.studyProfileName }}</span>
              </p>
            </div>

            <svg
              class="ml-4 h-5 w-5 shrink-0 text-light/60"
              viewBox="0 0 20 20"
              fill="currentColor"
            >
              <path
                fill-rule="evenodd"
                d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z"
                clip-rule="evenodd"
              />
            </svg>
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
