<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { describeApiError } from '@/utils/apiError'
import ActionButton from '@/components/common/ActionButton.vue'

/** A failed load, shown where the data would be. Actions use toasts instead. */
const props = defineProps<{ error: unknown; retryable?: boolean }>()
defineEmits<{ retry: [] }>()

const { t } = useI18n()
const described = computed(() => describeApiError(props.error))
</script>

<template>
  <div role="alert" class="rounded-xl border border-danger/30 bg-danger-fill p-6 text-center">
    <p class="font-semibold text-danger-text">{{ described.title }}</p>
    <p class="mt-1 text-sm text-muted">{{ described.message }}</p>
    <ActionButton v-if="retryable !== false" class="mx-auto mt-4" @click="$emit('retry')">
      {{ t('apiErrors.retry') }}
    </ActionButton>
  </div>
</template>
