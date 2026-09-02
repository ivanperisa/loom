<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import axios from 'axios'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import type { PartnerCourseResponse } from '@/types/institution.types'
import PartnerCourseFormModal from '@/components/common/PartnerCourseFormModal.vue'
import Pagination from '@/components/common/Pagination.vue'
import SortableHeader from '@/components/common/SortableHeader.vue'
import PartnerCourseRow from '@/components/admin/PartnerCourseRow.vue'
import PartnerCourseToolbar from '@/components/admin/PartnerCourseToolbar.vue'
import MergeCoursesModal from '@/components/admin/MergeCoursesModal.vue'
import { useConfirm } from '@/composables/useConfirm'
import { useDebouncedRef } from '@/composables/useDebouncedRef'
import { minSearchTerm } from '@/utils/searchTerm'

const COURSE_PER_PAGE = 10

const props = defineProps<{ institutionId: string; institutionName: string; institutionNameHr?: string | null; autoOpenCreate?: boolean }>()
const emit = defineEmits<{ 'count-changed': [delta: number] }>()

const { t } = useI18n()
const { confirm } = useConfirm()

const loading = ref(true)
const error = ref<string | null>(null)
const courses = ref<PartnerCourseResponse[]>([])
const totalCount = ref(0)
const coursePage = ref(1)
const totalCoursePages = computed(() => Math.max(1, Math.ceil(totalCount.value / COURSE_PER_PAGE)))

const courseSearch = ref('')
const debouncedCourseSearch = useDebouncedRef(courseSearch, 400)
const semesterFilter = ref<string | null>(null)
const levelFilter = ref<string | null>(null)
const showDeletedCourses = ref(false)
const hasDeletedCourses = ref(false)

const sortBy = ref('name')
const sortDir = ref<'asc' | 'desc'>('asc')

const codeColumnWidth = computed(() => `${(Math.max(3, ...courses.value.map(c => c.code.length)) + 2) * 7.2}px`)

function toggleSort(key: string) {
  if (sortBy.value === key) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortBy.value = key
    sortDir.value = 'asc'
  }
}

const courseModal = ref<{ mode: 'create' | 'edit'; course?: PartnerCourseResponse; initialName?: string } | null>(null)
const savingCourse = ref(false)
const courseError = ref<string | null>(null)
const deletingCourse = ref<string | null>(null)

const mergeSelecting = ref(false)
const selectedForMerge = ref<Set<string>>(new Set())
const mergeModal = ref<{ courses: PartnerCourseResponse[] } | null>(null)
const merging = ref(false)

let loadCoursesController: AbortController | null = null

async function loadCourses() {
  loadCoursesController?.abort()
  const controller = new AbortController()
  loadCoursesController = controller
  loading.value = true
  try {
    const res = await institutionService.getPartnerCoursesByInstitution(props.institutionId, showDeletedCourses.value, {
      page: coursePage.value,
      pageSize: COURSE_PER_PAGE,
      search: minSearchTerm(debouncedCourseSearch.value),
      semester: semesterFilter.value,
      level: levelFilter.value,
      sortBy: sortBy.value,
      sortDir: sortDir.value,
    }, controller.signal)
    courses.value = res.data.items
    totalCount.value = res.data.totalCount
    hasDeletedCourses.value = res.data.hasDeleted
    if (coursePage.value > totalCoursePages.value) coursePage.value = totalCoursePages.value
  } catch (err) {
    if (axios.isCancel(err)) return
    throw err
  } finally {
    if (loadCoursesController === controller) loading.value = false
  }
}

onMounted(() => {
  loadCourses()
  if (props.autoOpenCreate) openCreate()
})

watch(
  [coursePage, debouncedCourseSearch, semesterFilter, levelFilter, showDeletedCourses, sortBy, sortDir],
  ([newPage], [oldPage, oldSearch, oldSemester, oldLevel, oldShowDeleted]) => {
    if ((debouncedCourseSearch.value !== oldSearch || semesterFilter.value !== oldSemester || levelFilter.value !== oldLevel || showDeletedCourses.value !== oldShowDeleted) && newPage !== 1) {
      coursePage.value = 1
      return
    }
    if (newPage !== oldPage) selectedForMerge.value = new Set()
    loadCourses()
  },
)

function openCreate() {
  courseError.value = null
  courseModal.value = { mode: 'create', initialName: courseSearch.value || undefined }
}

defineExpose({ openCreate })

function openEdit(course: PartnerCourseResponse) {
  courseError.value = null
  courseModal.value = { mode: 'edit', course }
}

async function submitCourse(payload: {
  code: string; name: string; nameHr?: string; ects: number; semester: string; level: string
  lecturesH?: number; auditoryH?: number; labH?: number
}) {
  if (!courseModal.value) return
  savingCourse.value = true
  courseError.value = null
  try {
    if (courseModal.value.mode === 'edit' && courseModal.value.course) {
      const courseId = courseModal.value.course.id
      await institutionService.updatePartnerCourse(courseId, payload)
    } else {
      await institutionService.createPartnerCourseByInstitution(props.institutionId, payload)
      emit('count-changed', 1)
    }
    await loadCourses()
    courseModal.value = null
  } catch (e: unknown) {
    const err = e as { response?: { status?: number } }
    courseError.value = err.response?.status === 409 ? t('admin.institutions.duplicateCourseCode') : t('admin.institutions.saveError')
  } finally {
    savingCourse.value = false
  }
}

async function deleteCourse(courseId: string) {
  if (!await confirm({ title: t('admin.institutions.deleteCourseConfirm') })) return
  deletingCourse.value = courseId
  error.value = null
  try {
    await institutionService.deletePartnerCourse(courseId)
    await loadCourses()
    emit('count-changed', -1)
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    deletingCourse.value = null
  }
}

async function restoreCourse(courseId: string) {
  deletingCourse.value = courseId
  error.value = null
  try {
    await institutionService.restorePartnerCourse(courseId)
    await loadCourses()
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    deletingCourse.value = null
  }
}

function startMergeSelection() {
  mergeSelecting.value = true
  selectedForMerge.value = new Set()
}

function cancelMergeSelection() {
  mergeSelecting.value = false
  selectedForMerge.value = new Set()
}

function toggleCourseForMerge(courseId: string) {
  const set = selectedForMerge.value
  if (set.has(courseId)) set.delete(courseId)
  else set.add(courseId)
  selectedForMerge.value = new Set(set)
}

function openMergeModalFromSelection() {
  const selected = courses.value.filter(c => selectedForMerge.value.has(c.id))
  if (selected.length < 2) return
  mergeModal.value = { courses: selected }
  cancelMergeSelection()
}

async function submitMerge(primaryId: string) {
  if (!mergeModal.value) return
  const duplicateIds = mergeModal.value.courses.filter(c => c.id !== primaryId).map(c => c.id)
  merging.value = true
  error.value = null
  try {
    await institutionService.mergePartnerCourses(primaryId, duplicateIds)
    await loadCourses()
    emit('count-changed', -duplicateIds.length)
    mergeModal.value = null
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    merging.value = false
  }
}
</script>

<template>
  <div class="space-y-4">
    <div>
      <h2 class="text-xl font-semibold text-light">{{ institutionName }}</h2>
      <p v-if="institutionNameHr && institutionNameHr !== institutionName" class="text-sm text-light/40">{{ institutionNameHr }}</p>
    </div>

    <p v-if="error" class="rounded-xl border border-danger-text/35 bg-danger-fill px-4 py-3 text-sm text-danger-text">
      {{ error }}
    </p>

    <PartnerCourseToolbar
      :search="courseSearch"
      :semester="semesterFilter"
      :level="levelFilter"
      :show-deleted="showDeletedCourses"
      :has-deleted="hasDeletedCourses"
      :merge-selecting="mergeSelecting"
      :can-merge="courses.length > 1"
      :selected-count="selectedForMerge.size"
      @update:search="courseSearch = $event"
      @update:semester="semesterFilter = $event"
      @update:level="levelFilter = $event"
      @update:show-deleted="showDeletedCourses = $event"
      @start-merge="startMergeSelection"
      @confirm-merge="openMergeModalFromSelection"
      @cancel-merge="cancelMergeSelection"
    />

    <div v-if="loading && courses.length === 0" class="space-y-3">
      <div v-for="i in 4" :key="i" class="h-14 animate-pulse rounded-xl bg-dark-2"></div>
    </div>

    <div v-else-if="courses.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-6 text-center text-light/60">
      {{ courseSearch ? t('admin.institutions.noResults') : t('admin.institutions.noCourses') }}
    </div>

    <template v-else>
      <div class="overflow-x-auto rounded-xl border border-primary/20 bg-dark-2">
        <div class="min-w-[980px]" :style="{ '--code-col-width': codeColumnWidth }">
          <div class="admin-course-grid gap-3 border-b border-primary/20 px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-light/40" :class="{ 'has-checkbox': mergeSelecting }">
            <span v-if="mergeSelecting"></span>
            <SortableHeader :label="t('admin.institutions.courseColumns.code')" sort-key="code" :active-key="sortBy" :dir="sortDir" @sort="toggleSort" />
            <SortableHeader :label="t('admin.institutions.courseColumns.name')" sort-key="name" :active-key="sortBy" :dir="sortDir" @sort="toggleSort" />
            <SortableHeader :label="t('admin.institutions.courseColumns.nameHr')" sort-key="nameHr" :active-key="sortBy" :dir="sortDir" @sort="toggleSort" />
            <SortableHeader :label="t('admin.institutions.courseColumns.semester')" sort-key="semester" :active-key="sortBy" :dir="sortDir" @sort="toggleSort" />
            <SortableHeader :label="t('admin.institutions.courseColumns.level')" sort-key="level" :active-key="sortBy" :dir="sortDir" @sort="toggleSort" />
            <SortableHeader :label="t('admin.institutions.courseColumns.ects')" sort-key="ects" :active-key="sortBy" :dir="sortDir" @sort="toggleSort" />
            <span></span>
          </div>

          <div class="divide-y divide-hairline-soft">
            <PartnerCourseRow
              v-for="course in courses"
              :key="course.id"
              :course="course"
              :selectable="mergeSelecting"
              :selected="selectedForMerge.has(course.id)"
              :busy="deletingCourse === course.id"
              @toggle-select="toggleCourseForMerge"
              @edit="openEdit"
              @delete="deleteCourse"
              @restore="restoreCourse"
            />
          </div>
        </div>
      </div>

      <Pagination
        :page="coursePage"
        :total-pages="totalCoursePages"
        :total="totalCount"
        :per-page="COURSE_PER_PAGE"
        @update:page="coursePage = $event"
      />
    </template>

    <!-- Add/Edit Course Modal -->
    <PartnerCourseFormModal
      v-if="courseModal"
      :mode="courseModal.mode"
      :institution-name="institutionName"
      :course="courseModal.course"
      :initial-name="courseModal.initialName"
      :saving="savingCourse"
      :error="courseError"
      @submit="submitCourse"
      @close="courseModal = null"
    />

    <!-- Merge Courses Modal -->
    <MergeCoursesModal
      v-if="mergeModal"
      :courses="mergeModal.courses"
      :saving="merging"
      @submit="submitMerge"
      @close="mergeModal = null"
    />
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
