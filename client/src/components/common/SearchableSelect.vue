<script setup lang="ts" generic="T extends string | null">
import { ref, computed, watch } from 'vue'

export interface SelectOption {
  value: string | null
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
  }>(),
  {
    placeholder: '—',
    searchable: true,
    searchPlaceholder: 'Search...',
    noResultsLabel: 'No results.',
    loading: false,
  },
)

const emit = defineEmits<{ 'update:modelValue': [value: T] }>()

const open = ref(false)
const search = ref('')
const root = ref<HTMLElement | null>(null)

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

function toggle() {
  open.value = !open.value
}

function select(value: string | null) {
  emit('update:modelValue', value as T)
  open.value = false
  search.value = ''
}

function onFocusOut(e: FocusEvent) {
  if (!root.value?.contains(e.relatedTarget as Node)) {
    open.value = false
  }
}

watch(open, (val) => {
  if (val) search.value = ''
})
</script>

<template>
  <div ref="root" class="relative" @focusout.capture="onFocusOut">
    <!-- Trigger -->
    <button
      type="button"
      class="flex w-full items-center justify-between rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light transition focus:border-primary focus:outline-none"
      @click="toggle"
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

    <!-- Dropdown -->
    <div
      v-if="open"
      class="absolute left-0 right-0 top-full z-20 mt-1 rounded-lg border border-primary/20 bg-dark-2 shadow-xl"
    >
      <!-- Search -->
      <div v-if="searchable" class="p-2">
        <div class="relative">
          <svg
            class="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-slate-500"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            v-model="search"
            type="text"
            :placeholder="searchPlaceholder"
            class="w-full rounded-md border border-primary/10 bg-dark py-1.5 pl-7 pr-3 text-xs text-light placeholder-slate-500 focus:border-primary focus:outline-none"
            @click.stop
          />
        </div>
      </div>

      <!-- Options -->
      <div class="max-h-52 overflow-y-auto pb-1">
        <p v-if="filtered.length === 0" class="px-3 py-2 text-xs text-slate-500">
          {{ noResultsLabel }}
        </p>
        <button
          v-for="opt in filtered"
          :key="String(opt.value)"
          type="button"
          class="w-full px-3 py-2 text-left text-sm transition hover:bg-primary/10"
          :class="modelValue === opt.value ? 'font-medium text-primary-light' : 'text-light'"
          @click="select(opt.value)"
        >
          <span>{{ opt.label }}</span>
          <span v-if="opt.sublabel" class="ml-1 text-xs text-slate-500">{{ opt.sublabel }}</span>
        </button>
      </div>
    </div>
  </div>
</template>
