<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import LearningAgreementTable from '@/components/exchange/LearningAgreementTable.vue'
import ForeignCoursePanel from '@/components/exchange/ForeignCoursePanel.vue'
import RecognitionPanel from '@/components/exchange/RecognitionPanel.vue'
import { useExchangeStore } from '@/stores/exchange.store'
import { useAuthStore } from '@/stores/auth.store'
import { exchangeService } from '@/services/exchange.service'
import type { ExchangeSnapshotResponse } from '@/types/exchange.types'

const route = useRoute()
const { t } = useI18n()
const exchangeStore = useExchangeStore()
const authStore = useAuthStore()

const activeTab = ref<'la' | 'recognition' | 'history'>('la')
const history = ref<ExchangeSnapshotResponse[]>([])
const historyLoading = ref(false)
const exchangeId = computed(() => route.params.exchangeId as string)

const isCoordinator = computed(
  () => exchangeStore.exchange?.coordinatorId === authStore.user?.id || authStore.isAdmin
)
const isApproved = computed(() => exchangeStore.exchange?.status === 'Approved')
const isEditable = computed(() => {
  const status = exchangeStore.exchange?.status
  return status === 'Draft'
})

const coordinatorMessage = ref('')
const isEditingMessage = ref(false)
const isSavingMessage = ref(false)

const statusColorClass: Record<string, string> = {
  Draft: 'bg-slate-500/20 text-slate-300 border-slate-400',
  Submitted: 'bg-primary/20 text-primary-light border-primary',
  Approved: 'bg-green-500/20 text-green-300 border-green-400',
  Rejected: 'bg-red-500/20 text-red-300 border-red-400',
}

onMounted(async () => {
  await Promise.all([
    exchangeStore.fetchExchange(exchangeId.value),
    exchangeStore.fetchLearningAgreement(exchangeId.value),
  ])
})

async function submitExchange() {
  await exchangeStore.updateStatus(exchangeId.value, { status: 'Submitted' })
  await exchangeStore.fetchExchange(exchangeId.value)
}

async function backToDraft() {
  await exchangeStore.updateStatus(exchangeId.value, { status: 'Draft' })
  await exchangeStore.fetchExchange(exchangeId.value)
}

async function approveExchange() {
  await exchangeStore.updateStatus(exchangeId.value, { status: 'Approved' })
  await exchangeStore.fetchExchange(exchangeId.value)
}

async function rejectExchange() {
  await exchangeStore.updateStatus(exchangeId.value, {
    status: 'Rejected',
    message: coordinatorMessage.value.trim() || null,
  })
  await exchangeStore.fetchExchange(exchangeId.value)
  isEditingMessage.value = false
}

function startEditingMessage() {
  coordinatorMessage.value = exchangeStore.exchange?.coordinatorMessage ?? ''
  isEditingMessage.value = true
}

function cancelEditingMessage() {
  coordinatorMessage.value = exchangeStore.exchange?.coordinatorMessage ?? ''
  isEditingMessage.value = false
}

async function saveMessage() {
  isSavingMessage.value = true
  await exchangeStore.updateCoordinatorMessage(exchangeId.value, {
    message: coordinatorMessage.value.trim() || null,
  })
  isEditingMessage.value = false
  isSavingMessage.value = false
}

function switchToRecognition() {
  if (isApproved.value) activeTab.value = 'recognition'
}

async function openHistory() {
  activeTab.value = 'history'
  if (history.value.length > 0) return
  historyLoading.value = true
  try {
    const res = await exchangeService.getHistory(exchangeId.value)
    history.value = res.data
  } finally {
    historyLoading.value = false
  }
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('hr', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

</script>

<template>
  <main class="min-h-screen bg-dark">
    <section class="page-container">
      <!-- Loading skeleton -->
      <div v-if="exchangeStore.loading && !exchangeStore.exchange" class="space-y-4">
        <div class="animate-pulse rounded-xl border border-primary/20 bg-dark-2 p-6">
          <div class="h-6 w-64 rounded bg-primary/20"></div>
          <div class="mt-3 h-4 w-96 rounded bg-primary/20"></div>
          <div class="mt-4 grid grid-cols-3 gap-4">
            <div class="h-4 rounded bg-primary/20"></div>
            <div class="h-4 rounded bg-primary/20"></div>
            <div class="h-4 rounded bg-primary/20"></div>
          </div>
        </div>
      </div>

      <!-- Exchange loaded -->
      <template v-else-if="exchangeStore.exchange">
        <!-- Coordinator badge -->
        <div v-if="isCoordinator" class="mb-4 rounded-lg border border-primary/30 bg-primary/10 px-4 py-2 text-sm font-medium text-primary-light">
          {{ t('exchange.coordinatorView') }}
        </div>

        <!-- Status badge -->
        <div class="flex items-center justify-between">
          <h1 class="text-2xl font-bold text-light">{{ t('exchange.title') }}</h1>
          <span
            class="rounded-full border px-3 py-0.5 text-xs font-semibold"
            :class="statusColorClass[exchangeStore.exchange.status] ?? statusColorClass.Draft"
          >
            {{ t(`exchangeStatus.${exchangeStore.exchange.status}`) }}
          </span>
        </div>

        <!-- Two-column: Foreign (left) + Home (right) -->
        <div class="mt-4 grid gap-4 sm:grid-cols-2">
          <!-- Foreign institution -->
          <div class="rounded-xl border border-primary/20 bg-dark-2 p-5">
            <h3 class="mb-3 text-xs font-semibold uppercase tracking-wide text-light/60">{{ t('exchange.foreignInstitution') }}</h3>
            <dl class="space-y-2 text-sm">
              <div>
                <dt class="text-light/60">{{ t('exchange.institution') }}</dt>
                <dd class="font-medium text-light">{{ exchangeStore.exchange.foreignProgram.institutionName }}</dd>
              </div>
              <div>
                <dt class="text-light/60">{{ t('exchange.program') }}</dt>
                <dd class="font-medium text-light">{{ exchangeStore.exchange.foreignProgram.name }}</dd>
              </div>
            </dl>
          </div>

          <!-- Home institution -->
          <div class="rounded-xl border border-primary/20 bg-dark-2 p-5">
            <h3 class="mb-3 text-xs font-semibold uppercase tracking-wide text-light/60">{{ t('exchange.homeInstitution') }}</h3>
            <dl class="space-y-2 text-sm">
              <div>
                <dt class="text-light/60">{{ t('exchange.institution') }}</dt>
                <dd class="font-medium text-light">{{ exchangeStore.exchange.homeInstitutionName }}</dd>
              </div>
              <div>
                <dt class="text-light/60">{{ t('exchange.program') }}</dt>
                <dd class="font-medium text-light">{{ exchangeStore.exchange.studyProgramName }}</dd>
              </div>
              <div>
                <dt class="text-light/60">{{ t('exchange.profile') }}</dt>
                <dd class="font-medium text-light">{{ exchangeStore.exchange.studyProfile.name }}</dd>
              </div>
            </dl>
          </div>
        </div>

        <!-- Exchange details -->
        <div class="mt-4 rounded-xl border border-primary/20 bg-dark-2 p-5">
          <dl class="grid grid-cols-2 gap-x-6 gap-y-2 text-sm sm:grid-cols-4">
            <div>
              <dt class="text-light/60">{{ t('exchange.academicYear') }}</dt>
              <dd class="font-medium text-light">{{ exchangeStore.exchange.academicYear }}</dd>
            </div>
            <div>
              <dt class="text-light/60">{{ t('exchange.semester') }}</dt>
              <dd class="font-medium text-light">{{ t(`exchangeSemester.${exchangeStore.exchange.semesterType}`) }}</dd>
            </div>
            <div>
              <dt class="text-light/60">{{ t('exchange.studySemester') }}</dt>
              <dd class="font-medium text-light">{{ exchangeStore.exchange.studySemester }}</dd>
            </div>
            <!-- Coordinator sees student name -->
            <div v-if="isCoordinator && exchangeStore.exchange.studentName">
              <dt class="text-light/60">{{ t('exchange.student') }}</dt>
              <dd class="font-medium text-light">{{ exchangeStore.exchange.studentName }}</dd>
            </div>
          </dl>

          <!-- Mentor & Coordinator (read-only, managed in Settings) -->
          <dl class="mt-4 grid grid-cols-2 gap-x-6 gap-y-2 border-t border-primary/20 pt-4 text-sm sm:grid-cols-4">
            <div>
              <dt class="text-light/60">{{ t('exchange.coordinatorLabel') }}</dt>
              <dd class="font-medium text-light">{{ exchangeStore.exchange.coordinatorName ?? t('exchange.noCoordinator') }}</dd>
            </div>
            <div>
              <dt class="text-light/60">{{ t('exchange.mentor') }}</dt>
              <dd class="font-medium text-light">{{ exchangeStore.exchange.mentor ?? '-' }}</dd>
            </div>
          </dl>

          <!-- Coordinator message (visible to student) -->
          <div
            v-if="!isCoordinator && exchangeStore.exchange.coordinatorMessage"
            class="mt-4 rounded-lg border border-amber-400/40 bg-amber-500/10 px-4 py-3"
          >
            <p class="text-xs font-semibold uppercase tracking-wide text-amber-400">{{ t('exchange.coordinatorMessage') }}</p>
            <p class="mt-1 text-sm text-amber-200 whitespace-pre-wrap">{{ exchangeStore.exchange.coordinatorMessage }}</p>
          </div>

          <!-- Coordinator message (editable by coordinator) -->
          <div v-if="isCoordinator" class="mt-4">
            <template v-if="isEditingMessage">
              <label class="block text-xs font-semibold uppercase tracking-wide text-primary-light mb-1">{{ t('exchange.coordinatorMessage') }}</label>
              <textarea
                v-model="coordinatorMessage"
                rows="3"
                class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder-light/60 focus:border-primary focus:outline-none"
                :placeholder="t('exchange.coordinatorMessagePlaceholder')"
              ></textarea>
              <div class="mt-2 flex gap-2">
                <button
                  type="button"
                  class="rounded-lg bg-primary px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-60"
                  :disabled="isSavingMessage"
                  @click="saveMessage"
                >
                  {{ isSavingMessage ? t('common.loading') : t('exchange.saveMessage') }}
                </button>
                <button
                  type="button"
                  class="rounded-lg border border-slate-500 px-3 py-1.5 text-xs text-slate-200 transition hover:bg-slate-700/40"
                  @click="cancelEditingMessage"
                >
                  {{ t('common.cancel') }}
                </button>
              </div>
            </template>
            <template v-else>
              <div v-if="exchangeStore.exchange.coordinatorMessage" class="rounded-lg border border-amber-400/40 bg-amber-500/10 px-4 py-3">
                <div class="flex items-start justify-between gap-2">
                  <div>
                    <p class="text-xs font-semibold uppercase tracking-wide text-amber-400">{{ t('exchange.coordinatorMessage') }}</p>
                    <p class="mt-1 text-sm text-amber-200 whitespace-pre-wrap">{{ exchangeStore.exchange.coordinatorMessage }}</p>
                  </div>
                  <button
                    type="button"
                    class="shrink-0 text-xs text-primary-light hover:text-white transition"
                    @click="startEditingMessage"
                  >
                    {{ t('exchange.editMessage') }}
                  </button>
                </div>
              </div>
              <button
                v-else
                type="button"
                class="text-xs text-light/60 hover:text-primary-light transition"
                @click="startEditingMessage"
              >
                + {{ t('exchange.addMessage') }}
              </button>
            </template>
          </div>

          <!-- Student actions -->
          <div v-if="!isCoordinator" class="mt-4">
            <button
              v-if="exchangeStore.exchange.status === 'Draft'"
              type="button"
              class="rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
              @click="submitExchange"
            >
              {{ t('exchange.actions.submit') }}
            </button>
            <div
              v-else-if="exchangeStore.exchange.status === 'Submitted'"
              class="flex items-center gap-3"
            >
              <span class="inline-block rounded-lg border border-primary/20 px-4 py-2 text-sm text-light/60">
                {{ t('exchange.status.waitingApproval') }}
              </span>
              <button
                type="button"
                class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40"
                @click="backToDraft"
              >
                {{ t('exchange.actions.backToDraft') }}
              </button>
            </div>
            <button
              v-else-if="exchangeStore.exchange.status === 'Rejected'"
              type="button"
              class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40"
              @click="backToDraft"
            >
              {{ t('exchange.actions.backToDraft') }}
            </button>
          </div>

          <!-- Coordinator actions -->
          <div v-if="isCoordinator" class="mt-4 flex gap-3">
            <template v-if="exchangeStore.exchange.status === 'Submitted'">
              <button
                type="button"
                class="rounded-lg bg-green-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-green-500"
                @click="approveExchange"
              >
                {{ t('exchange.actions.approve') }}
              </button>
              <button
                type="button"
                class="rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-red-500"
                @click="rejectExchange"
              >
                {{ t('exchange.actions.reject') }}
              </button>
            </template>
            <button
              v-if="exchangeStore.exchange.status === 'Approved' || exchangeStore.exchange.status === 'Rejected'"
              type="button"
              class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40"
              @click="backToDraft"
            >
              {{ t('exchange.actions.backToDraft') }}
            </button>
          </div>
        </div>

        <!-- Tabs -->
        <div class="mt-6 flex border-b border-primary/20">
          <button
            type="button"
            class="px-4 py-2.5 text-sm font-semibold transition"
            :class="
              activeTab === 'la'
                ? 'border-b-2 border-primary text-primary'
                : 'text-light/60 hover:text-primary-light'
            "
            @click="activeTab = 'la'"
          >
            {{ t('exchange.tabs.learningAgreement') }}
          </button>
          <button
            type="button"
            class="px-4 py-2.5 text-sm font-semibold transition"
            :class="
              activeTab === 'recognition'
                ? 'border-b-2 border-primary text-primary'
                : isApproved
                  ? 'text-light/60 hover:text-primary-light'
                  : 'cursor-not-allowed text-slate-600'
            "
            :disabled="!isApproved"
            @click="switchToRecognition"
          >
            {{ t('exchange.tabs.recognition') }}
          </button>
          <button
            type="button"
            class="px-4 py-2.5 text-sm font-semibold transition"
            :class="activeTab === 'history' ? 'border-b-2 border-primary text-primary' : 'text-light/60 hover:text-primary-light'"
            @click="openHistory"
          >
            {{ t('exchange.tabs.history') }}
          </button>
        </div>

        <!-- Tab content -->
        <div class="mt-6">
          <template v-if="activeTab === 'la'">
            <!-- LA table: always full width -->
            <LearningAgreementTable
              v-if="exchangeStore.learningAgreement"
              :learning-agreement="exchangeStore.learningAgreement"
              :exchange-id="exchangeId"
              :readonly="!isEditable"
            />

            <!-- Course panels below table (Draft/Rejected only) -->
            <div v-if="isEditable && exchangeStore.exchange" class="mt-6 flex gap-6 items-start">
              <!-- Left: available courses -->
              <div class="min-w-0 flex-1 rounded-xl border border-primary/20 bg-dark-2 p-4">
                <h3 class="mb-2 text-sm font-semibold text-primary-light">{{ t('foreignCourses.availableCourses') }}</h3>
                <p class="mb-3 text-xs text-light/60">{{ t('foreignCourses.dragHint') }}</p>
                <ForeignCoursePanel
                  :foreign-program-id="exchangeStore.exchange.foreignProgram.id"
                  :exchange-id="exchangeId"
                  variant="available"
                />
              </div>
              <!-- Right: mapped courses -->
              <div class="w-80 shrink-0 rounded-xl border border-primary/20 bg-dark-2 p-4">
                <h3 class="mb-2 text-sm font-semibold text-green-400">{{ t('foreignCourses.mappedCourses') }}</h3>
                <ForeignCoursePanel
                  :foreign-program-id="exchangeStore.exchange.foreignProgram.id"
                  :exchange-id="exchangeId"
                  variant="mapped"
                />
              </div>
            </div>
          </template>

          <template v-else-if="activeTab === 'recognition'">
            <div v-if="!isApproved" class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center">
              <p class="text-light/60">{{ t('recognition.notApproved') }}</p>
            </div>
            <RecognitionPanel
              v-else
              :exchange-id="exchangeId"
              :exchange="exchangeStore.exchange!"
              :learning-agreement="exchangeStore.learningAgreement!"
            />
          </template>

          <template v-else-if="activeTab === 'history'">
            <!-- Loading -->
            <div v-if="historyLoading" class="space-y-2">
              <div v-for="i in 4" :key="i" class="h-12 animate-pulse rounded-lg bg-primary/20"></div>
            </div>

            <!-- Empty -->
            <div v-else-if="history.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center">
              <p class="text-light/60">{{ t('exchange.history.empty') }}</p>
            </div>

            <!-- Timeline -->
            <div v-else class="relative space-y-0">
              <!-- Vertical line -->
              <div class="absolute left-[11px] top-4 bottom-4 w-px bg-primary/20"></div>

              <div
                v-for="(snap, idx) in history"
                :key="snap.id"
                class="relative flex gap-4 py-3"
              >
                <!-- Dot -->
                <div class="mt-0.5 h-6 w-6 shrink-0 rounded-full border-2 flex items-center justify-center"
                  :class="snap.phase === 'LearningAgreement' ? 'border-primary bg-dark' : 'border-green-500 bg-dark'"
                >
                  <div class="h-2 w-2 rounded-full" :class="snap.phase === 'LearningAgreement' ? 'bg-primary' : 'bg-green-500'"></div>
                </div>

                <!-- Content -->
                <div class="flex-1 rounded-lg border border-primary/20 bg-dark-2 px-4 py-3">
                  <div class="flex flex-wrap items-center justify-between gap-2">
                    <div class="flex items-center gap-2">
                      <span
                        class="rounded-full border px-2 py-0.5 text-[10px] font-semibold"
                        :class="snap.phase === 'LearningAgreement'
                          ? 'border-primary/40 bg-primary/10 text-primary-light'
                          : 'border-green-500/40 bg-green-500/10 text-green-400'"
                      >
                        {{ t(`snapshotPhase.${snap.phase}`) }}
                      </span>
                      <span class="text-sm font-medium text-light">{{ snap.changedByName }}</span>
                    </div>
                    <span class="text-xs text-light/50">{{ formatDate(snap.createdAt) }}</span>
                  </div>
                </div>
              </div>
            </div>
          </template>
        </div>
      </template>

      <!-- Error -->
      <div v-else-if="exchangeStore.error" class="rounded-xl border border-red-400/30 bg-red-900/20 p-6 text-center">
        <p class="text-red-300">{{ exchangeStore.error }}</p>
      </div>
    </section>
  </main>
</template>
