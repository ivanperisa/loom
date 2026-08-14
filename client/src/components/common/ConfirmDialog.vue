<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useConfirm } from '@/composables/useConfirm'
import BaseModal from '@/components/common/BaseModal.vue'

const { state, respond } = useConfirm()
const { t } = useI18n()
</script>

<template>
  <BaseModal
    v-if="state"
    max-width="max-w-sm"
    z-class="z-[100]"
    labelled-by="confirm-dialog-title"
    @close="respond(false)"
  >
    <div class="p-6">
      <p v-if="state.title" id="confirm-dialog-title" class="font-semibold text-light">
        {{ state.title }}
      </p>
      <p v-if="state.message" class="mt-1 text-sm text-light/70">{{ state.message }}</p>
      <div class="mt-5 flex justify-end gap-2">
        <button
          type="button"
          class="rounded-lg border border-primary/20 px-4 py-1.5 text-sm text-light/70 transition hover:bg-white/5 hover:text-light"
          @click="respond(false)"
        >
          {{ state.cancelLabel ?? t('common.cancel') }}
        </button>
        <button
          type="button"
          class="rounded-lg px-4 py-1.5 text-sm font-medium text-white transition"
          :class="
            state.variant === 'neutral'
              ? 'bg-primary hover:bg-primary-light hover:text-dark'
              : 'bg-red-600 hover:bg-red-500'
          "
          @click="respond(true)"
        >
          {{ state.confirmLabel ?? t('common.confirm') }}
        </button>
      </div>
    </div>
  </BaseModal>
</template>
