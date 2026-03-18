<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { useI18n } from 'vue-i18n'
import { recognitionService } from '@/services/recognition.service'
import { useAuthStore } from '@/stores/auth.store'
import type { RecognitionResponse, RecognitionEntryResponse } from '@/types/recognition.types'

const props = defineProps<{
  exchangeId: string
}>()

const { t } = useI18n()
const authStore = useAuthStore()

const recognition = ref<RecognitionResponse | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const savingEntryId = ref<string | null>(null)

const isCoordinator = ref(authStore.role === 'Coordinator')

const statusColorClass: Record<string, string> = {
  Draft: 'bg-slate-500/20 text-slate-300 border-slate-400',
  Submitted: 'bg-[#218CD9]/20 text-[#8AC4ED] border-[#218CD9]',
  Approved: 'bg-green-500/20 text-green-300 border-green-400',
  Rejected: 'bg-red-500/20 text-red-300 border-red-400',
}

// Editable entry data keyed by entry id
const editableEntries = reactive<Record<string, {
  enrollmentStatus: string
  originalGrade: string
  ectsGrade: string
  hrGrade: string
  examDate: string
}>>({})

function initEditableEntry(entry: RecognitionEntryResponse) {
  editableEntries[entry.id] = {
    enrollmentStatus: entry.enrollmentStatus ?? '',
    originalGrade: entry.originalGrade ?? '',
    ectsGrade: entry.ectsGrade ?? '',
    hrGrade: entry.hrGrade ?? '',
    examDate: entry.examDate ?? '',
  }
}

onMounted(async () => {
  try {
    const res = await recognitionService.getOrCreate(props.exchangeId)
    recognition.value = res.data
    for (const entry of res.data.entries) {
      initEditableEntry(entry)
    }
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
})

async function saveEntry(entry: RecognitionEntryResponse) {
  const editable = editableEntries[entry.id]
  if (!editable) return

  savingEntryId.value = entry.id
  try {
    const res = await recognitionService.upsertEntry(props.exchangeId, {
      slotMappingId: entry.slotMappingId,
      enrollmentStatus: editable.enrollmentStatus || null,
      originalGrade: editable.originalGrade || null,
      ectsGrade: editable.ectsGrade || null,
      hrGrade: editable.hrGrade || null,
      examDate: editable.examDate || null,
    })
    recognition.value = res.data
    for (const e of res.data.entries) {
      initEditableEntry(e)
    }
  } catch {
    error.value = t('common.error')
  } finally {
    savingEntryId.value = null
  }
}

async function submitRecognition() {
  try {
    const res = await recognitionService.updateStatus(props.exchangeId, { status: 'Submitted' })
    recognition.value = res.data
  } catch {
    error.value = t('common.error')
  }
}

async function approveRecognition() {
  try {
    const res = await recognitionService.updateStatus(props.exchangeId, { status: 'Approved' })
    recognition.value = res.data
  } catch {
    error.value = t('common.error')
  }
}

async function rejectRecognition() {
  try {
    const res = await recognitionService.updateStatus(props.exchangeId, { status: 'Rejected' })
    recognition.value = res.data
  } catch {
    error.value = t('common.error')
  }
}
</script>

<template>
  <div>
    <!-- Loading -->
    <div v-if="loading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="h-20 animate-pulse rounded-lg bg-[#1E4A6E]"></div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="rounded-xl border border-red-400/30 bg-red-900/20 p-6 text-center">
      <p class="text-red-300">{{ error }}</p>
    </div>

    <!-- Recognition -->
    <template v-else-if="recognition">
      <!-- Status + action -->
      <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
        <span
          class="rounded-full border px-3 py-0.5 text-xs font-semibold"
          :class="statusColorClass[recognition.status] ?? statusColorClass.Draft"
        >
          {{ t(`recognitionStatus.${recognition.status}`) }}
        </span>
        <div class="flex gap-2">
          <button
            v-if="!isCoordinator && recognition.status === 'Draft'"
            type="button"
            class="rounded-lg bg-[#218CD9] px-4 py-2 text-sm font-semibold text-white transition hover:bg-[#8AC4ED] hover:text-[#071C2C]"
            @click="submitRecognition"
          >
            {{ t('recognition.actions.submit') }}
          </button>
          <template v-if="isCoordinator && recognition.status === 'Submitted'">
            <button
              type="button"
              class="rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-green-700"
              @click="approveRecognition"
            >
              {{ t('recognition.actions.approve') }}
            </button>
            <button
              type="button"
              class="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-red-700"
              @click="rejectRecognition"
            >
              {{ t('recognition.actions.reject') }}
            </button>
          </template>
        </div>
      </div>

      <!-- No entries -->
      <div v-if="recognition.entries.length === 0" class="rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-8 text-center">
        <p class="text-[#5A8AAD]">{{ t('recognition.noEntries') }}</p>
      </div>

      <!-- Entries -->
      <div v-else class="space-y-3">
        <div
          v-for="entry in recognition.entries"
          :key="entry.id"
          class="rounded-lg border border-[#1E4A6E] bg-[#0A2235] p-4"
        >
          <div class="mb-3 flex flex-wrap items-start justify-between gap-2">
            <div>
              <div class="text-sm font-medium text-[#CAE4F7]">
                <span class="font-mono text-[#5A8AAD]">{{ entry.foreignCourseCode }}</span>
                {{ entry.foreignCourseNameEn }}
              </div>
              <div class="text-xs text-[#5A8AAD]">
                {{ entry.courseSlotName }} &middot; {{ entry.awardedEcts }} ECTS
              </div>
            </div>
          </div>

          <template v-if="editableEntries[entry.id]">
            <div class="grid grid-cols-2 gap-3 sm:grid-cols-5">
              <div>
                <label class="mb-0.5 block text-[10px] font-medium text-[#5A8AAD]">{{ t('exchangeCourse.statusLabel') }}</label>
                <input
                  v-model="editableEntries[entry.id]!.enrollmentStatus"
                  type="text"
                  class="w-full rounded border border-[#1E4A6E] bg-[#071C2C] px-2 py-1 text-xs text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
                />
              </div>
              <div>
                <label class="mb-0.5 block text-[10px] font-medium text-[#5A8AAD]">{{ t('exchangeCourse.originalGrade') }}</label>
                <input
                  v-model="editableEntries[entry.id]!.originalGrade"
                  type="text"
                  class="w-full rounded border border-[#1E4A6E] bg-[#071C2C] px-2 py-1 text-xs text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
                />
              </div>
              <div>
                <label class="mb-0.5 block text-[10px] font-medium text-[#5A8AAD]">{{ t('exchangeCourse.ectsGrade') }}</label>
                <input
                  v-model="editableEntries[entry.id]!.ectsGrade"
                  type="text"
                  class="w-full rounded border border-[#1E4A6E] bg-[#071C2C] px-2 py-1 text-xs text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
                />
              </div>
              <div>
                <label class="mb-0.5 block text-[10px] font-medium text-[#5A8AAD]">{{ t('mapping.convertedGrade') }}</label>
                <input
                  v-model="editableEntries[entry.id]!.hrGrade"
                  type="text"
                  class="w-full rounded border border-[#1E4A6E] bg-[#071C2C] px-2 py-1 text-xs text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
                />
              </div>
              <div>
                <label class="mb-0.5 block text-[10px] font-medium text-[#5A8AAD]">{{ t('exchangeCourse.examDate') }}</label>
                <input
                  v-model="editableEntries[entry.id]!.examDate"
                  type="date"
                  class="w-full rounded border border-[#1E4A6E] bg-[#071C2C] px-2 py-1 text-xs text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
                />
              </div>
            </div>
            <div class="mt-2 flex justify-end">
              <button
                type="button"
                :disabled="savingEntryId === entry.id"
                class="rounded bg-[#218CD9] px-3 py-1 text-xs font-semibold text-white transition hover:bg-[#8AC4ED] hover:text-[#071C2C] disabled:opacity-50"
                @click="saveEntry(entry)"
              >
                {{ savingEntryId === entry.id ? t('common.loading') : t('settings.institutions.save') }}
              </button>
            </div>
          </template>
        </div>
      </div>
    </template>
  </div>
</template>
