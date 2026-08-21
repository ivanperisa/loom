<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseModal from '@/components/common/BaseModal.vue'

const props = defineProps<{
  title: string
  courseCode: string
  courseName: string
  max: number
  totalEcts?: number
  modelValue: number
}>()
const emit = defineEmits<{
  'update:modelValue': [value: number]
  confirm: []
  cancel: []
}>()

const { t } = useI18n()

const isOverMax = computed(() => props.modelValue > props.max)

function tryConfirm() {
  if (isOverMax.value) return
  emit('confirm')
}
</script>

<template>
  <BaseModal labelled-by="ects-amount-dialog-title" @close="emit('cancel')">
    <div style="padding: 24px">
      <h3 id="ects-amount-dialog-title" style="color: var(--color-light); font-size: 14px; font-weight: 600; margin-bottom: 16px">
        {{ title }}
      </h3>
      <div style="color: var(--color-primary-light); font-size: 12px; margin-bottom: 4px">
        {{ courseCode }} — {{ courseName }}
      </div>
      <div style="color: var(--color-light); opacity: 0.6; font-size: 11px; margin-bottom: 16px">
        {{ t('partnerCourses.availableEcts') }}: {{ max }}<template v-if="totalEcts !== undefined"> / {{ totalEcts }}</template> ECTS
      </div>
      <label style="display: block; color: var(--color-light); font-size: 12px; margin-bottom: 6px">
        {{ t('partnerCourses.awardedEcts') }}
      </label>
      <input
        :value="modelValue"
        type="number"
        :min="0.5"
        :max="max"
        step="0.5"
        style="width: 100%; background: var(--color-dark); border: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent); color: var(--color-light); padding: 8px; border-radius: 4px; font-size: 13px;"
        :style="isOverMax ? { borderColor: '#ef4444' } : {}"
        @input="emit('update:modelValue', Number(($event.target as HTMLInputElement).value))"
        @keydown.enter.prevent="tryConfirm"
      />
      <div style="min-height: 16px; margin-top: 4px; margin-bottom: 12px; font-size: 11px; color: #ef4444;">
        <template v-if="isOverMax">{{ t('partnerCourses.ectsExceedsMax', { max }) }}</template>
      </div>
      <div style="display: flex; gap: 8px; justify-content: flex-end">
        <button
          type="button"
          style="padding: 8px 16px; border: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent); background: transparent; color: var(--color-primary-light); border-radius: 4px; cursor: pointer; font-size: 13px;"
          @click="emit('cancel')"
        >
          {{ t('common.cancel') }}
        </button>
        <button
          type="button"
          :disabled="isOverMax"
          :style="isOverMax ? { opacity: 0.5, cursor: 'not-allowed' } : {}"
          style="padding: 8px 16px; background: var(--color-primary); border: none; color: white; border-radius: 4px; font-size: 13px; font-weight: 600;"
          @click="tryConfirm"
        >
          {{ t('common.confirm') }}
        </button>
      </div>
    </div>
  </BaseModal>
</template>
