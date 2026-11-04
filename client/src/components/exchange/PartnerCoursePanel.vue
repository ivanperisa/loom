<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLearningAgreementDraftStore } from '@/stores/learningAgreementDraft.store'
import { useExchangeContext } from '@/composables/useExchangeContext'
import { useAddPartnerCourse, usePartnerCoursesQuery } from '@/queries/exchange.queries'
import type { PartnerCourseResponse } from '@/types/institution.types'
import SearchInput from '@/components/common/SearchInput.vue'
import PartnerCourseFormModal from '@/components/common/PartnerCourseFormModal.vue'
import { useDebouncedRef } from '@/composables/useDebouncedRef'
import { useNotification } from '@/composables/useNotification'
import { ArrowRightIcon, CheckIcon, ExternalLinkIcon, GripVerticalIcon, PlusIcon, XIcon } from '@lucide/vue'

const props = withDefaults(
  defineProps<{
    exchangeId: string
    variant?: 'available' | 'mapped' | 'all'
  }>(),
  { variant: 'all' },
)

const { t } = useI18n()
const draft = useLearningAgreementDraftStore()
const { exchange } = useExchangeContext()
const coursesQuery = usePartnerCoursesQuery(() => props.exchangeId)
const addCourse = useAddPartnerCourse(() => props.exchangeId)
const { notifySuccess } = useNotification()

const courses = computed(() => coursesQuery.data.value ?? [])
const loading = computed(() => coursesQuery.isPending.value)
const searchQuery = ref('')
const debouncedSearchQuery = useDebouncedRef(searchQuery)

const showAddForm = ref(false)
const addingCourse = computed(() => addCourse.isPending.value)
const addError = ref<string | null>(null)
const initialNameForForm = ref('')

function openAddForm() {
  addError.value = null
  initialNameForForm.value = searchQuery.value.trim()
  showAddForm.value = true
}

async function submitAddCourse(payload: {
  code: string; name: string; nameHr?: string; ects: number; semester: string; level: string
  lecturesH?: number; auditoryH?: number; labH?: number
}) {
  addError.value = null
  try {
    await addCourse.mutateAsync(payload)
    notifySuccess(t('common.created.course'))
    showAddForm.value = false
  } catch {
    addError.value = t('partnerCourses.saveError')
  }
}

const visibleCourses = computed(() => {
  const semesterType = exchange.value?.semesterType
  return courses.value
    .filter((c) => !c.isDeleted)
    .filter(
      (c) =>
        !semesterType ||
        semesterType === 'Both' ||
        c.semester === 'Both' ||
        c.semester === semesterType,
    )
    .sort((a, b) => a.name.localeCompare(b.name))
})

const mappedEctsMap = computed(() => {
  const map = new Map<number, number>()
  for (const state of draft.slotStates) {
    for (const m of state.mappings) {
      map.set(m.partnerCourseId, (map.get(m.partnerCourseId) ?? 0) + m.awardedEcts)
    }
  }
  return map
})

function mappedEcts(courseId: number): number {
  return mappedEctsMap.value.get(courseId) ?? 0
}

const mappedCourses = computed(() => {
  const withEcts = courses.value
    .filter((c) => mappedEcts(c.id) > 0)
    .sort((a, b) => a.name.localeCompare(b.name))
  const stagedOnly = visibleCourses.value.filter(
    (c) => draft.stagedPartnerCourseIds.has(c.id) && mappedEcts(c.id) === 0,
  )
  return [...withEcts, ...stagedOnly]
})

const availableCourses = computed(() =>
  visibleCourses.value.filter((c) => mappedEcts(c.id) === 0 && !draft.stagedPartnerCourseIds.has(c.id))
)

const mappedCoursesTotalEcts = computed(() =>
  mappedCourses.value.reduce((sum, c) => sum + c.ects, 0)
)

defineExpose({ mappedCoursesTotalEcts })

const searchResults = computed(() => {
  const q = debouncedSearchQuery.value.trim().toLowerCase()
  if (!q) return availableCourses.value
  return availableCourses.value.filter(
    (c) =>
      c.code.toLowerCase().includes(q) ||
      c.name.toLowerCase().includes(q) ||
      (c.nameHr?.toLowerCase().includes(q) ?? false),
  )
})

function onDragStart(course: PartnerCourseResponse) {
  draft.startDrag(course)
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
            class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg border border-primary/20 text-light/60 transition hover:border-primary hover:text-primary-text"
            @click="openAddForm"
          >
            <PlusIcon :size="14" />
          </button>
        </div>

        <PartnerCourseFormModal
          v-if="showAddForm"
          mode="create"
          :initialName="initialNameForForm"
          :defaultSemester="exchange?.semesterType"
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
            role="button"
            tabindex="0"
            :aria-pressed="draft.armedCourse?.id === course.id"
            class="flex items-center gap-3 rounded-lg border px-4 py-3 cursor-grab transition hover:border-primary focus-visible:outline focus-visible:outline-2 focus-visible:outline-primary active:cursor-grabbing"
            :class="
              draft.draggingCourse?.id === course.id || draft.armedCourse?.id === course.id
                ? 'border-primary bg-primary/10'
                : 'border-primary/20 bg-dark-2'
            "
            @click="draft.armCourse(course)"
            @keydown.enter.self.prevent="draft.armCourse(course)"
            @keydown.space.self.prevent="draft.armCourse(course)"
            @dragstart="onDragStart(course)"
            @dragend="draft.endDrag()"
          >
            <GripVerticalIcon class="shrink-0 text-light/60" :size="12" />
            <div class="min-w-0 flex-1">
              <div class="text-xs font-bold text-light">{{ course.code }}</div>
              <div class="text-sm font-medium text-light">{{ course.name }}</div>
              <div class="text-xs text-light/60">{{ course.nameHr ?? '-' }}</div>
            </div>
            <div class="shrink-0 flex items-center gap-2">
              <a
                v-if="course.url"
                :href="course.url"
                target="_blank"
                rel="noopener noreferrer"
                :title="t('admin.institutions.courseUrl')"
                class="flex h-6 w-6 items-center justify-center rounded text-blue-400 transition hover:bg-blue-400/10 hover:text-blue-300"
                @click.stop
              >
                <ExternalLinkIcon :size="12" />
              </a>
              <span class="rounded bg-fill-soft px-1.5 py-0.5 text-[11px] text-light/40">{{ semesterLabel(course.semester) }}</span>
              <span class="rounded bg-fill-soft px-1.5 py-0.5 text-[11px] text-light/40">{{ levelLabel(course.level) }}</span>
              <span
                v-if="mappedEcts(course.id) > 0"
                class="rounded px-2 py-0.5 text-xs font-semibold bg-info-fill text-info-text"
              >{{ mappedEcts(course.id) }}/{{ course.ects }} ECTS</span>
              <span v-else class="rounded bg-primary/20 px-2 py-0.5 text-xs font-semibold text-primary-text">
                {{ course.ects }} ECTS
              </span>
              <button
                v-if="!draft.stagedPartnerCourseIds.has(course.id)"
                :title="t('partnerCourses.stageAdd')"
                class="flex items-center justify-center w-6 h-6 rounded text-light/40 hover:text-primary-strong hover:bg-primary/10 transition"
                @click.stop="draft.stagePartnerCourse(course.id)"
              >
                <ArrowRightIcon :size="12" />
              </button>
              <span v-else :title="t('partnerCourses.stageAdded')" class="flex items-center justify-center w-6 h-6 rounded text-success-text">
                <CheckIcon :size="12" />
              </span>
            </div>
          </div>
        </div>
      </template>

      <template v-if="variant === 'mapped' || variant === 'all'">
        <div class="space-y-1.5" :class="variant === 'all' ? 'mt-4' : ''">
          <div
            v-for="course in mappedCourses"
            :key="course.id"
            draggable="true"
            class="flex items-center gap-3 rounded-lg px-4 py-3 cursor-grab transition hover:border-primary active:cursor-grabbing"
            :class="
              draft.draggingCourse?.id === course.id || draft.armedCourse?.id === course.id
                ? 'border border-primary bg-primary/10'
                : mappedEcts(course.id) === 0
                  ? 'border border-dashed border-light/20 bg-dark-2'
                  : 'border border-success-text/35 bg-success-fill'
            "
            @click="draft.armCourse(course)"
            @keydown.enter.self.prevent="draft.armCourse(course)"
            @keydown.space.self.prevent="draft.armCourse(course)"
            @dragstart="onDragStart(course)"
            @dragend="draft.endDrag()"
          >
            <GripVerticalIcon class="shrink-0 text-light/60" :size="12" />
            <div class="min-w-0 flex-1">
              <div class="text-xs font-bold text-light">{{ course.code }}</div>
              <div class="text-sm font-medium text-light">{{ course.name }}</div>
              <div class="text-xs text-light/60">{{ course.nameHr ?? '-' }}</div>
            </div>
            <div class="shrink-0 flex items-center gap-2">
              <a
                v-if="course.url"
                :href="course.url"
                target="_blank"
                rel="noopener noreferrer"
                :title="t('admin.institutions.courseUrl')"
                class="flex h-6 w-6 items-center justify-center rounded text-blue-400 transition hover:bg-blue-400/10 hover:text-blue-300"
                @click.stop
              >
                <ExternalLinkIcon :size="12" />
              </a>
              <span class="rounded bg-fill-soft px-1.5 py-0.5 text-[11px] text-light/40">{{ semesterLabel(course.semester) }}</span>
              <span class="rounded bg-fill-soft px-1.5 py-0.5 text-[11px] text-light/40">{{ levelLabel(course.level) }}</span>
              <span
                class="rounded px-2 py-0.5 text-xs font-semibold"
                :class="
                  mappedEcts(course.id) === 0
                    ? 'bg-light/10 text-light/40'
                    : mappedEcts(course.id) >= course.ects
                      ? 'bg-success-fill text-success-text'
                      : 'bg-info-fill text-info-text'
                "
              >{{ mappedEcts(course.id) }}/{{ course.ects }} ECTS</span>
              <button
                type="button"
                :aria-label="`${t('common.remove')}: ${course.code}`"
                class="flex items-center justify-center w-5 h-5 rounded text-light/40 hover:text-red-400 hover:bg-red-400/10 transition"
                @click.stop="draft.removeAllMappingsForCourse(course.id); draft.unstagePartnerCourse(course.id)"
              >
                <XIcon :size="10" />
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
