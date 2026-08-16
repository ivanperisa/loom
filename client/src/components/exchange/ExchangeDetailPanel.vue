<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import LearningAgreementPanel from '@/components/exchange/LearningAgreementPanel.vue'
import RecognitionPanel from '@/components/exchange/RecognitionPanel.vue'
import MappingSchemePanel from '@/components/exchange/MappingSchemePanel.vue'
import NotesModal from '@/components/exchange/NotesModal.vue'
import EditExchangeModal from '@/components/exchange/EditExchangeModal.vue'
import BaseModal from '@/components/common/BaseModal.vue'
import { exchangeService } from '@/services/exchange.service'
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

const VALID_TABS = ['la', 'recognition', 'mappingScheme'] as const
type ExchangeTab = (typeof VALID_TABS)[number]
const activeTab = ref<ExchangeTab>(
  VALID_TABS.includes(route.query.tab as ExchangeTab) ? (route.query.tab as ExchangeTab) : 'la',
)
const deleting = ref(false)

async function copyAccessLink() {
  if (!exchangeStore.exchange) return
  await navigator.clipboard.writeText(buildAccessLink(exchangeStore.exchange.guid))
  notifySuccess(t('exchangeAccess.linkCopied'))
}

const regenerating = ref(false)

async function regenerateAccessLink() {
  const ex = exchangeStore.exchange
  if (!ex) return
  const ok = await confirm({ title: t('exchangeAccess.regenerateConfirm') })
  if (!ok) return
  regenerating.value = true
  try {
    const res = await exchangeService.regenerateAccessLink(props.exchangeId)
    await navigator.clipboard.writeText(buildAccessLink(res.data.guid))
    notifySuccess(t('exchangeAccess.regenerated'))
    // The route is keyed by the old guid; move to the new one.
    router.replace(`/exchange/${res.data.guid}`)
  } finally {
    regenerating.value = false
  }
}

const showEwpModal = ref(false)
const ewpLinkInput = ref('')
const isSavingEwpLink = ref(false)

function openEwpModal() {
  ewpLinkInput.value = exchangeStore.exchange?.ewpLink ?? ''
  showEwpModal.value = true
}

async function saveEwpLink() {
  const ex = exchangeStore.exchange
  if (!ex) return
  isSavingEwpLink.value = true
  try {
    await exchangeStore.updateExchange(props.exchangeId, {
      academicYear: ex.academicYear,
      semesterType: ex.semesterType,
      studySemesters: ex.studySemesters,
      coordinatorId: ex.coordinatorId,
      mentor: ex.mentor,
      ewpLink: ewpLinkInput.value.trim() || null,
    })
    showEwpModal.value = false
  } finally {
    isSavingEwpLink.value = false
  }
}

const showActionsMenu = ref(false)
function closeActionsMenu() {
  showActionsMenu.value = false
}
function handleActionsMenuOutsideClick(e: MouseEvent) {
  if (!(e.target as HTMLElement).closest('[data-menu-anchor]')) closeActionsMenu()
}
onMounted(() => document.addEventListener('click', handleActionsMenuOutsideClick))
onUnmounted(() => document.removeEventListener('click', handleActionsMenuOutsideClick))

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

const showEdit = ref(false)

const laMappedSemesters = computed(() => {
  const la = exchangeStore.serverLearningAgreement
  if (!la) return []
  const slotSemester = new Map(la.slots.map((s) => [s.id, s.semester]))
  const semesters = new Set<number>()
  for (const entry of la.entries) {
    if (entry.isDeleted || entry.partnerCourseId === null) continue
    const sem = slotSemester.get(entry.homeSlotId)
    if (sem !== undefined) semesters.add(sem)
  }
  return Array.from(semesters)
})

async function onExchangeSaved() {
  showEdit.value = false
  await exchangeStore.fetchExchange(props.exchangeId)
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
  if (tab === 'mappingScheme') await exchangeStore.fetchMappingScheme(props.exchangeId)
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
    <!-- Exchange header -->
    <div class="rounded-xl border border-primary/20 bg-dark-2">
      <div class="flex flex-wrap items-center justify-between gap-x-6 gap-y-3 px-4 py-3">
        <div class="flex flex-wrap items-center gap-x-6 gap-y-2">
          <div v-if="isCoordinator && exchangeStore.exchange.studentName">
            <p class="text-lg font-bold text-light">
              {{ exchangeStore.exchange.studentName }}
              <span v-if="exchangeStore.exchange.studentJmbag" class="ml-1.5 text-xs font-normal text-light/40">{{ exchangeStore.exchange.studentJmbag }}</span>
            </p>
            <p class="mt-0.5 text-base font-semibold text-primary-light">
              {{ exchangeStore.exchange.partnerInstitutionName }}
            </p>
          </div>
          <div v-else>
            <p class="text-lg font-bold text-light">
              {{ exchangeStore.exchange.partnerInstitutionName }}
            </p>
          </div>

          <div class="hidden h-9 w-px bg-primary/20 sm:block"></div>

          <div class="flex flex-wrap gap-x-6 gap-y-1">
            <div class="text-sm text-light/50">
              {{ t('exchange.academicYear') }}: <span class="font-semibold text-light">{{ exchangeStore.exchange.academicYear }}</span>
            </div>
            <div class="text-sm text-light/50">
              {{ t('exchange.semester') }}:
              <span class="font-semibold text-light"
                >{{ t(`exchangeSemester.${exchangeStore.exchange.semesterType}`) }} ({{
                  exchangeStore.exchange.studySemesters.slice().sort((a: number, b: number) => a - b).join(', ')
                }})</span
              >
            </div>
            <div class="text-sm text-light/50">
              {{ t('exchange.coordinatorLabel') }}: <span class="font-semibold text-light">{{ exchangeStore.exchange.coordinatorName ?? t('exchange.noCoordinator') }}</span>
            </div>
            <div class="text-sm text-light/50">
              {{ t('exchange.mentor') }}: <span class="font-semibold text-light">{{ exchangeStore.exchange.mentor ?? '-' }}</span>
            </div>
          </div>
        </div>

        <div class="flex shrink-0 items-center gap-2">
          <a
            v-if="exchangeStore.exchange.ewpLink"
            :href="exchangeStore.exchange.ewpLink"
            target="_blank"
            rel="noopener noreferrer"
            class="inline-flex items-center gap-1.5 rounded-lg border border-primary/30 px-3 py-1.5 text-sm font-medium text-primary-light transition hover:border-primary hover:bg-primary/10"
          >
            <svg width="12" height="12" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
              <path d="M5 2H2a1 1 0 0 0-1 1v7a1 1 0 0 0 1 1h7a1 1 0 0 0 1-1V7" />
              <path d="M8 1h3v3" /><line x1="11" y1="1" x2="5" y2="7" />
            </svg>
            {{ t('exchange.ewpLink') }}
          </a>
          <button
            v-else
            type="button"
            class="rounded-lg border border-dashed border-primary/20 px-3 py-1.5 text-sm font-medium text-light/30 transition hover:border-primary/40 hover:text-primary-light"
            @click="openEwpModal"
          >
            + {{ t('exchange.ewpLink') }}
          </button>
          <button
            v-if="exchangeStore.exchange.studentIsPlaceholder"
            type="button"
            class="inline-flex items-center gap-1.5 rounded-lg bg-primary px-3 py-1.5 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
            @click="copyAccessLink"
          >
            <svg width="12" height="12" viewBox="0 0 20 20" fill="currentColor">
              <path d="M12.586 4.586a2 2 0 112.828 2.828l-3 3a2 2 0 01-2.828 0 1 1 0 00-1.414 1.414 4 4 0 005.656 0l3-3a4 4 0 00-5.656-5.656l-1.5 1.5a1 1 0 101.414 1.414l1.5-1.5z" />
              <path d="M7.414 15.414a2 2 0 01-2.828-2.828l3-3a2 2 0 012.828 0 1 1 0 001.414-1.414 4 4 0 00-5.656 0l-3 3a4 4 0 105.656 5.656l1.5-1.5a1 1 0 10-1.414-1.414l-1.5 1.5z" />
            </svg>
            {{ t('exchangeAccess.copyLink') }}
          </button>

          <div class="relative" data-menu-anchor>
            <button
              type="button"
              class="flex h-8 w-8 items-center justify-center rounded-lg text-lg leading-none text-light/40 transition hover:bg-fill hover:text-light"
              :aria-expanded="showActionsMenu"
              aria-haspopup="true"
              @click.stop="showActionsMenu = !showActionsMenu"
            >
              &#8942;
            </button>
            <div
              v-if="showActionsMenu"
              class="absolute right-0 top-full z-10 mt-1 w-52 space-y-0.5 rounded-xl border border-primary/20 bg-dark-2 p-1.5 shadow-2xl shadow-black/50"
            >
              <button
                type="button"
                class="flex w-full items-center gap-2 rounded-lg px-2.5 py-1.5 text-left text-sm font-medium text-primary-light transition hover:bg-fill-soft"
                @click="closeActionsMenu(); showEdit = true"
              >
                <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
                {{ t('common.edit') }}
              </button>
              <button
                v-if="exchangeStore.exchange.studentIsPlaceholder"
                type="button"
                class="flex w-full items-center gap-2 rounded-lg px-2.5 py-1.5 text-left text-sm font-medium text-danger transition hover:bg-danger/10 disabled:opacity-50"
                :disabled="regenerating"
                @click="closeActionsMenu(); regenerateAccessLink()"
              >
                <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
                {{ t('exchangeAccess.regenerate') }}
              </button>
              <button
                v-if="canDelete"
                type="button"
                class="flex w-full items-center gap-2 rounded-lg px-2.5 py-1.5 text-left text-sm font-medium text-danger transition hover:bg-danger/10 disabled:opacity-50"
                :disabled="deleting"
                @click="closeActionsMenu(); confirmDelete()"
              >
                <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
                {{ deleting ? t('common.loading') : t('home.deleteExchange') }}
              </button>
            </div>
          </div>
        </div>
      </div>

    </div>

    <!-- Tabs -->
    <div class="mt-4 flex items-center justify-between border-b border-primary/20">
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
        <button
          type="button"
          class="px-4 py-2.5 text-sm font-semibold transition"
          :class="
            activeTab === 'mappingScheme'
              ? 'border-b-2 border-primary text-primary'
              : 'text-light/60 hover:text-primary-light'
          "
          @click="activeTab = 'mappingScheme'"
        >
          {{ t('exchange.tabs.mappingScheme') }}
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
    <div class="mt-4">
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

      <template v-else-if="activeTab === 'mappingScheme'">
        <MappingSchemePanel
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
    <p class="text-danger">{{ exchangeStore.error }}</p>
  </div>

  <NotesModal
    v-if="showNotes"
    :la-message="exchangeStore.serverLearningAgreement?.message ?? null"
    :recognition-message="exchangeStore.serverRecognition?.message ?? null"
    :saving="savingNotes"
    @save="saveNotes"
    @close="showNotes = false"
  />

  <EditExchangeModal
    v-if="showEdit && exchangeStore.exchange"
    :exchange="exchangeStore.exchange"
    :la-mapped-semesters="laMappedSemesters"
    @saved="onExchangeSaved"
    @close="showEdit = false"
  />

  <!-- EWP link modal -->
  <BaseModal v-if="showEwpModal" max-width="max-w-md" labelled-by="ewp-link-title" @close="showEwpModal = false">
    <div class="p-6">
      <div class="mb-5 flex items-center justify-between">
        <h2 id="ewp-link-title" class="text-base font-semibold text-light">{{ t('exchange.ewpLink') }}</h2>
        <button type="button" class="text-light/40 transition hover:text-light" @click="showEwpModal = false">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
            <line x1="2" y1="2" x2="14" y2="14" /><line x1="14" y1="2" x2="2" y2="14" />
          </svg>
        </button>
      </div>

      <input
        v-model="ewpLinkInput"
        type="url"
        class="w-full rounded-lg border border-primary/20 bg-dark-2 px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
        :placeholder="t('exchange.ewpLinkPlaceholder')"
        @keyup.enter="saveEwpLink"
      />

      <div class="mt-5 flex justify-end gap-2">
        <button
          type="button"
          class="rounded-lg border border-primary/20 px-4 py-1.5 text-sm text-light/70 transition hover:bg-fill-soft hover:text-light"
          @click="showEwpModal = false"
        >
          {{ t('common.cancel') }}
        </button>
        <button
          type="button"
          class="rounded-lg bg-primary px-4 py-1.5 text-sm font-medium text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-60"
          :disabled="isSavingEwpLink"
          @click="saveEwpLink"
        >
          {{ isSavingEwpLink ? t('common.loading') : t('common.save') }}
        </button>
      </div>
    </div>
  </BaseModal>
</template>
