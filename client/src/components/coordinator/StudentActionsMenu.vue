<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { CoordinatorStudentResponse } from '@/types/coordinator.types'

/** Row actions. Editing and deleting are for placeholder students the coordinator owns. */
defineProps<{ student: CoordinatorStudentResponse; position: { top: number; left: number }; deleting: boolean }>()
const emit = defineEmits<{ edit: []; createExchange: []; delete: [] }>()

const { t } = useI18n()
</script>

<template>
  <Teleport to="body">
    <div
        data-menu-anchor
      class="fixed z-50 w-[200px] space-y-0.5 rounded-xl border border-primary/20 bg-dark-2 p-1.5 shadow-2xl shadow-black/50"
      :style="{ top: position.top + 'px', left: position.left + 'px' }"
    >
      <button
        v-if="student.isPlaceholder && student.isMyStudent"
        type="button"
        class="flex w-full items-center gap-2 rounded-lg px-2.5 py-1.5 text-left text-sm font-medium text-light transition hover:bg-fill-soft"
        @click="emit('edit')"
      >
        <svg class="h-3.5 w-3.5 text-light/50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
        </svg>
        {{ t('common.edit') }}
      </button>
      <button
        v-if="student.isMyStudent"
        type="button"
        class="flex w-full items-center gap-2 rounded-lg px-2.5 py-1.5 text-left text-sm font-medium text-primary-text transition hover:bg-primary/10"
        @click="emit('createExchange')"
      >
        <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        {{ t('coordinator.createExchange') }}
      </button>
      <button
        v-if="student.isPlaceholder && student.isMyStudent"
        type="button"
        class="flex w-full items-center gap-2 rounded-lg px-2.5 py-1.5 text-left text-sm font-medium text-danger-text transition hover:bg-danger-fill disabled:opacity-50"
        :disabled="deleting"
        @click="emit('delete')"
      >
        <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
        {{ deleting ? t('common.loading') : t('coordinator.deleteStudent') }}
      </button>
      <p v-if="!student.isMyStudent" class="px-2.5 py-1.5 text-xs text-light/40">
        {{ t('coordinator.table.reassignedNote') }}
      </p>
    </div>
  </Teleport>
</template>
