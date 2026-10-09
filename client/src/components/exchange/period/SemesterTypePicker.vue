<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { exchangeSemester } from '@/utils/exchangeSemester'
import type { ExchangeSemester } from '@/types/exchange.types'

/** Winter, summer or both. A type that would drop study semesters already used by the LA is disabled. */
const props = withDefaults(defineProps<{ modelValue: ExchangeSemester; canSelect?: (type: ExchangeSemester) => boolean }>(), {
  canSelect: () => true,
})
const emit = defineEmits<{ 'update:modelValue': [type: ExchangeSemester] }>()

const { t } = useI18n()
const types = [exchangeSemester.Winter, exchangeSemester.Summer, exchangeSemester.Both]
</script>

<template>
  <div>
    <p id="semester-type-label" class="mb-2 block text-sm font-semibold text-primary-text">{{ t('exchange.semester') }}</p>
    <div class="grid grid-cols-3 gap-2" role="radiogroup" aria-labelledby="semester-type-label">
      <button
        v-for="type in types"
        :key="type"
        type="button"
        role="radio"
        :aria-checked="modelValue === type"
        :disabled="!props.canSelect(type)"
        class="rounded-xl border py-2.5 text-xs font-medium transition disabled:cursor-not-allowed disabled:opacity-40"
        :class="
          modelValue === type
            ? 'border-primary bg-primary/10 text-primary-on-tint'
            : 'border-hairline bg-dark text-light/60 hover:border-primary/50 hover:text-light'
        "
        @click="emit('update:modelValue', type)"
      >
        {{ t(`exchangeSemester.${type}`) }}
      </button>
    </div>
  </div>
</template>
