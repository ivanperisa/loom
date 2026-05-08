<script setup lang="ts">
import { useNotification } from '@/composables/useNotification'
import type { ToastType } from '@/composables/useNotification'

const { toasts, removeToast } = useNotification()

const icons: Record<ToastType, string> = {
  success: '✓',
  error: '✕',
  warning: '⚠',
  info: 'ℹ',
}

const styles: Record<ToastType, { border: string; bg: string; iconBg: string; iconText: string; text: string }> = {
  success: {
    border: 'border-green-400/30',
    bg: 'bg-green-900/20',
    iconBg: 'bg-green-500/20',
    iconText: 'text-green-300',
    text: 'text-green-100',
  },
  error: {
    border: 'border-red-400/30',
    bg: 'bg-red-900/20',
    iconBg: 'bg-red-500/20',
    iconText: 'text-red-300',
    text: 'text-red-100',
  },
  warning: {
    border: 'border-yellow-400/30',
    bg: 'bg-yellow-900/20',
    iconBg: 'bg-yellow-500/20',
    iconText: 'text-yellow-300',
    text: 'text-yellow-100',
  },
  info: {
    border: 'border-primary/30',
    bg: 'bg-primary/10',
    iconBg: 'bg-primary/20',
    iconText: 'text-primary-light',
    text: 'text-light',
  },
}
</script>

<template>
  <Teleport to="body">
    <div class="fixed right-5 bottom-5 z-50 flex flex-col gap-3" aria-live="polite">
      <TransitionGroup
        enter-active-class="transition-all duration-300 ease-out"
        enter-from-class="opacity-0 translate-x-8 scale-95"
        enter-to-class="opacity-100 translate-x-0 scale-100"
        leave-active-class="transition-all duration-200 ease-in"
        leave-from-class="opacity-100 translate-x-0 scale-100"
        leave-to-class="opacity-0 translate-x-8 scale-95"
      >
        <div
          v-for="toast in toasts"
          :key="toast.id"
          class="flex w-80 items-start gap-3 rounded-xl border p-4 shadow-lg shadow-black/40 backdrop-blur-sm"
          :class="[styles[toast.type].border, styles[toast.type].bg]"
        >
          <div
            class="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-xs font-bold"
            :class="[styles[toast.type].iconBg, styles[toast.type].iconText]"
          >
            {{ icons[toast.type] }}
          </div>

          <div class="min-w-0 flex-1">
            <p class="text-sm font-semibold leading-tight" :class="styles[toast.type].text">
              {{ toast.title }}
            </p>
            <p
              v-if="toast.message"
              class="mt-1 text-xs leading-relaxed opacity-80"
              :class="styles[toast.type].text"
            >
              {{ toast.message }}
            </p>
          </div>

          <button
            class="mt-0.5 shrink-0 cursor-pointer opacity-50 transition-opacity hover:opacity-100"
            :class="styles[toast.type].text"
            @click="removeToast(toast.id)"
            aria-label="Close notification"
          >
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>
