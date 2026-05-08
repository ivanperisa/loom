<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import LearningAgreementPanel from '@/components/exchange/LearningAgreementPanel.vue'
import RecognitionPanel from '@/components/exchange/RecognitionPanel.vue'
import { useExchangeStore } from '@/stores/exchange.store'
import { useExchangePermissions } from '@/composables/useExchangePermissions'

const route = useRoute()
const { t } = useI18n()
const exchangeStore = useExchangeStore()
const { isCoordinator } = useExchangePermissions()

const activeTab = ref<'la' | 'recognition'>('la')
const exchangeId = computed(() => route.params.exchangeId as string)

onMounted(async () => {
  await Promise.all([
    exchangeStore.fetchExchange(exchangeId.value),
    exchangeStore.fetchLearningAgreement(exchangeId.value),
  ])
})
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
        <div
          v-if="isCoordinator"
          class="mb-4 rounded-lg border border-primary/30 bg-primary/10 px-4 py-2 text-sm font-medium text-primary-light"
        >
          {{ t('exchange.coordinatorView') }}
        </div>

        <h1 class="text-2xl font-bold text-light">{{ t('exchange.title') }}</h1>

        <!-- Foreign institution -->
        <div class="mt-4 rounded-xl border border-primary/20 bg-dark-2 p-5">
          <h3 class="mb-3 text-xs font-semibold uppercase tracking-wide text-light/60">
            {{ t('exchange.foreignInstitution') }}
          </h3>
          <dl class="space-y-2 text-sm">
            <div>
              <dt class="text-light/60">{{ t('exchange.institution') }}</dt>
              <dd class="font-medium text-light">
                {{ exchangeStore.exchange.foreignProgram.institutionName }}
              </dd>
            </div>
            <div>
              <dt class="text-light/60">{{ t('exchange.program') }}</dt>
              <dd class="font-medium text-light">
                {{ exchangeStore.exchange.foreignProgram.name }}
              </dd>
            </div>
          </dl>
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
              <dd class="font-medium text-light">
                {{ t(`exchangeSemester.${exchangeStore.exchange.semesterType}`) }}
              </dd>
            </div>
            <div>
              <dt class="text-light/60">{{ t('exchange.studySemester') }}</dt>
              <dd class="font-medium text-light">{{ exchangeStore.exchange.studySemester }}</dd>
            </div>
            <div v-if="isCoordinator && exchangeStore.exchange.studentName">
              <dt class="text-light/60">{{ t('exchange.student') }}</dt>
              <dd class="font-medium text-light">{{ exchangeStore.exchange.studentName }}</dd>
            </div>
          </dl>

          <dl
            class="mt-4 grid grid-cols-2 gap-x-6 gap-y-2 border-t border-primary/20 pt-4 text-sm sm:grid-cols-4"
          >
            <div>
              <dt class="text-light/60">{{ t('exchange.coordinatorLabel') }}</dt>
              <dd class="font-medium text-light">
                {{ exchangeStore.exchange.coordinatorName ?? t('exchange.noCoordinator') }}
              </dd>
            </div>
            <div>
              <dt class="text-light/60">{{ t('exchange.mentor') }}</dt>
              <dd class="font-medium text-light">{{ exchangeStore.exchange.mentor ?? '-' }}</dd>
            </div>
          </dl>
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
                : 'text-light/60 hover:text-primary-light'
            "
            @click="activeTab = 'recognition'"
          >
            {{ t('exchange.tabs.recognition') }}
          </button>
        </div>

        <!-- Tab content -->
        <div class="mt-6">
          <template v-if="activeTab === 'la'">
            <LearningAgreementPanel
              :exchange-id="exchangeId"
              :study-profile-name="exchangeStore.exchange.studyProfile.name"
            />
          </template>

          <template v-else-if="activeTab === 'recognition'">
            <RecognitionPanel
              :exchange-id="exchangeId"
              :exchange="exchangeStore.exchange!"
              :learning-agreement="exchangeStore.serverLearningAgreement!"
              :study-profile-name="exchangeStore.exchange.studyProfile.name"
            />
          </template>
        </div>
      </template>

      <!-- Error -->
      <div
        v-else-if="exchangeStore.error"
        class="rounded-xl border border-red-400/30 bg-red-900/20 p-6 text-center"
      >
        <p class="text-red-300">{{ exchangeStore.error }}</p>
      </div>
    </section>
  </main>
</template>
