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
    <button
      v-if="hasDeleted"
      type="button"
      role="switch"
      :aria-checked="showDeleted"
      class="flex shrink-0 items-center gap-2 text-xs text-light/60 transition hover:text-light"
      @click="emit('update:showDeleted', !showDeleted)"
    >
      <span
        class="relative inline-flex h-5 w-9 shrink-0 items-center rounded-full border transition-colors"
        :class="showDeleted ? 'border-primary bg-primary' : 'border-hairline bg-fill-soft'"
      >
        <span
          class="inline-block h-3.5 w-3.5 transform rounded-full bg-white shadow transition-transform"
          :class="showDeleted ? 'translate-x-[18px]' : 'translate-x-0.5'"
        ></span>
      </span>
      {{ t('admin.institutions.showDeleted') }}
    </button>
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
        class="shrink-0 rounded-lg border border-hairline px-3 py-1.5 text-xs text-light/60 transition hover:text-light"
        @click="emit('cancel-merge')"
      >
        {{ t('admin.institutions.cancel') }}
      </button>
    </template>
  </div>
</template>
