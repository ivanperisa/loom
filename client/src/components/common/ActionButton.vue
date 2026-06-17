<script setup lang="ts">
withDefaults(defineProps<{
  variant?: 'outline' | 'solid'
  size?: 'sm' | 'md'
}>(), {
  variant: 'outline',
  size: 'sm',
})
</script>

<template>
  <button type="button" :class="['action-btn', `action-btn--${variant}`, `action-btn--${size}`]">
    <slot />
  </button>
</template>

<style scoped>
.action-btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 600;
  transition: background 0.15s;
  white-space: nowrap;
  flex-shrink: 0;
}

.action-btn--sm {
  padding: 4px 10px;
  font-size: 11px;
}

.action-btn--md {
  padding: 7px 16px;
  font-size: 13px;
}

.action-btn--outline {
  border: 1px solid color-mix(in srgb, var(--color-primary) 40%, transparent);
  background: color-mix(in srgb, var(--color-primary) 12%, var(--color-dark-2));
  color: var(--color-light);
}
.action-btn--outline:hover {
  background: color-mix(in srgb, var(--color-primary) 22%, var(--color-dark-2));
}

.action-btn--solid {
  border: none;
  background: color-mix(in srgb, var(--color-primary) 85%, var(--color-dark-2));
  color: var(--color-light);
}
.action-btn--solid:hover {
  background: var(--color-primary);
}
</style>
