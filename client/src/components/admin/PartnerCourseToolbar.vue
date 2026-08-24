<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import SearchInput from '@/components/common/SearchInput.vue'
import SearchableSelect from '@/components/common/SearchableSelect.vue'
import ShowDeletedToggle from '@/components/common/ShowDeletedToggle.vue'

defineProps<{
  search: string
  semester: string | null
  level: string | null
  showDeleted: boolean
  hasDeleted: boolean
  mergeSelecting: boolean
  canMerge: boolean
  selectedCount: number
}>()
const emit = defineEmits<{
  'update:search': [value: string]
  'update:semester': [value: string | null]
  'update:level': [value: string | null]
  'update:showDeleted': [value: boolean]
  'start-merge': []
  'confirm-merge': []
  'cancel-merge': []
}>()

const { t } = useI18n()

const semesterOptions = computed(() => [
  { value: null, label: t('admin.institutions.allSemesters') },
  { value: 'Winter', label: t('exchangeSemester.Winter') },
  { value: 'Summer', label: t('exchangeSemester.Summer') },
  { value: 'Both', label: t('exchangeSemester.Both') },
])

const levelOptions = computed(() => [
  { value: null, label: t('admin.institutions.allLevels') },
  { value: 'Undergraduate', label: t('admin.institutions.levelUndergraduate') },
  { value: 'Graduate', label: t('admin.institutions.levelGraduate') },
  { value: 'Postgraduate', label: t('admin.institutions.levelPostgraduate') },
])
</script>

<template>
  <div class="mb-2 flex flex-wrap items-center justify-between gap-3">
    <div class="flex flex-1 flex-wrap items-center gap-2">
      <SearchInput
        :model-value="search"
        :placeholder="t('admin.institutions.searchCourses')"
        class="min-w-[160px] flex-1"
        @update:model-value="emit('update:search', $event)"
      />
      <SearchableSelect
        :model-value="semester"
        :options="semesterOptions"
        :placeholder="t('admin.institutions.allSemesters')"
        :searchable="false"
        class="min-w-[130px] max-w-[180px] flex-none"
        @update:model-value="emit('update:semester', $event)"
      />
      <SearchableSelect
        :model-value="level"
        :options="levelOptions"
        :placeholder="t('admin.institutions.allLevels')"
        :searchable="false"
        class="min-w-[140px] max-w-[190px] flex-none"
        @update:model-value="emit('update:level', $event)"
      />
    </div>

    <div class="flex shrink-0 items-center gap-3">
      <ShowDeletedToggle v-if="hasDeleted" :model-value="showDeleted" @update:model-value="emit('update:showDeleted', $event)" />
      <button
        v-if="!mergeSelecting && canMerge"
        type="button"
        class="flex shrink-0 items-center gap-1.5 rounded-lg border border-primary/30 px-3 py-1.5 text-xs font-medium text-primary-text transition hover:bg-primary/10"
        @click="emit('start-merge')"
      >
        <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <circle cx="9" cy="12" r="6" stroke-width="2" />
          <circle cx="15" cy="12" r="6" stroke-width="2" />
        </svg>
        {{ t('admin.institutions.mergeCourses') }}
      </button>
      <template v-else-if="mergeSelecting">
        <button
          type="button"
          class="shrink-0 rounded-lg bg-primary-strong px-3 py-1.5 text-xs font-medium text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-40"
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
  </div>
</template>
