<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import LearningAgreementPanel from '@/components/exchange/LearningAgreementPanel.vue'
import RecognitionPanel from '@/components/exchange/RecognitionPanel.vue'
import NotesModal from '@/components/exchange/NotesModal.vue'
import { useExchangeStore } from '@/stores/exchange.store'
import { useExchangePermissions } from '@/composables/useExchangePermissions'
import { useAuthStore } from '@/stores/auth.store'
import { useConfirm } from '@/composables/useConfirm'
import { documentStatus } from '@/utils/documentStatus'
import { buildAccessLink } from '@/utils/accessLink'
import { useNotification } from '@/composables/useNotification'

const props = withDefaults(
  defineProps<{
    exchangeId: string
    allowDelete?: boolean
  }>(),
  { allowDelete: true },
)

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const exchangeStore = useExchangeStore()
const { isCoordinator } = useExchangePermissions()
const authStore = useAuthStore()
const { confirm } = useConfirm()
const { notifySuccess } = useNotification()

const VALID_TABS = ['la', 'recognition'] as const
type ExchangeTab = (typeof VALID_TABS)[number]
const activeTab = ref<ExchangeTab>(
  VALID_TABS.includes(route.query.tab as ExchangeTab) ? (route.query.tab as ExchangeTab) : 'la',
)
const deleting = ref(false)


const ewpLinkInput = ref('')
const isEditingEwpLink = ref(false)
const isSavingEwpLink = ref(false)

function startEditingEwpLink() {
  ewpLinkInput.value = exchangeStore.exchange?.ewpLink ?? ''
  isEditingEwpLink.value = true
}

function cancelEditingEwpLink() {
  isEditingEwpLink.value = false
}

async function saveEwpLink() {
  isSavingEwpLink.value = true
  await exchangeStore.updateEwpLink(props.exchangeId, ewpLinkInput.value.trim() || null)
  isEditingEwpLink.value = false
  isSavingEwpLink.value = false
}

async function copyAccessLink() {
  if (!exchangeStore.exchange) return
  await navigator.clipboard.writeText(buildAccessLink(exchangeStore.exchange.guid))
  notifySuccess(t('exchangeAccess.linkCopied'))
}

const canDelete = computed(
  () =>
    props.allowDelete &&
    exchangeStore.exchange &&
    exchangeStore.serverLearningAgreement?.status === documentStatus.Draft &&
    exchangeStore.serverRecognition?.status === documentStatus.Draft,
)

async function confirmDelete() {
  const ok = await confirm({ title: t('home.deleteConfirm') })
  if (!ok) return
  deleting.value = true
  try {
    await exchangeStore.deleteExchange(props.exchangeId)
    router.push(authStore.canActAsCoordinator ? '/coordinator' : '/home')
  } finally {
    deleting.value = false
  }
}

const showNotes = ref(false)
const savingNotes = ref(false)

async function saveNotes(la: string | null, recognition: string | null) {
  savingNotes.value = true
  await Promise.all([
    exchangeStore.updateLaMessage(props.exchangeId, la),
    exchangeStore.updateRecognitionMessage(props.exchangeId, recognition),
  ])
  savingNotes.value = false
  showNotes.value = false
}

watch(
  () => exchangeStore.error,
  (err) => {
    if (err) router.push('/home')
  },
)

watch(activeTab, async (tab) => {
  router.replace({ query: { ...route.query, tab } })
  if (tab === 'recognition') await exchangeStore.fetchRecognition(props.exchangeId)
})

onMounted(async () => {
  await Promise.all([
    exchangeStore.fetchExchange(props.exchangeId),
    exchangeStore.fetchLearningAgreement(props.exchangeId),
    exchangeStore.fetchRecognition(props.exchangeId),
  ])
})
</script>

<template>
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
      <div class="flex items-center gap-2">
        <button
          v-if="exchangeStore.exchange.studentIsPlaceholder"
          type="button"
          class="flex items-center gap-1.5 rounded-lg border border-primary/30 px-2.5 py-1 text-xs font-semibold text-primary-light transition hover:bg-primary/10"
          @click="copyAccessLink"
        >
          <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
            <path d="M12.586 4.586a2 2 0 112.828 2.828l-3 3a2 2 0 01-2.828 0 1 1 0 00-1.414 1.414 4 4 0 005.656 0l3-3a4 4 0 00-5.656-5.656l-1.5 1.5a1 1 0 101.414 1.414l1.5-1.5z" />
            <path d="M7.414 15.414a2 2 0 01-2.828-2.828l3-3a2 2 0 012.828 0 1 1 0 001.414-1.414 4 4 0 00-5.656 0l-3 3a4 4 0 105.656 5.656l1.5-1.5a1 1 0 10-1.414-1.414l-1.5 1.5z" />
          </svg>
          {{ t('exchangeAccess.copyLink') }}
        </button>
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
    </div>

    <!-- Exchange header -->
    <div class="mt-4 rounded-xl border border-primary/20 bg-dark-2 px-5 py-4">
      <p class="text-xs font-semibold uppercase tracking-wide text-light/50">
        {{ t('exchange.partnerInstitution') }}
      </p>
      <p class="mt-1 text-base font-semibold text-light">
        {{ exchangeStore.exchange.partnerInstitutionName }}
      </p>

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
            exchangeStore.exchange.studySemesters.slice().sort((a: number, b: number) => a - b).join(', ')
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

      <!-- EWP link -->
      <div class="mt-2 text-sm">
        <template v-if="isEditingEwpLink">
          <div class="flex items-center gap-2">
            <input
              v-model="ewpLinkInput"
              type="url"
              class="flex-1 rounded-lg border border-primary/20 bg-dark px-3 py-1.5 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
              :placeholder="t('exchange.ewpLinkPlaceholder')"
              @keyup.enter="saveEwpLink"
              @keyup.escape="cancelEditingEwpLink"
            />
            <button type="button" class="text-xs text-primary-light transition hover:text-white" :disabled="isSavingEwpLink" @click="saveEwpLink">
              {{ isSavingEwpLink ? t('common.loading') : t('common.confirm') }}
            </button>
            <button type="button" class="text-xs text-light/50 transition hover:text-light" @click="cancelEditingEwpLink">
              {{ t('common.cancel') }}
            </button>
          </div>
        </template>
        <template v-else>
          <div class="flex items-center gap-2 text-sm">
            <a
              v-if="exchangeStore.exchange.ewpLink"
              :href="exchangeStore.exchange.ewpLink"
              target="_blank"
              rel="noopener noreferrer"
              class="inline-flex items-center gap-1.5 rounded-lg border border-primary/30 px-3 py-1 text-xs font-medium text-primary-light transition hover:border-primary hover:bg-primary/10"
            >
              <svg width="12" height="12" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                <path d="M5 2H2a1 1 0 0 0-1 1v7a1 1 0 0 0 1 1h7a1 1 0 0 0 1-1V7" />
                <path d="M8 1h3v3" /><line x1="11" y1="1" x2="5" y2="7" />
              </svg>
              {{ t('exchange.ewpLink') }}
            </a>
            <button type="button" class="text-xs text-light/30 transition hover:text-primary-light" @click="startEditingEwpLink">
              {{ exchangeStore.exchange.ewpLink ? t('common.edit') : '+ ' + t('exchange.ewpLink') }}
            </button>
          </div>
        </template>
      </div>
    </div>

    <!-- Tabs -->
    <div class="mt-6 flex items-center justify-between border-b border-primary/20">
      <div class="flex">
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
      <button
        v-if="exchangeStore.serverLearningAgreement"
        type="button"
        class="relative mb-1 rounded-lg border border-primary/40 bg-primary/10 px-3 py-1 text-xs font-medium text-light transition hover:bg-primary/20"
        @click="showNotes = true"
      >
        {{ t('exchange.notes') }}
        <span
          v-if="exchangeStore.serverLearningAgreement?.message || exchangeStore.serverRecognition?.message"
          class="absolute -right-1 -top-1 h-2 w-2 rounded-full bg-primary"
        ></span>
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

  <NotesModal
    v-if="showNotes"
    :la-message="exchangeStore.serverLearningAgreement?.message ?? null"
    :recognition-message="exchangeStore.serverRecognition?.message ?? null"
    :saving="savingNotes"
    @save="saveNotes"
    @close="showNotes = false"
  />
</template>
