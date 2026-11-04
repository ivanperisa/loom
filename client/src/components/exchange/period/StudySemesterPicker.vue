<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { exchangeSemester } from '@/utils/exchangeSemester'
import type { ExchangeSemester } from '@/types/exchange.types'

/** Which study semesters the exchange covers: one semester (winter/summer) or a pair (both). */
const props = defineProps<{ semesterType: ExchangeSemester }>()
const model = defineModel<number[]>({ required: true })

const { t } = useI18n()

const single = computed(() => (props.semesterType === exchangeSemester.Winter ? [1, 3] : [2, 4]))
const pairs = [[1, 2], [3, 4]] as const

const isSelected = (semesters: readonly number[]) =>
  model.value.length === semesters.length && semesters.every((s) => model.value.includes(s))

/** Clicking the selected option clears it; anything else replaces the selection. */
function choose(semesters: readonly number[]) {
  model.value = isSelected(semesters) ? [] : [...semesters]
}

const optionClass = (selected: boolean) =>
  selected
    ? 'border-primary bg-primary/10 text-primary-on-tint'
    : 'border-hairline bg-dark text-light/60 hover:border-primary/50 hover:text-light'
</script>

<template>
  <div>
    <p id="study-semester-label" class="mb-2 block text-sm font-semibold text-primary-text">{{ t('exchange.studySemester') }}</p>

    <div v-if="semesterType !== exchangeSemester.Both" class="flex gap-2" role="group" aria-labelledby="study-semester-label">
      <button
        v-for="s in single"
        :key="s"
        type="button"
        class="h-10 w-10 rounded-xl border text-sm font-semibold transition"
        :class="optionClass(isSelected([s]))"
        :aria-pressed="isSelected([s])"
        @click="choose([s])"
      >
        {{ s }}
      </button>
    </div>

    <div v-else class="flex gap-3" role="group" aria-labelledby="study-semester-label">
      <button
        v-for="pair in pairs"
        :key="pair.join()"
        type="button"
        class="rounded-xl border px-5 py-2.5 text-sm font-semibold transition"
        :class="optionClass(isSelected(pair))"
        :aria-pressed="isSelected(pair)"
        @click="choose(pair)"
      >
        {{ pair.join(' + ') }}
      </button>
    </div>
  </div>
</template>
