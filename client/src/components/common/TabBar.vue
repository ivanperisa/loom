<script setup lang="ts" generic="K extends string">
import { ref } from 'vue'

/**
 * Accessible tabs (WAI-ARIA tablist): arrow keys, Home and End move between tabs, only the active tab is in the
 * tab order. Pair each panel with `role="tabpanel"`, `:id="tabPanelId(idPrefix, key)"` and
 * `:aria-labelledby="tabId(idPrefix, key)"`.
 */
const props = withDefaults(
  defineProps<{
    tabs: { key: K; label: string }[]
    /** Unique per page: tab and panel ids are built from it. */
    idPrefix: string
    variant?: 'underline' | 'pill'
    label?: string
  }>(),
  { variant: 'underline', label: undefined },
)
const active = defineModel<K>({ required: true })

const buttons = ref<HTMLButtonElement[]>([])

function move(from: number, step: number | 'first' | 'last') {
  const count = props.tabs.length
  const to = step === 'first' ? 0 : step === 'last' ? count - 1 : (from + step + count) % count
  const tab = props.tabs[to]
  if (!tab) return
  active.value = tab.key
  buttons.value[to]?.focus()
}
</script>

<script lang="ts">
export const tabId = (prefix: string, key: string) => `${prefix}-tab-${key}`
export const tabPanelId = (prefix: string, key: string) => `${prefix}-panel-${key}`
</script>

<template>
  <div
    role="tablist"
    :aria-label="label"
    :class="variant === 'pill' ? 'flex gap-1 rounded-xl border border-primary/20 bg-dark-2 p-1' : 'flex'"
  >
    <button
      v-for="(tab, index) in tabs"
      :id="tabId(idPrefix, tab.key)"
      :key="tab.key"
      ref="buttons"
      type="button"
      role="tab"
      :aria-selected="active === tab.key"
      :aria-controls="tabPanelId(idPrefix, tab.key)"
      :tabindex="active === tab.key ? 0 : -1"
      :class="
        variant === 'pill'
          ? ['flex-1 rounded-lg px-4 py-2 text-sm font-medium transition', active === tab.key ? 'bg-primary-strong text-white' : 'text-light/60 hover:text-light']
          : ['px-4 py-2.5 text-sm font-semibold transition', active === tab.key ? 'border-b-2 border-primary text-primary-active-text' : 'text-light/60 hover:text-primary-text']
      "
      @click="active = tab.key"
      @keydown.right.prevent="move(index, 1)"
      @keydown.left.prevent="move(index, -1)"
      @keydown.home.prevent="move(index, 'first')"
      @keydown.end.prevent="move(index, 'last')"
    >
      {{ tab.label }}
    </button>
  </div>
</template>
