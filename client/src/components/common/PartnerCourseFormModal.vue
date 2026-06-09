<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { PartnerCourseResponse } from '@/types/institution.types'

const props = withDefaults(
  defineProps<{
    mode: 'create' | 'edit'
    institutionName?: string
    course?: PartnerCourseResponse | null
    initialName?: string
    saving?: boolean
    error?: string | null
  }>(),
  { saving: false, error: null, course: null, initialName: '' },
)

const emit = defineEmits<{
  submit: [payload: {
    code: string
    name: string
    nameHr?: string
    ects: number
    semester: string
    level: string
    lecturesH?: number
    auditoryH?: number
    labH?: number
  }]
  close: []
}>()

const { t } = useI18n()

const semesters = ['Winter', 'Summer', 'Both']
const levels = ['Undergraduate', 'Graduate', 'Postgraduate']

function formFromCourse(course: PartnerCourseResponse | null | undefined) {
  if (!course) return { code: '', name: props.initialName || '', nameHr: '', ects: '', semester: 'Winter', level: 'Graduate', lecturesH: '', auditoryH: '', labH: '' }
  return {
    code: course.code,
    name: course.name,
    nameHr: course.nameHr ?? '',
    ects: String(course.ects),
    semester: course.semester,
    level: course.level,
    lecturesH: course.lecturesH != null ? String(course.lecturesH) : '',
    auditoryH: course.auditoryH != null ? String(course.auditoryH) : '',
    labH: course.labH != null ? String(course.labH) : '',
  }
}

const courseForm = ref(formFromCourse(props.course))

function semesterLabel(semester: string) {
  return t(`exchangeSemester.${semester}`)
}

function levelLabel(level: string) {
  const map: Record<string, string> = {
    Undergraduate: t('admin.institutions.levelUndergraduate'),
    Graduate: t('admin.institutions.levelGraduate'),
    Postgraduate: t('admin.institutions.levelPostgraduate'),
  }
  return map[level] ?? level
}

function submit() {
  const f = courseForm.value
  if (!f.code.trim() || !f.name.trim() || !f.ects) return
  emit('submit', {
    code: f.code.trim(),
    name: f.name.trim(),
    nameHr: f.nameHr.trim() || undefined,
    ects: parseFloat(f.ects),
    semester: f.semester,
    level: f.level,
    lecturesH: f.lecturesH ? parseInt(f.lecturesH) : undefined,
    auditoryH: f.auditoryH ? parseInt(f.auditoryH) : undefined,
    labH: f.labH ? parseInt(f.labH) : undefined,
  })
}
</script>

<template>
  <Teleport to="body">
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 px-4"
      @mousedown.self="emit('close')"
    >
      <div class="w-full max-w-2xl rounded-2xl border border-primary/20 bg-dark-2 shadow-2xl">
        <div class="flex items-center justify-between border-b border-primary/20 px-6 py-4">
          <div>
            <h3 class="font-semibold text-light">{{ mode === 'edit' ? t('admin.institutions.editCourse') : t('admin.institutions.addCourse') }}</h3>
            <p v-if="institutionName" class="mt-0.5 text-xs text-light/40">{{ institutionName }}</p>
          </div>
          <button type="button" class="text-light/40 transition hover:text-white" @click="emit('close')">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
        <div class="space-y-4 px-6 py-5">
          <!-- Code + ECTS -->
          <div class="flex gap-3">
            <div class="w-36 flex-shrink-0">
              <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.courseCode') }} *</label>
              <input v-model="courseForm.code" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none font-mono uppercase" placeholder="e.g. CS101" />
            </div>
            <div class="w-28 flex-shrink-0">
              <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.courseEcts') }} *</label>
              <input v-model="courseForm.ects" type="number" step="0.5" min="0" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" placeholder="6" />
            </div>
          </div>
          <!-- Semester + Level -->
          <div class="flex gap-3">
            <div class="flex-1">
              <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.courseSemester') }} *</label>
              <div class="flex gap-2">
                <button
                  v-for="sem in semesters"
                  :key="sem"
                  type="button"
                  class="flex-1 rounded-lg border py-2 text-xs font-medium transition"
                  :class="courseForm.semester === sem ? 'border-primary bg-primary/10 text-white' : 'border-white/10 bg-dark text-light/60 hover:border-primary/40 hover:text-white'"
                  @click="courseForm.semester = sem"
                >{{ semesterLabel(sem) }}</button>
              </div>
            </div>
            <div class="flex-1">
              <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.level') }} *</label>
              <div class="flex gap-2">
                <button
                  v-for="lvl in levels"
                  :key="lvl"
                  type="button"
                  class="flex-1 rounded-lg border py-2 text-xs font-medium transition"
                  :class="courseForm.level === lvl ? 'border-primary bg-primary/10 text-white' : 'border-white/10 bg-dark text-light/60 hover:border-primary/40 hover:text-white'"
                  @click="courseForm.level = lvl"
                >{{ levelLabel(lvl) }}</button>
              </div>
            </div>
          </div>
          <!-- Name -->
          <div>
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.courseName') }} *</label>
            <input v-model="courseForm.name" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
          </div>
          <!-- Name HR -->
          <div>
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.courseNameHr') }}</label>
            <input v-model="courseForm.nameHr" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
          </div>
          <!-- Hours -->
          <div>
            <label class="mb-1.5 block text-sm text-light/70">
              Sati nastave
              <span class="text-light/30">({{ t('admin.institutions.optional') }})</span>
            </label>
            <div class="grid grid-cols-3 gap-3">
              <div>
                <label class="mb-1 block text-xs text-light/40">{{ t('admin.institutions.courseLecturesH') }}</label>
                <input v-model="courseForm.lecturesH" type="number" min="0" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" placeholder="0" />
              </div>
              <div>
                <label class="mb-1 block text-xs text-light/40">{{ t('admin.institutions.courseAuditoryH') }}</label>
                <input v-model="courseForm.auditoryH" type="number" min="0" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" placeholder="0" />
              </div>
              <div>
                <label class="mb-1 block text-xs text-light/40">{{ t('admin.institutions.courseLabH') }}</label>
                <input v-model="courseForm.labH" type="number" min="0" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" placeholder="0" />
              </div>
            </div>
          </div>
          <p v-if="error" class="text-xs text-red-400">{{ error }}</p>
        </div>
        <div class="flex justify-end gap-2 border-t border-primary/20 px-6 py-4">
          <button type="button" class="rounded-lg border border-white/10 px-4 py-2 text-sm text-light/60 transition hover:text-light" @click="emit('close')">{{ t('admin.institutions.cancel') }}</button>
          <button
            type="button"
            class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
            :disabled="saving || !courseForm.code.trim() || !courseForm.name.trim() || !courseForm.ects"
            @click="submit"
          >
            {{ saving ? t('common.loading') : (mode === 'edit' ? t('admin.institutions.saveEdit') : t('admin.institutions.save')) }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
