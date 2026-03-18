<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import { useExchangeStore } from '@/stores/exchange.store'
import type { ForeignCourseResponse } from '@/types/institution.types'

const props = withDefaults(defineProps<{
  foreignProgramId: string
  exchangeId: string
  variant?: 'available' | 'mapped' | 'all'
}>(), { variant: 'all' })

const { t } = useI18n()
const exchangeStore = useExchangeStore()

const courses = ref<ForeignCourseResponse[]>([])
const loading = ref(true)

onMounted(async () => {
  try {
    const res = await institutionService.getForeignCourses(props.foreignProgramId)
    courses.value = res.data
  } catch {
    // keep empty
  } finally {
    loading.value = false
  }
})

// Compute mapped ECTS per foreign course from learning agreement
const mappedEctsMap = computed(() => {
  const map = new Map<string, number>()
  const la = exchangeStore.learningAgreement
  if (!la) return map
  for (const state of la.slotStates) {
    for (const m of state.mappings) {
      map.set(m.foreignCourseId, (map.get(m.foreignCourseId) ?? 0) + m.awardedEcts)
    }
  }
  return map
})

function mappedEcts(courseId: string): number {
  return mappedEctsMap.value.get(courseId) ?? 0
}

const mappedCourses = computed(() =>
  courses.value.filter(c => mappedEcts(c.id) > 0)
)

const availableCourses = computed(() =>
  courses.value.filter(c => mappedEcts(c.id) < c.ects)
)

function onDragStart(course: ForeignCourseResponse) {
  exchangeStore.startDrag(course)
}
</script>

<template>
  <div>
    <!-- Loading -->
    <div v-if="loading" class="space-y-2">
      <div v-for="i in 4" :key="i" class="h-14 animate-pulse rounded-lg bg-[#1E4A6E]"></div>
    </div>

    <template v-else>
      <!-- Available variant: courses not fully mapped (draggable) -->
      <template v-if="variant === 'available' || variant === 'all'">
        <p v-if="variant === 'all'" class="mb-3 text-xs text-[#5A8AAD]">{{ t('foreignCourses.dragHint') }}</p>
        <div class="max-h-[400px] space-y-1.5 overflow-y-auto pr-1">
          <div
            v-for="course in availableCourses"
            :key="course.id"
            draggable="true"
            class="flex items-center gap-3 rounded-lg border border-[#1E4A6E] bg-[#0A2235] px-4 py-3 cursor-grab transition hover:border-[#218CD9] active:cursor-grabbing"
            @dragstart="onDragStart(course)"
            @dragend="exchangeStore.endDrag()"
          >
            <svg class="shrink-0 text-[#5A8AAD]" width="12" height="18" viewBox="0 0 12 18" fill="currentColor">
              <circle cx="3" cy="3" r="1.5"/><circle cx="9" cy="3" r="1.5"/>
              <circle cx="3" cy="9" r="1.5"/><circle cx="9" cy="9" r="1.5"/>
              <circle cx="3" cy="15" r="1.5"/><circle cx="9" cy="15" r="1.5"/>
            </svg>
            <div class="min-w-0 flex-1">
              <div class="text-xs font-bold text-[#CAE4F7]">{{ course.code }}</div>
              <div class="text-sm font-medium text-[#CAE4F7] truncate">{{ course.nameEn }}</div>
              <div v-if="course.nameHr" class="text-xs text-[#5A8AAD] truncate">{{ course.nameHr }}</div>
            </div>
            <div class="shrink-0 text-right">
              <span
                v-if="mappedEcts(course.id) > 0"
                class="rounded px-2 py-0.5 text-xs font-semibold bg-amber-500/20 text-amber-300"
              >
                {{ mappedEcts(course.id) }}/{{ course.ects }} ECTS
              </span>
              <span v-else class="rounded bg-[#1E4A6E] px-2 py-0.5 text-xs font-semibold text-[#8AC4ED]">
                {{ course.ects }} ECTS
              </span>
            </div>
          </div>
          <p v-if="availableCourses.length === 0" class="py-4 text-center text-xs text-[#5A8AAD]">
            {{ t('foreignCourses.allMapped') }}
          </p>
        </div>
      </template>

      <!-- Mapped variant: courses with at least one mapping -->
      <template v-if="variant === 'mapped' || variant === 'all'">
        <div class="max-h-[400px] space-y-1.5 overflow-y-auto pr-1" :class="variant === 'all' ? 'mt-4' : ''">
          <div
            v-for="course in mappedCourses"
            :key="course.id"
            draggable="true"
            class="flex items-center gap-3 rounded-lg border border-green-500/30 bg-green-900/10 px-4 py-3 cursor-grab transition hover:border-[#218CD9] active:cursor-grabbing"
            @dragstart="onDragStart(course)"
            @dragend="exchangeStore.endDrag()"
          >
            <svg class="shrink-0 text-[#5A8AAD]" width="12" height="18" viewBox="0 0 12 18" fill="currentColor">
              <circle cx="3" cy="3" r="1.5"/><circle cx="9" cy="3" r="1.5"/>
              <circle cx="3" cy="9" r="1.5"/><circle cx="9" cy="9" r="1.5"/>
              <circle cx="3" cy="15" r="1.5"/><circle cx="9" cy="15" r="1.5"/>
            </svg>
            <div class="min-w-0 flex-1">
              <div class="text-xs font-bold text-[#CAE4F7]">{{ course.code }}</div>
              <div class="text-sm font-medium text-[#CAE4F7] truncate">{{ course.nameEn }}</div>
              <div v-if="course.nameHr" class="text-xs text-[#5A8AAD] truncate">{{ course.nameHr }}</div>
            </div>
            <div class="shrink-0 text-right">
              <span
                class="rounded px-2 py-0.5 text-xs font-semibold"
                :class="mappedEcts(course.id) >= course.ects ? 'bg-green-500/20 text-green-300' : 'bg-amber-500/20 text-amber-300'"
              >
                {{ mappedEcts(course.id) }}/{{ course.ects }} ECTS
              </span>
            </div>
          </div>
          <p v-if="mappedCourses.length === 0" class="py-4 text-center text-xs text-[#5A8AAD]">
            {{ t('foreignCourses.noMapped') }}
          </p>
        </div>
      </template>
    </template>
  </div>
</template>
