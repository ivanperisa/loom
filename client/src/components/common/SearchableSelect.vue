<script setup lang="ts" generic="T extends string | number | null">
import { ref, computed, watch, nextTick, onBeforeUnmount, useId } from 'vue'

export interface SelectOption {
  value: string | number | null
  label: string
  sublabel?: string
}

const props = withDefaults(
  defineProps<{
    modelValue: T
    options: SelectOption[]
    placeholder?: string
    searchable?: boolean
    searchPlaceholder?: string
    noResultsLabel?: string
    loading?: boolean
    disabled?: boolean
    /** Accessible name when no visible label points at the select. */
    ariaLabel?: string
  }>(),
  {
    placeholder: '—',
    searchable: true,
    searchPlaceholder: 'Search...',
    noResultsLabel: 'No results.',
    loading: false,
    disabled: false,
  },
)

const emit = defineEmits<{ 'update:modelValue': [value: T] }>()

const open = ref(false)
const search = ref('')
const root = ref<HTMLElement | null>(null)
const dropdownRef = ref<HTMLElement | null>(null)
const dropdownStyle = ref<Record<string, string>>({})
const trigger = ref<HTMLButtonElement | null>(null)
const searchInput = ref<HTMLInputElement | null>(null)
const listbox = ref<HTMLElement | null>(null)
/** The highlighted option (keyboard), by index in `filtered`. */
const activeIndex = ref(-1)
const listId = useId()
const optionId = (index: number) => `${listId}-option-${index}`

const selectedLabel = computed(
  () => props.options.find((o) => o.value === props.modelValue)?.label ?? null,
)

const filtered = computed(() => {
  if (!props.searchable || !search.value.trim()) return props.options
  const q = search.value.trim().toLowerCase()
  return props.options.filter(
    (o) =>
      o.label.toLowerCase().includes(q) ||
      (o.sublabel?.toLowerCase().includes(q) ?? false),
  )
})

const ESTIMATED_DROPDOWN_HEIGHT = 260

function updateDropdownPosition() {
  if (!root.value) return
  const rect = root.value.getBoundingClientRect()
  const spaceBelow = window.innerHeight - rect.bottom
  const openUpward = spaceBelow < ESTIMATED_DROPDOWN_HEIGHT && rect.top > spaceBelow
  dropdownStyle.value = {
    position: 'fixed',
    ...(openUpward
      ? { bottom: `${window.innerHeight - rect.top + 4}px` }
      : { top: `${rect.bottom + 4}px` }),
    left: `${rect.left}px`,
    width: `${rect.width}px`,
    zIndex: '9999',
  }
}

function onClickOutside(e: MouseEvent) {
  if (
    !root.value?.contains(e.target as Node) &&
    !dropdownRef.value?.contains(e.target as Node)
  ) {
    closeDropdown()
  }
}

function openDropdown() {
  updateDropdownPosition()
  open.value = true
  search.value = ''
  activeIndex.value = Math.max(0, filtered.value.findIndex((o) => o.value === props.modelValue))
  document.addEventListener('mousedown', onClickOutside)
  // Keyboard users continue in the search box, or on the list itself.
  nextTick(() => (searchInput.value ?? listbox.value)?.focus())
}

function closeDropdown(returnFocus = false) {
  open.value = false
  document.removeEventListener('mousedown', onClickOutside)
  if (returnFocus) trigger.value?.focus()
}

function moveActive(step: number) {
  const count = filtered.value.length
  if (count === 0) return
  activeIndex.value = (activeIndex.value + step + count) % count
  nextTick(() => document.getElementById(optionId(activeIndex.value))?.scrollIntoView({ block: 'nearest' }))
}

function onListKeydown(e: KeyboardEvent) {
  if (e.key === 'ArrowDown') moveActive(1)
  else if (e.key === 'ArrowUp') moveActive(-1)
  else if (e.key === 'Enter') {
    const option = filtered.value[activeIndex.value]
    if (option) select(option.value)
  } else if (e.key === 'Escape') closeDropdown(true)
  else if (e.key === 'Tab') closeDropdown()
  else return
  e.preventDefault()
}

function toggle() {
  if (props.disabled) return
  if (open.value) closeDropdown()
  else openDropdown()
}

function select(value: string | number | null) {
  emit('update:modelValue', value as T)
  closeDropdown(true)
  search.value = ''
}

watch(search, () => (activeIndex.value = filtered.value.length > 0 ? 0 : -1))

onBeforeUnmount(() => {
  document.removeEventListener('mousedown', onClickOutside)
})

watch(open, (val) => {
  if (val) search.value = ''
})
</script>

<template>
  <div ref="root" class="relative">
    <button
      ref="trigger"
      type="button"
      :disabled="disabled"
      aria-haspopup="listbox"
      :aria-expanded="open"
      :aria-controls="open ? listId : undefined"
      :aria-label="ariaLabel ? `${ariaLabel}: ${selectedLabel ?? placeholder}` : undefined"
      class="flex w-full items-center justify-between rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light transition focus:border-primary focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
      @click="toggle"
      @keydown.down.prevent="!open && openDropdown()"
    >
      <span v-if="loading" class="text-light/40">…</span>
      <span v-else :class="selectedLabel ? 'text-light' : 'text-light/40'">
        {{ selectedLabel ?? placeholder }}
      </span>
      <svg
        class="h-4 w-4 flex-shrink-0 text-light/40 transition-transform"
        :class="open ? 'rotate-180' : ''"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
      </svg>
    </button>

    <Teleport to="body">
      <div
        v-if="open"
        ref="dropdownRef"
        :style="dropdownStyle"
        class="rounded-lg border border-primary/20 bg-dark-2 shadow-xl"
      >
        <!-- Search -->
        <div v-if="searchable" class="p-2">
          <div class="relative">
            <svg
              class="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-faint"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            <input
              ref="searchInput"
              v-model="search"
              type="text"
              role="combobox"
              aria-autocomplete="list"
              :aria-expanded="true"
              :aria-controls="listId"
              :aria-activedescendant="activeIndex >= 0 ? optionId(activeIndex) : undefined"
              :aria-label="searchPlaceholder"
              :placeholder="searchPlaceholder"
              class="w-full rounded-md border border-primary/10 bg-dark py-1.5 pl-7 pr-3 text-xs text-light placeholder-faint focus:border-primary focus:outline-none"
              @click.stop
              @keydown="onListKeydown"
            />
          </div>
        </div>

        <!-- Options -->
        <div
          :id="listId"
          ref="listbox"
          role="listbox"
          :tabindex="searchable ? -1 : 0"
          :aria-activedescendant="!searchable && activeIndex >= 0 ? optionId(activeIndex) : undefined"
          :aria-label="ariaLabel ?? placeholder"
          class="max-h-52 overflow-y-auto pb-1 focus:outline-none"
          @keydown="onListKeydown"
        >
          <p v-if="filtered.length === 0" class="px-3 py-2 text-xs text-faint">
            {{ noResultsLabel }}
          </p>
          <div
            v-for="(opt, index) in filtered"
            :id="optionId(index)"
            :key="String(opt.value)"
            role="option"
            :aria-selected="modelValue === opt.value"
            class="w-full cursor-pointer px-3 py-2 text-left text-sm transition hover:bg-primary/10"
            :class="[modelValue === opt.value ? 'font-medium text-primary-text' : 'text-light', index === activeIndex ? 'bg-primary/10' : '']"
            @click="select(opt.value)"
            @mousemove="activeIndex = index"
          >
            <span>{{ opt.label }}</span>
            <span v-if="opt.sublabel" class="ml-1 text-xs text-faint">{{ opt.sublabel }}</span>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
