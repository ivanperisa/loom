<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import SearchInput from '@/components/common/SearchInput.vue'

defineProps<{
  search: string
  showDeleted: boolean
  hasDeleted: boolean
  mergeSelecting: boolean
  canMerge: boolean
  selectedCount: number
}>()
const emit = defineEmits<{
  'update:search': [value: string]
  'update:showDeleted': [value: boolean]
  'start-merge': []
  'confirm-merge': []
  'cancel-merge': []
}>()

const { t } = useI18n()
</script>

<template>
  <div class="mb-2 flex items-center justify-between gap-2">
    <SearchInput
      :model-value="search"
      :placeholder="t('admin.institutions.searchCourses')"
      class="flex-1"
      @update:model-value="emit('update:search', $event)"
    />
    <label v-if="hasDeleted" class="flex shrink-0 items-center gap-2 text-xs text-light/60">
      <input
        type="checkbox"
        class="accent-primary"
        :checked="showDeleted"
        @change="emit('update:showDeleted', ($event.target as HTMLInputElement).checked)"
      />
      {{ t('admin.institutions.showDeleted') }}
    </label>
    <button
      v-if="!mergeSelecting && canMerge"
      type="button"
      class="shrink-0 rounded-lg border border-primary/30 px-3 py-1.5 text-xs font-medium text-primary-light transition hover:bg-primary/10"
      @click="emit('start-merge')"
    >
      {{ t('admin.institutions.mergeCourses') }}
    </button>
    <template v-else-if="mergeSelecting">
      <button
        type="button"
        class="shrink-0 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-40"
        :disabled="selectedCount < 2"
        @click="emit('confirm-merge')"
      >
        {{ t('admin.institutions.mergeSelected', { count: selectedCount }) }}
      </button>
      <button
        type="button"
        class="shrink-0 rounded-lg border border-white/10 px-3 py-1.5 text-xs text-light/60 transition hover:text-light"
        @click="emit('cancel-merge')"
      >
        {{ t('admin.institutions.cancel') }}
      </button>
    </template>
  </div>
</template>
