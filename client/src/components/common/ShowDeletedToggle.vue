<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import SearchableSelect from '@/components/common/SearchableSelect.vue'

const props = defineProps<{ modelValue: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean] }>()

const { t } = useI18n()

const options = computed(() => [
  { value: 'active', label: t('admin.institutions.activeOnly') },
  { value: 'all', label: t('admin.institutions.withDeleted') },
])
</script>

<template>
  <SearchableSelect
    :model-value="props.modelValue ? 'all' : 'active'"
    :options="options"
    :searchable="false"
    class="min-w-[150px] max-w-[200px] flex-none"
    @update:model-value="emit('update:modelValue', $event === 'all')"
  />
</template>
