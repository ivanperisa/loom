<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import CreateExchangeModal from '@/components/exchange/CreateExchangeModal.vue'
import SearchableSelect from '@/components/common/SearchableSelect.vue'
import { useAuthStore } from '@/stores/auth.store'
import { useExchangeStore } from '@/stores/exchange.store'
import { userService } from '@/services/user.service'
import { statusColorClass } from '@/utils/statusColors'
import { documentStatus } from '@/utils/documentStatus'
import { useQuerySync } from '@/composables/useQuerySync'

const router = useRouter()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()
const { t } = useI18n()

const showCreateModal = ref(false)
const requestingCoordinator = ref(false)

const displayName = computed(() => authStore.name?.trim() || t('common.user'))
const coordinatorRequestStatus = computed(() => authStore.user?.coordinatorRequestStatus ?? null)

const loading = ref(true)

const selectedAcademicYear = ref<string | null>(null)

useQuerySync({ year: selectedAcademicYear })

const academicYears = computed(() => {
  const years = new Set(exchangeStore.summaries.map((ex) => ex.academicYear))
  return Array.from(years).sort().reverse()
})

const academicYearFilterOptions = computed(() => [
  { value: null, label: t('home.allYears') },
  ...academicYears.value.map((year) => ({ value: year, label: year })),
])

const filteredSummaries = computed(() => {
  if (!selectedAcademicYear.value) return exchangeStore.summaries
  return exchangeStore.summaries.filter((ex) => ex.academicYear === selectedAcademicYear.value)
})

const soleExchange = computed(() =>
  filteredSummaries.value.length === 1 ? filteredSummaries.value[0] : null,
)

async function fetchData() {
  loading.value = true
  await exchangeStore.fetchMySummaries()
  loading.value = false
}

onMounted(fetchData)

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
        class="mt-6 flex items-center justify-between rounded-lg border border-danger/40 bg-danger/10 px-4 py-3"
      >
        <span class="text-sm text-danger">{{ t('home.coordinatorRequestRejected') }}</span>
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
        v-if="loading"
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
        <div class="mt-8 flex flex-wrap items-center justify-between gap-3">
          <h2 class="text-xl font-semibold text-light">{{ t('home.myExchanges') }}</h2>
          <div class="flex items-center gap-3">
            <div v-if="academicYears.length >= 1" class="w-40">
              <SearchableSelect
                v-model="selectedAcademicYear"
                :searchable="false"
                :options="academicYearFilterOptions"
              />
            </div>
            <button
              type="button"
              class="rounded-lg bg-primary px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
              @click="openCreateModal"
            >
              + {{ t('home.createNew') }}
            </button>
          </div>
        </div>

        <!-- No exchanges -->
        <div
          v-if="filteredSummaries.length === 0"
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

        <!-- Single exchange: a summary card reads better than a table with one row -->
        <div v-else-if="soleExchange" class="group relative mt-4">
          <RouterLink
            :to="`/exchange/${soleExchange.guid}`"
            class="absolute inset-0 z-0 rounded-xl focus-visible:outline focus-visible:outline-2 focus-visible:outline-primary focus-visible:-outline-offset-2"
            :aria-label="`${soleExchange.partnerInstitutionName} — ${soleExchange.academicYear}`"
          />
          <div class="relative pointer-events-none rounded-xl border border-primary/20 bg-dark-2 p-5 transition group-hover:border-primary/50 group-hover:bg-dark-2/80">
            <div class="flex items-start justify-between gap-4">
              <div class="min-w-0">
                <p class="text-lg font-semibold text-light">{{ soleExchange.partnerInstitutionName }}</p>
                <p class="mt-1 text-sm text-light/50">
                  {{ soleExchange.homeProgramName
                  }}<span v-if="soleExchange.homeProfileName"> &middot; {{ soleExchange.homeProfileName }}</span>
                </p>
              </div>
              <span
                class="shrink-0 rounded-full border px-2.5 py-0.5 text-xs font-semibold"
                :class="statusColorClass[soleExchange.learningAgreementStatus]"
              >
                {{ t(`documentStatus.${soleExchange.learningAgreementStatus}`) }}
              </span>
            </div>

            <div class="mt-4 flex flex-wrap items-center gap-3 text-sm text-light/60">
              <span>{{ soleExchange.academicYear }} &middot; {{ t(`exchangeSemester.${soleExchange.semesterType}`) }}</span>
              <a
                v-if="soleExchange.ewpLink"
                :href="soleExchange.ewpLink"
                target="_blank"
                rel="noopener noreferrer"
                class="pointer-events-auto inline-flex items-center gap-1.5 rounded-lg border border-primary/30 px-2.5 py-1 text-xs font-medium text-primary-light transition hover:border-primary hover:bg-primary/10"
                @click.stop
              >
                <svg width="11" height="11" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M5 2H2a1 1 0 0 0-1 1v7a1 1 0 0 0 1 1h7a1 1 0 0 0 1-1V7" />
                  <path d="M8 1h3v3" /><line x1="11" y1="1" x2="5" y2="7" />
                </svg>
                {{ t('exchange.ewpLink') }}
              </a>
            </div>
          </div>
        </div>

        <!-- Multiple exchanges: table -->
        <div v-else class="mt-4 overflow-x-auto rounded-xl border border-primary/20 bg-dark-2">
          <div class="min-w-[640px]">
            <div class="home-row-grid gap-4 border-b border-primary/20 px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-light/40">
              <span>{{ t('coordinator.table.exchange') }}</span>
              <span>{{ t('coordinator.table.period') }}</span>
              <span class="text-center">{{ t('coordinator.table.learningAgreement') }}</span>
              <span></span>
            </div>

            <div class="divide-y divide-primary/10">
              <div v-for="ex in filteredSummaries" :key="ex.id" class="group relative">
                <RouterLink
                  :to="`/exchange/${ex.guid}`"
                  class="absolute inset-0 z-0 focus-visible:outline focus-visible:outline-2 focus-visible:outline-primary focus-visible:-outline-offset-2"
                  :aria-label="`${ex.partnerInstitutionName} — ${ex.academicYear}`"
                />
                <div class="home-row-grid relative pointer-events-none gap-4 px-4 py-3 transition group-hover:bg-dark">
                  <div class="min-w-0">
                    <p class="truncate text-sm font-semibold text-light">{{ ex.partnerInstitutionName }}</p>
                    <p class="mt-0.5 truncate text-xs text-light/40">
                      {{ ex.homeProgramName
                      }}<span v-if="ex.homeProfileName"> &middot; {{ ex.homeProfileName }}</span>
                    </p>
                  </div>

                  <div class="min-w-0">
                    <p class="truncate text-sm text-light/70">{{ ex.academicYear }}</p>
                    <p class="truncate text-xs text-light/40">{{ t(`exchangeSemester.${ex.semesterType}`) }}</p>
                  </div>

                  <div class="flex justify-center">
                    <span
                      class="rounded-full border px-2.5 py-0.5 text-xs font-semibold"
                      :class="statusColorClass[ex.learningAgreementStatus]"
                    >
                      {{ t(`documentStatus.${ex.learningAgreementStatus}`) }}
                    </span>
                  </div>

                  <div class="flex items-center justify-self-center">
                    <a
                      v-if="ex.ewpLink"
                      :href="ex.ewpLink"
                      target="_blank"
                      rel="noopener noreferrer"
                      :title="t('exchange.ewpLink')"
                      class="pointer-events-auto flex h-7 w-7 shrink-0 items-center justify-center rounded-lg text-light/40 transition hover:bg-primary/10 hover:text-primary-light"
                      @click.stop
                    >
                      <svg width="12" height="12" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M5 2H2a1 1 0 0 0-1 1v7a1 1 0 0 0 1 1h7a1 1 0 0 0 1-1V7" />
                        <path d="M8 1h3v3" /><line x1="11" y1="1" x2="5" y2="7" />
                      </svg>
                    </a>
                    <span v-else class="h-7 w-7 shrink-0"></span>
                  </div>
                </div>
              </div>
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

<style scoped>
.home-row-grid {
  display: grid;
  grid-template-columns: minmax(220px, 1.6fr) 160px 150px 44px;
  align-items: center;
}
</style>
