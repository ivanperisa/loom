<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useConfirm } from '@/composables/useConfirm'

const { state, respond } = useConfirm()
const { t } = useI18n()
</script>

<template>
  <Teleport to="body">
    <div
      v-if="state"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm"
      @mousedown.self="respond(false)"
    >
      <div class="w-full max-w-sm rounded-xl border border-primary/30 bg-dark p-6 shadow-xl">
        <p v-if="state.title" class="font-semibold text-light">{{ state.title }}</p>
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
            class="rounded-lg bg-red-600 px-4 py-1.5 text-sm font-medium text-white transition hover:bg-red-500"
            @click="respond(true)"
          >
            {{ state.confirmLabel ?? t('common.confirm') }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
