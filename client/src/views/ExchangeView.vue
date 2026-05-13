<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import LearningAgreementPanel from '@/components/exchange/LearningAgreementPanel.vue'
import RecognitionPanel from '@/components/exchange/RecognitionPanel.vue'
import { useExchangeStore } from '@/stores/exchange.store'
import { useExchangePermissions } from '@/composables/useExchangePermissions'
import { useConfirm } from '@/composables/useConfirm'
import { documentStatus } from '@/utils/documentStatus'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const exchangeStore = useExchangeStore()
const { isCoordinator } = useExchangePermissions()
const { confirm } = useConfirm()

const activeTab = ref<'la' | 'recognition'>('la')
const exchangeId = computed(() => route.params.exchangeId as string)
const deleting = ref(false)

const canDelete = computed(
  () =>
    exchangeStore.exchange &&
    exchangeStore.serverLearningAgreement?.status === documentStatus.Draft &&
    exchangeStore.serverRecognition?.status === documentStatus.Draft,
)

async function confirmDelete() {
  const ok = await confirm({ title: t('home.deleteConfirm') })
  if (!ok) return
  deleting.value = true
  try {
    await exchangeStore.deleteExchange(exchangeId.value)
    router.push('/home')
  } finally {
    deleting.value = false
  }
}

onMounted(async () => {
  await Promise.all([
    exchangeStore.fetchExchange(exchangeId.value),
    exchangeStore.fetchLearningAgreement(exchangeId.value),
    exchangeStore.fetchRecognition(exchangeId.value),
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
        <div class="flex items-center justify-between">
          <h1 class="text-2xl font-bold text-light">{{ t('exchange.title') }}</h1>
          <button
            v-if="canDelete"
            type="button"
            class="rounded-lg border border-red-400/50 px-3 py-1.5 text-sm font-medium text-red-300 transition hover:bg-red-500/20 disabled:opacity-50"
            :disabled="deleting"
            @click="confirmDelete"
          >
            {{ deleting ? t('common.loading') : t('home.deleteExchange') }}
          </button>
        </div>

        <!-- Exchange header -->
        <div class="mt-4 rounded-xl border border-primary/20 bg-dark-2 px-5 py-4">
          <p class="text-xs font-semibold uppercase tracking-wide text-light/50">
            {{ t('exchange.partnerInstitution') }}
          </p>
          <p class="mt-1 text-base font-semibold text-light">
            {{ exchangeStore.exchange.partnerProgram.institutionName }}
          </p>
          <p class="text-sm text-light/70">{{ exchangeStore.exchange.partnerProgram.name }}</p>

          <div class="my-3 border-t border-primary/15"></div>

          <div class="flex flex-wrap gap-x-6 gap-y-1.5 text-sm">
            <span class="text-light/50"
              >{{ t('exchange.academicYear') }}:
              <span class="font-medium text-light">{{
                exchangeStore.exchange.academicYear
              }}</span></span
            >
            <span class="text-light/50"
              >{{ t('exchange.semester') }}:
              <span class="font-medium text-light">{{
                t(`exchangeSemester.${exchangeStore.exchange.semesterType}`)
              }}</span></span
            >
            <span class="text-light/50"
              >{{ t('exchange.studySemester') }}:
              <span class="font-medium text-light">{{
                exchangeStore.exchange.studySemester
              }}</span></span
            >
            <span v-if="isCoordinator && exchangeStore.exchange.studentName" class="text-light/50"
              >{{ t('exchange.student') }}:
              <span class="font-medium text-light">{{
                exchangeStore.exchange.studentName
              }}</span></span
            >
          </div>

          <div class="mt-1.5 flex flex-wrap gap-x-6 gap-y-1 text-sm text-light/40">
            <span
              >{{ t('exchange.coordinatorLabel') }}:
              <span class="text-light/60">{{
                exchangeStore.exchange.coordinatorName ?? t('exchange.noCoordinator')
              }}</span></span
            >
            <span
              >{{ t('exchange.mentor') }}:
              <span class="text-light/60">{{ exchangeStore.exchange.mentor ?? '-' }}</span></span
            >
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
              :home-profile-name="exchangeStore.exchange.homeProfile.name"
            />
          </template>

          <template v-else-if="activeTab === 'recognition'">
            <RecognitionPanel
              :exchange-id="exchangeId"
              :home-profile-name="exchangeStore.exchange.homeProfile.name"
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
