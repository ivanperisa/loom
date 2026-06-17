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
</script>

<template>
  <Teleport to="body">
    <div class="toast-list" aria-live="polite">
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
          class="toast"
          :class="`toast--${toast.type}`"
        >
          <div class="toast__icon">{{ icons[toast.type] }}</div>

          <div class="toast__body">
            <p class="toast__title">{{ toast.title }}</p>
            <p v-if="toast.message" class="toast__message">{{ toast.message }}</p>
          </div>

          <button class="toast__close" aria-label="Close" @click="removeToast(toast.id)">
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<style scoped>
.toast-list {
  position: fixed;
  right: 20px;
  bottom: 20px;
  z-index: 50;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.toast {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  width: 320px;
  padding: 14px 16px;
  border-radius: 12px;
  border-width: 1px;
  border-style: solid;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.25);
}

.toast--success {
  background: color-mix(in srgb, #16a34a 18%, var(--color-dark-2));
  border-color: color-mix(in srgb, #16a34a 50%, transparent);
}
.toast--error {
  background: color-mix(in srgb, #dc2626 18%, var(--color-dark-2));
  border-color: color-mix(in srgb, #dc2626 50%, transparent);
}
.toast--warning {
  background: color-mix(in srgb, #d97706 18%, var(--color-dark-2));
  border-color: color-mix(in srgb, #d97706 50%, transparent);
}
.toast--info {
  background: color-mix(in srgb, var(--color-primary) 18%, var(--color-dark-2));
  border-color: color-mix(in srgb, var(--color-primary) 50%, transparent);
}

.toast__icon {
  margin-top: 1px;
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 700;
}

.toast--success .toast__icon { background: color-mix(in srgb, #16a34a 25%, transparent); color: #16a34a; }
.toast--error   .toast__icon { background: color-mix(in srgb, #dc2626 25%, transparent); color: #dc2626; }
.toast--warning .toast__icon { background: color-mix(in srgb, #d97706 25%, transparent); color: #d97706; }
.toast--info    .toast__icon { background: color-mix(in srgb, var(--color-primary) 25%, transparent); color: var(--color-primary); }

.toast__body {
  flex: 1;
  min-width: 0;
}

.toast__title {
  font-size: 13px;
  font-weight: 700;
  line-height: 1.3;
  color: var(--color-light);
  margin: 0;
}

.toast__message {
  font-size: 12px;
  line-height: 1.5;
  color: var(--color-light);
  opacity: 0.75;
  margin: 4px 0 0;
}

.toast__close {
  margin-top: 2px;
  flex-shrink: 0;
  color: var(--color-light);
  opacity: 0.45;
  background: none;
  border: none;
  cursor: pointer;
  padding: 0;
  transition: opacity 0.15s;
}
.toast__close:hover { opacity: 0.9; }
</style>
