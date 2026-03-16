<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { MappingHistoryResponse } from '@/types/exchange.types'

defineProps<{
  history: MappingHistoryResponse[]
  loading?: boolean
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const { t } = useI18n()

const statusColors: Record<string, string> = {
  Pending: 'bg-yellow-500/20 text-yellow-300',
  Approved: 'bg-green-500/20 text-green-300',
  Rejected: 'bg-red-500/20 text-red-300'
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString()
}
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
    <div class="w-full max-w-lg rounded-xl bg-[#0E2A3D] p-6 shadow-xl max-h-[80vh] flex flex-col">
      <div class="mb-4 flex items-center justify-between">
        <h2 class="text-xl font-semibold text-[#CAE4F7]">{{ t('mappingHistory.title') }}</h2>
        <button @click="emit('close')" class="text-[#8AC4ED] hover:text-white transition">&times;</button>
      </div>

      <div v-if="loading" class="flex-1 flex items-center justify-center text-[#8AC4ED]">
        {{ t('common.loading') }}
      </div>

      <div v-else-if="history.length === 0" class="flex-1 flex items-center justify-center text-[#8AC4ED] text-sm">
        {{ t('mappingHistory.noHistory') }}
      </div>

      <div v-else class="flex-1 overflow-y-auto space-y-3 pr-1">
        <div
          v-for="entry in history"
          :key="entry.id"
          class="rounded-lg bg-[#071C2C] p-4 border border-[#1E4A6E]"
        >
          <div class="mb-2 flex items-center justify-between gap-2">
            <span class="text-xs text-[#8AC4ED]">
              {{ t('mappingHistory.changedBy') }}: <strong class="text-[#CAE4F7]">{{ entry.changedByName }}</strong>
            </span>
            <span class="text-xs text-[#5A8AAD]">{{ formatDate(entry.createdAt) }}</span>
          </div>
          <div class="space-y-1 text-sm">
            <div class="flex items-center gap-2">
              <span
                :class="[statusColors[entry.snapshot.status] ?? 'bg-gray-500/20 text-gray-300', 'rounded px-2 py-0.5 text-xs font-medium']"
              >
                {{ t(`mappingStatus.${entry.snapshot.status}`) }}
              </span>
              <span class="text-[#CAE4F7]">{{ entry.snapshot.courseName }}</span>
              <span v-if="entry.snapshot.courseCode" class="text-[#5A8AAD] text-xs">({{ entry.snapshot.courseCode }})</span>
            </div>
            <div v-if="entry.snapshot.awardedEcts != null" class="text-xs text-[#8AC4ED]">
              {{ t('mapping.awardedEcts') }}: {{ entry.snapshot.awardedEcts }}
            </div>
            <div v-if="entry.snapshot.convertedGrade" class="text-xs text-[#8AC4ED]">
              {{ t('mapping.convertedGrade') }}: {{ entry.snapshot.convertedGrade }}
            </div>
            <div v-if="entry.snapshot.coordinatorNote" class="text-xs text-[#8AC4ED]">
              {{ t('mapping.coordinatorNote') }}: {{ entry.snapshot.coordinatorNote }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
