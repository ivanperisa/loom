<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import { useExchangeStore } from '@/stores/exchange.store'
import type { PartnerCourseResponse } from '@/types/institution.types'
import SearchInput from '@/components/common/SearchInput.vue'
import PartnerCourseFormModal from '@/components/common/PartnerCourseFormModal.vue'

const props = withDefaults(
  defineProps<{
    partnerInstitutionId: string
    exchangeId: string
    variant?: 'available' | 'mapped' | 'all'
  }>(),
  { variant: 'all' },
)

const { t } = useI18n()
const exchangeStore = useExchangeStore()

const courses = ref<PartnerCourseResponse[]>([])
const loading = ref(true)
const searchQuery = ref('')

const showAddForm = ref(false)
const addingCourse = ref(false)
const addError = ref<string | null>(null)

function openAddForm() {
  addError.value = null
  showAddForm.value = true
}

async function submitAddCourse(payload: {
  code: string; name: string; nameHr?: string; ects: number; semester: string; level: string
  lecturesH?: number; auditoryH?: number; labH?: number
}) {
  addingCourse.value = true
  addError.value = null
  try {
    const res = await institutionService.createPartnerCourseByInstitution(props.partnerInstitutionId, payload)
    courses.value.push(res.data)
    showAddForm.value = false
  } catch {
    addError.value = t('partnerCourses.saveError')
  } finally {
    addingCourse.value = false
  }
}

onMounted(async () => {
  try {
    const res = await institutionService.getPartnerCoursesByInstitution(props.partnerInstitutionId)
    courses.value = res.data
  } catch {
    // keep empty
  } finally {
    loading.value = false
  }
})

const mappedEctsMap = computed(() => {
  const map = new Map<string, number>()
  for (const state of exchangeStore.localSlotStates) {
    for (const m of state.mappings) {
      map.set(m.partnerCourseId, (map.get(m.partnerCourseId) ?? 0) + m.awardedEcts)
    }
  }
  return map
})

function mappedEcts(courseId: string): number {
  return mappedEctsMap.value.get(courseId) ?? 0
}

const mappedCourses = computed(() => {
  const withEcts = courses.value.filter((c) => mappedEcts(c.id) > 0)
  const stagedOnly = courses.value.filter(
    (c) => exchangeStore.stagedPartnerCourseIds.has(c.id) && mappedEcts(c.id) === 0,
  )
  return [...withEcts, ...stagedOnly]
})

const availableCourses = computed(() =>
  courses.value.filter((c) => mappedEcts(c.id) === 0 && !exchangeStore.stagedPartnerCourseIds.has(c.id))
)

const searchResults = computed(() => {
  const q = searchQuery.value.trim().toLowerCase()
  if (!q) return availableCourses.value
  return availableCourses.value.filter(
    (c) =>
      c.code.toLowerCase().includes(q) ||
      c.name.toLowerCase().includes(q) ||
      (c.nameHr?.toLowerCase().includes(q) ?? false),
  )
})

function onDragStart(course: PartnerCourseResponse) {
  exchangeStore.startDrag(course)
}

function levelLabel(level: string) {
  const map: Record<string, string> = {
    Undergraduate: t('admin.institutions.levelUndergraduate'),
    Graduate: t('admin.institutions.levelGraduate'),
    Postgraduate: t('admin.institutions.levelPostgraduate'),
  }
  return map[level] ?? level
}

function semesterLabel(semester: string) {
  return t(`exchangeSemester.${semester}`)
}
</script>

<template>
  <div>
    <div v-if="loading" class="space-y-2">
      <div v-for="i in 4" :key="i" class="h-14 animate-pulse rounded-lg bg-primary/20"></div>
    </div>

    <template v-else>
      <template v-if="variant === 'available' || variant === 'all'">
        <div class="mb-3 flex items-center gap-2">
          <SearchInput
            v-model="searchQuery"
            :placeholder="t('partnerCourses.searchPlaceholder')"
            class="flex-1"
          />
          <button
            v-if="!showAddForm"
            type="button"
            :title="t('partnerCourses.addCourse')"
            class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg border border-primary/20 text-light/60 transition hover:border-primary hover:text-primary-light"
            @click="openAddForm"
          >
            <svg width="14" height="14" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
              <line x1="6" y1="1" x2="6" y2="11" /><line x1="1" y1="6" x2="11" y2="6" />
            </svg>
          </button>
        </div>

        <PartnerCourseFormModal
          v-if="showAddForm"
          mode="create"
          :saving="addingCourse"
          :error="addError"
          @submit="submitAddCourse"
          @close="showAddForm = false"
        />

        <div class="max-h-[400px] space-y-1.5 overflow-y-auto pr-1">
          <p v-if="searchResults.length === 0" class="py-4 text-center text-xs text-light/60">
            {{ t('partnerCourses.noResults') }}
          </p>
          <div
            v-for="course in searchResults"
            :key="course.id"
            draggable="true"
            class="flex items-center gap-3 rounded-lg border border-primary/20 bg-dark-2 px-4 py-3 cursor-grab transition hover:border-primary active:cursor-grabbing"
            @dragstart="onDragStart(course)"
            @dragend="exchangeStore.endDrag()"
          >
            <svg class="shrink-0 text-light/60" width="12" height="18" viewBox="0 0 12 18" fill="currentColor">
              <circle cx="3" cy="3" r="1.5" /><circle cx="9" cy="3" r="1.5" />
              <circle cx="3" cy="9" r="1.5" /><circle cx="9" cy="9" r="1.5" />
              <circle cx="3" cy="15" r="1.5" /><circle cx="9" cy="15" r="1.5" />
            </svg>
            <div class="min-w-0 flex-1">
              <div class="text-xs font-bold text-light">{{ course.code }}</div>
              <div class="text-sm font-medium text-light">{{ course.name }}</div>
              <div class="text-xs text-light/60">{{ course.nameHr ?? '-' }}</div>
              <div class="mt-1 flex items-center gap-1.5">
                <span class="truncate rounded bg-white/5 px-1.5 py-0.5 text-[11px] text-light/40">{{ semesterLabel(course.semester) }}</span>
                <span class="truncate rounded bg-white/5 px-1.5 py-0.5 text-[11px] text-light/40">{{ levelLabel(course.level) }}</span>
              </div>
            </div>
            <div class="shrink-0 flex items-center gap-2">
              <span
                v-if="mappedEcts(course.id) > 0"
                class="rounded px-2 py-0.5 text-xs font-semibold bg-amber-500/20 text-amber-300"
              >{{ mappedEcts(course.id) }}/{{ course.ects }} ECTS</span>
              <span v-else class="rounded bg-primary/20 px-2 py-0.5 text-xs font-semibold text-primary-light">
                {{ course.ects }} ECTS
              </span>
              <button
                v-if="!exchangeStore.stagedPartnerCourseIds.has(course.id)"
                :title="t('partnerCourses.stageAdd')"
                class="flex items-center justify-center w-6 h-6 rounded text-light/40 hover:text-primary hover:bg-primary/10 transition"
                @click.stop="exchangeStore.stagePartnerCourse(course.id)"
              >
                <svg width="12" height="12" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
                  <line x1="6" y1="1" x2="6" y2="11" /><line x1="1" y1="6" x2="11" y2="6" />
                </svg>
              </button>
              <span v-else :title="t('partnerCourses.stageAdded')" class="flex items-center justify-center w-6 h-6 rounded text-green-400">
                <svg width="12" height="12" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <polyline points="2,6 5,9 10,3" />
                </svg>
              </span>
            </div>
          </div>
        </div>
      </template>

      <template v-if="variant === 'mapped' || variant === 'all'">
        <div class="max-h-[400px] space-y-1.5 overflow-y-auto pr-1" :class="variant === 'all' ? 'mt-4' : ''">
          <div
            v-for="course in mappedCourses"
            :key="course.id"
            draggable="true"
            class="flex items-center gap-3 rounded-lg px-4 py-3 cursor-grab transition hover:border-primary active:cursor-grabbing"
            :class="
              mappedEcts(course.id) === 0
                ? 'border border-dashed border-light/20 bg-dark-2'
                : 'border border-green-500/30 bg-green-900/10'
            "
            @dragstart="onDragStart(course)"
            @dragend="exchangeStore.endDrag()"
          >
            <svg class="shrink-0 text-light/60" width="12" height="18" viewBox="0 0 12 18" fill="currentColor">
              <circle cx="3" cy="3" r="1.5" /><circle cx="9" cy="3" r="1.5" />
              <circle cx="3" cy="9" r="1.5" /><circle cx="9" cy="9" r="1.5" />
              <circle cx="3" cy="15" r="1.5" /><circle cx="9" cy="15" r="1.5" />
            </svg>
            <div class="min-w-0 flex-1">
              <div class="text-xs font-bold text-light">{{ course.code }}</div>
              <div class="text-sm font-medium text-light">{{ course.name }}</div>
              <div class="text-xs text-light/60">{{ course.nameHr ?? '-' }}</div>
              <div class="mt-1 flex items-center gap-1.5">
                <span class="truncate rounded bg-white/5 px-1.5 py-0.5 text-[11px] text-light/40">{{ semesterLabel(course.semester) }}</span>
                <span class="truncate rounded bg-white/5 px-1.5 py-0.5 text-[11px] text-light/40">{{ levelLabel(course.level) }}</span>
              </div>
            </div>
            <div class="shrink-0 flex items-center gap-2">
              <span
                class="rounded px-2 py-0.5 text-xs font-semibold"
                :class="
                  mappedEcts(course.id) === 0
                    ? 'bg-light/10 text-light/40'
                    : mappedEcts(course.id) >= course.ects
                      ? 'bg-green-500/20 text-green-300'
                      : 'bg-amber-500/20 text-amber-300'
                "
              >{{ mappedEcts(course.id) }}/{{ course.ects }} ECTS</span>
              <button
                class="flex items-center justify-center w-5 h-5 rounded text-light/40 hover:text-red-400 hover:bg-red-400/10 transition"
                @click.stop="exchangeStore.localRemoveAllMappingsForCourse(course.id); exchangeStore.unstagePartnerCourse(course.id)"
              >
                <svg width="10" height="10" viewBox="0 0 10 10" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
                  <line x1="1" y1="1" x2="9" y2="9" /><line x1="9" y1="1" x2="1" y2="9" />
                </svg>
              </button>
            </div>
          </div>
          <p v-if="mappedCourses.length === 0" class="py-4 text-center text-xs text-light/60">
            {{ t('partnerCourses.noMapped') }}
          </p>
        </div>
      </template>
    </template>
  </div>
</template>
