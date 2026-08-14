<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

const props = withDefaults(
  defineProps<{
    page: number
    totalPages: number
    total?: number
    perPage?: number
  }>(),
  { perPage: 10 },
)

defineEmits<{ 'update:page': [value: number] }>()

const { t } = useI18n()

const rangeLabel = computed(() => {
  if (props.total === undefined) return null
  const from = (props.page - 1) * props.perPage + 1
  const to = Math.min(props.page * props.perPage, props.total)
  return `${from}–${to} / ${props.total}`
})
</script>

<template>
  <div v-if="totalPages > 1" class="mt-2 flex items-center justify-between gap-3 text-xs text-light/40">
    <span v-if="rangeLabel">{{ rangeLabel }}</span>
    <span v-else></span>
    <div class="flex items-center gap-2">
      <button
        type="button"
        class="rounded px-2 py-0.5 transition hover:text-light disabled:opacity-30"
        :disabled="page <= 1"
        @click="$emit('update:page', page - 1)"
      >
        {{ t('common.previous') }}
      </button>
      <span>{{ page }} / {{ totalPages }}</span>
      <button
        type="button"
        class="rounded px-2 py-0.5 transition hover:text-light disabled:opacity-30"
        :disabled="page >= totalPages"
        @click="$emit('update:page', page + 1)"
      >
        {{ t('common.next') }}
      </button>
    </div>
  </div>
</template>
