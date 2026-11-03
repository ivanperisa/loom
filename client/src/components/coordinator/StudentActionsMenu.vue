<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { CoordinatorStudentResponse } from '@/types/coordinator.types'
import { PlusIcon, SquarePenIcon, XIcon } from '@lucide/vue'

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
        <SquarePenIcon class="h-3.5 w-3.5 text-light/50" />
        {{ t('common.edit') }}
      </button>
      <button
        v-if="student.isMyStudent"
        type="button"
        class="flex w-full items-center gap-2 rounded-lg px-2.5 py-1.5 text-left text-sm font-medium text-primary-text transition hover:bg-primary/10"
        @click="emit('createExchange')"
      >
        <PlusIcon class="h-3.5 w-3.5" />
        {{ t('coordinator.createExchange') }}
      </button>
      <button
        v-if="student.isPlaceholder && student.isMyStudent"
        type="button"
        class="flex w-full items-center gap-2 rounded-lg px-2.5 py-1.5 text-left text-sm font-medium text-danger-text transition hover:bg-danger-fill disabled:opacity-50"
        :disabled="deleting"
        @click="emit('delete')"
      >
        <XIcon class="h-3.5 w-3.5" />
        {{ deleting ? t('common.loading') : t('coordinator.deleteStudent') }}
      </button>
      <p v-if="!student.isMyStudent" class="px-2.5 py-1.5 text-xs text-light/40">
        {{ t('coordinator.table.reassignedNote') }}
      </p>
    </div>
  </Teleport>
</template>
