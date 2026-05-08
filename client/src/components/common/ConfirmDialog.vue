<script setup lang="ts">
import { useConfirm } from '@/composables/useConfirm'
import { useI18n } from 'vue-i18n'

const { state, respond } = useConfirm()
const { t } = useI18n()
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition-opacity duration-200"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-150"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="state"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
        @click.self="respond(false)"
      >
        <Transition
          enter-active-class="transition-all duration-200 ease-out"
          enter-from-class="opacity-0 scale-95"
          enter-to-class="opacity-100 scale-100"
          leave-active-class="transition-all duration-150 ease-in"
          leave-from-class="opacity-100 scale-100"
          leave-to-class="opacity-0 scale-95"
        >
          <div
            v-if="state"
            class="w-full max-w-sm rounded-2xl border border-primary/20 bg-dark-2 p-6 shadow-2xl shadow-black/60"
          >
            <h3 class="text-base font-semibold text-light">{{ state.title }}</h3>
            <p v-if="state.message" class="mt-2 text-sm text-light/60">{{ state.message }}</p>

            <div class="mt-6 flex justify-end gap-3">
              <button
                type="button"
                class="rounded-lg border border-white/10 px-4 py-2 text-sm font-medium text-light/70 transition hover:border-white/20 hover:text-light"
                @click="respond(false)"
              >
                {{ state.cancelLabel ?? t('common.cancel') }}
              </button>
              <button
                type="button"
                class="rounded-lg border border-red-400/50 px-4 py-2 text-sm font-semibold text-red-300 transition hover:bg-red-500/20"
                @click="respond(true)"
              >
                {{ state.confirmLabel ?? t('common.confirm') }}
              </button>
            </div>
          </div>
        </Transition>
      </div>
    </Transition>
  </Teleport>
</template>
