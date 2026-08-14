<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import type { PartnerCourseResponse, PartnerCourseUsage } from '@/types/institution.types'
import { nWord } from '@/utils/plural'

const props = defineProps<{
  course: PartnerCourseResponse
  selectable: boolean
  selected: boolean
  busy: boolean
}>()
const emit = defineEmits<{
  'toggle-select': [courseId: string]
  edit: [course: PartnerCourseResponse]
  delete: [courseId: string]
  restore: [courseId: string]
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
    <div class="flex items-center justify-between py-1.5" :class="course.isDeleted ? 'opacity-60' : ''">
      <div class="flex min-w-0 items-center gap-2">
        <button
          type="button"
          class="flex h-5 w-5 shrink-0 items-center justify-center rounded text-light/30 transition hover:bg-white/10 hover:text-light"
          :title="t('admin.institutions.usage.toggle')"
          @click="toggleUsage"
        >
          <svg class="h-3 w-3 transition-transform" :class="expanded ? 'rotate-90' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>
        <input
          v-if="selectable"
          type="checkbox"
          class="shrink-0 accent-primary"
          :checked="selected"
          @change="emit('toggle-select', course.id)"
        />
        <div class="flex min-w-0 items-baseline gap-3">
          <span class="w-16 flex-shrink-0 font-mono text-xs text-light/50">{{ course.code }}</span>
          <div class="min-w-0">
            <span class="text-xs text-light">{{ course.name }}</span>
            <span v-if="course.nameHr" class="ml-2 text-xs text-light/40">/ {{ course.nameHr }}</span>
            <span v-if="course.isDeleted" class="ml-2 rounded border border-red-400/30 bg-red-500/10 px-1.5 py-0.5 text-[10px] text-red-300">{{ t('admin.institutions.deleted') }}</span>
          </div>
          <a
            v-if="course.url"
            :href="course.url"
            target="_blank"
            rel="noopener noreferrer"
            :title="t('admin.institutions.courseUrl')"
            class="flex h-5 w-5 flex-shrink-0 items-center justify-center rounded text-light/40 transition hover:bg-primary/10 hover:text-primary-light"
          >
            <svg width="11" height="11" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
              <path d="M5 2H2a1 1 0 0 0-1 1v7a1 1 0 0 0 1 1h7a1 1 0 0 0 1-1V7" />
              <path d="M8 1h3v3" /><line x1="11" y1="1" x2="5" y2="7" />
            </svg>
          </a>
        </div>
      </div>
      <div class="flex flex-shrink-0 items-center gap-3 text-xs text-light/40">
        <span class="w-28 flex-shrink-0 truncate rounded bg-white/5 px-2 py-0.5 text-left text-xs text-light/40">{{ semesterLabel(course.semester) }}</span>
        <span class="w-28 flex-shrink-0 truncate rounded bg-white/5 px-2 py-0.5 text-left text-xs text-light/40">{{ levelLabel(course.level) }}</span>
        <span class="font-medium text-light/60">{{ course.ects }} ECTS</span>
        <button
          v-if="course.isDeleted"
          type="button"
          class="rounded border border-green-400/30 px-2 py-0.5 text-xs font-medium text-green-300 transition hover:bg-green-500/10 disabled:opacity-40"
          :disabled="busy"
          @click="emit('restore', course.id)"
        >
          {{ t('admin.institutions.restore') }}
        </button>
        <template v-else>
          <button
            type="button"
            class="flex h-6 w-6 items-center justify-center rounded text-light/40 transition hover:bg-primary/10 hover:text-primary-light disabled:opacity-40"
            :title="t('admin.institutions.editCourse')"
            @click="emit('edit', course)"
          >
            <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            type="button"
            class="flex h-6 w-6 items-center justify-center rounded text-red-400/50 transition hover:bg-red-500/10 hover:text-red-300 disabled:opacity-40"
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
    <div v-if="expanded" class="py-1.5 pb-2.5 pl-7 pr-2 text-xs">
      <div v-if="loadingUsage" class="h-4 w-32 animate-pulse rounded bg-white/5"></div>
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
              class="flex flex-wrap items-center justify-between gap-2 rounded bg-white/5 px-2 py-1"
            >
              <div class="min-w-0 text-light/70">
                <span>{{ group.programName }} &middot; {{ group.profileName }}</span>
                <span class="mx-1 text-light/30">&rarr;</span>
                <span class="text-light">
                  <template v-if="group.recognizedAsIsvuCode">[{{ group.recognizedAsIsvuCode }}] </template>{{ group.recognizedAsName }}
                </span>
              </div>
              <div class="flex shrink-0 flex-wrap items-center gap-1.5">
                <span class="rounded bg-white/10 px-1.5 py-0.5 text-light/50">{{ group.exchangeCount }}&times;</span>
                <span v-for="year in group.academicYears" :key="year" class="rounded bg-primary/10 px-1.5 py-0.5 text-primary-light">{{ year }}</span>
                <span class="font-medium text-light/60">{{ group.totalAwardedEcts }} ECTS</span>
              </div>
            </div>
          </div>
        </template>
      </template>
    </div>
  </div>
</template>
