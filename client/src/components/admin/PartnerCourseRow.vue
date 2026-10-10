<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import CourseUrlLink from '@/components/common/CourseUrlLink.vue'
import type { PartnerCourseResponse, PartnerCourseUsage } from '@/types/institution.types'
import { nWord } from '@/utils/plural'

const props = defineProps<{
  course: PartnerCourseResponse
  selectable: boolean
  selected: boolean
  busy: boolean
}>()
const emit = defineEmits<{
  'toggle-select': [courseId: number]
  edit: [course: PartnerCourseResponse]
  delete: [courseId: number]
  restore: [courseId: number]
}>()

const { t, locale } = useI18n()

const expanded = ref(false)
const usage = ref<PartnerCourseUsage | null>(null)
const loadingUsage = ref(false)

async function toggleUsage() {
  if (expanded.value) {
    expanded.value = false
    return
  }
  expanded.value = true
  if (usage.value) return
  loadingUsage.value = true
  try {
    const res = await institutionService.getPartnerCourseUsage(props.course.id)
    usage.value = res.data
  } finally {
    loadingUsage.value = false
  }
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
    <div
      class="admin-course-grid cursor-pointer gap-3 px-4 py-3 text-sm transition"
      :class="[course.isDeleted ? 'opacity-60' : '', selectable ? 'has-checkbox hover:bg-primary/5' : 'hover:bg-dark']"
      @click="selectable ? emit('toggle-select', course.id) : toggleUsage()"
    >
      <span
        v-if="selectable"
        class="flex h-4 w-4 shrink-0 items-center justify-center rounded border transition-colors"
        :class="selected ? 'border-primary bg-primary' : 'border-hairline bg-fill-soft'"
      >
        <svg v-if="selected" class="h-3 w-3 text-white" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M16.704 5.29a1 1 0 010 1.415l-7.5 7.5a1 1 0 01-1.414 0l-3.5-3.5a1 1 0 111.414-1.414l2.793 2.793 6.793-6.793a1 1 0 011.414 0z" clip-rule="evenodd" />
        </svg>
      </span>

      <span class="min-w-0">
        <span :title="course.code" class="inline-block max-w-full truncate rounded border border-primary/30 bg-primary/10 px-1.5 py-0.5 align-middle font-mono text-xs text-primary-text">{{ course.code }}</span>
      </span>

      <p class="line-clamp-2 text-light" :title="course.name">{{ course.name }}</p>

      <p class="line-clamp-2 text-light/70" :title="course.nameHr || undefined">{{ course.nameHr || '—' }}</p>
      <span class="truncate text-light/70">{{ semesterLabel(course.semester) }}</span>
      <span class="truncate text-light/70">{{ levelLabel(course.level) }}</span>
      <span class="text-light/70">{{ course.ects }}</span>

      <div class="flex items-center justify-end gap-1" @click.stop>
        <CourseUrlLink
          v-if="course.url"
          block
          :size="12"
          :url="course.url"
          :title="t('admin.institutions.courseUrl')"
          class="flex h-6 w-6 items-center justify-center rounded transition hover:bg-primary/10"
        />
        <button
          v-if="course.isDeleted"
          type="button"
          class="rounded border border-success-text/35 px-2 py-0.5 text-xs font-medium text-success-text transition hover:bg-success-fill disabled:opacity-40"
          :disabled="busy"
          @click="emit('restore', course.id)"
        >
          {{ t('admin.institutions.restore') }}
        </button>
        <template v-else>
          <button
            type="button"
            class="flex h-6 w-6 items-center justify-center rounded text-light/40 transition hover:bg-primary/10 hover:text-primary-text disabled:opacity-40"
            :title="t('admin.institutions.editCourse')"
            @click="emit('edit', course)"
          >
            <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            type="button"
            class="flex h-6 w-6 items-center justify-center rounded text-danger/50 transition hover:bg-danger-fill hover:text-danger-text disabled:opacity-40"
            :disabled="busy"
            :title="t('admin.institutions.deleteCourse')"
            @click="emit('delete', course.id)"
          >
            <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </template>
      </div>
    </div>

    <!-- Course usage detail -->
    <div v-if="expanded" class="border-t border-hairline-soft px-4 py-2.5 pl-11 text-xs">
      <div v-if="loadingUsage" class="h-4 w-32 animate-pulse rounded bg-fill-soft"></div>
      <template v-else-if="usage">
        <p v-if="usage.exchangeCount === 0" class="text-light/30">
          {{ t('admin.institutions.usage.empty') }}
        </p>
        <template v-else>
          <p class="mb-1.5 font-medium text-light/50">
            {{ t('admin.institutions.usage.usedIn') }}
            {{ nWord(usage.exchangeCount, locale, { en: ['exchange', 'exchanges'], hr: ['razmjeni', 'razmjene', 'razmjena'] }) }}
          </p>
          <div class="space-y-1">
            <div
              v-for="(group, gi) in usage.groups"
              :key="gi"
              class="flex flex-wrap items-center justify-between gap-2 rounded bg-fill-soft px-2 py-1"
            >
              <div class="min-w-0 text-light/70">
                <span>{{ group.programName }} &middot; {{ group.profileName }}</span>
                <span class="mx-1 text-light/30">&rarr;</span>
                <span class="text-light">
                  <template v-if="group.recognizedAsIsvuCode">[{{ group.recognizedAsIsvuCode }}] </template>{{ group.recognizedAsName }}
                </span>
              </div>
              <div class="flex shrink-0 flex-wrap items-center gap-1.5">
                <span class="rounded bg-fill px-1.5 py-0.5 text-light/50">{{ group.exchangeCount }}&times;</span>
                <span v-for="year in group.academicYears" :key="year" class="rounded bg-primary/10 px-1.5 py-0.5 text-primary-text">{{ year }}</span>
                <span class="font-medium text-light/60">{{ group.totalAwardedEcts }} ECTS</span>
              </div>
            </div>
          </div>
        </template>
      </template>
    </div>
  </div>
</template>

<style scoped>
.admin-course-grid {
  display: grid;
  grid-template-columns: var(--code-col-width, 90px) minmax(140px, 1fr) minmax(140px, 1fr) 100px 120px 80px 76px;
  align-items: center;
}
.admin-course-grid.has-checkbox {
  grid-template-columns: 32px var(--code-col-width, 90px) minmax(140px, 1fr) minmax(140px, 1fr) 100px 120px 80px 76px;
}
</style>
