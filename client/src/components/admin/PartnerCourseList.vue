<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import type { PartnerCourseResponse } from '@/types/institution.types'
import PartnerCourseFormModal from '@/components/common/PartnerCourseFormModal.vue'
import Pagination from '@/components/common/Pagination.vue'
import PartnerCourseRow from '@/components/admin/PartnerCourseRow.vue'
import PartnerCourseToolbar from '@/components/admin/PartnerCourseToolbar.vue'
import MergeCoursesModal from '@/components/admin/MergeCoursesModal.vue'
import { useConfirm } from '@/composables/useConfirm'
import { useDebouncedRef } from '@/composables/useDebouncedRef'
import { usePagination } from '@/composables/usePagination'

const COURSE_PER_PAGE = 10

const props = defineProps<{ institutionId: string; institutionName: string }>()
const emit = defineEmits<{ 'count-changed': [delta: number] }>()

const { t } = useI18n()
const { confirm } = useConfirm()

const loading = ref(true)
const error = ref<string | null>(null)
const courses = ref<PartnerCourseResponse[]>([])

const courseSearch = ref('')
const debouncedCourseSearch = useDebouncedRef(courseSearch)
const showDeletedCourses = ref(false)

const courseModal = ref<{ mode: 'create' | 'edit'; course?: PartnerCourseResponse; initialName?: string } | null>(null)
const savingCourse = ref(false)
const courseError = ref<string | null>(null)
const deletingCourse = ref<string | null>(null)

const mergeSelecting = ref(false)
const selectedForMerge = ref<Set<string>>(new Set())
const mergeModal = ref<{ courses: PartnerCourseResponse[] } | null>(null)
const merging = ref(false)

const hasDeletedCourses = computed(() => courses.value.some(c => c.isDeleted))

const filteredCourses = computed(() => {
  let list = courses.value
  if (!showDeletedCourses.value) list = list.filter(c => !c.isDeleted)
  const q = debouncedCourseSearch.value.trim().toLowerCase()
  if (q) {
    list = list.filter(c =>
      c.code.toLowerCase().includes(q) ||
      c.name.toLowerCase().includes(q) ||
      c.nameHr?.toLowerCase().includes(q)
    )
  }
  return [...list].sort((a, b) => a.name.localeCompare(b.name))
})

const {
  page: coursePage,
  totalPages: totalCoursePages,
  paged: pagedCourses,
} = usePagination(filteredCourses, COURSE_PER_PAGE)

function onCourseSearch() { coursePage.value = 1 }

async function loadCourses() {
  loading.value = true
  try {
    const res = await institutionService.getPartnerCoursesByInstitution(props.institutionId, true)
    courses.value = res.data
  } finally {
    loading.value = false
  }
}

onMounted(loadCourses)

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
      const res = await institutionService.updatePartnerCourse(courseId, payload)
      const idx = courses.value.findIndex(c => c.id === courseId)
      if (idx !== -1) courses.value[idx] = res.data
    } else {
      const res = await institutionService.createPartnerCourseByInstitution(props.institutionId, payload)
      courses.value.push(res.data)
      emit('count-changed', 1)
    }
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
    courses.value = courses.value.filter(c => c.id === primaryId || !duplicateIds.includes(c.id))
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
  <div class="border-t border-hairline-soft px-5 pb-4 pt-3">
    <div v-if="loading" class="space-y-1.5">
      <div v-for="i in 3" :key="i" class="h-7 animate-pulse rounded bg-fill-soft"></div>
    </div>
    <template v-else>
      <p v-if="error" class="mb-2 rounded-lg border border-danger/40 bg-danger/10 px-3 py-2 text-xs text-danger">
        {{ error }}
      </p>

      <PartnerCourseToolbar
        :search="courseSearch"
        :show-deleted="showDeletedCourses"
        :has-deleted="hasDeletedCourses"
        :merge-selecting="mergeSelecting"
        :can-merge="filteredCourses.length > 1"
        :selected-count="selectedForMerge.size"
        @update:search="courseSearch = $event; onCourseSearch()"
        @update:show-deleted="showDeletedCourses = $event"
        @start-merge="startMergeSelection"
        @confirm-merge="openMergeModalFromSelection"
        @cancel-merge="cancelMergeSelection"
      />

      <p v-if="filteredCourses.length === 0" class="text-xs text-light/30">
        {{ courseSearch ? t('admin.institutions.noResults') : t('admin.institutions.noCourses') }}
      </p>
      <div v-else>
        <div class="divide-y divide-hairline-soft">
          <PartnerCourseRow
            v-for="course in pagedCourses"
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

        <!-- Course pagination -->
        <Pagination
          :page="coursePage"
          :total-pages="totalCoursePages"
          :total="filteredCourses.length"
          :per-page="COURSE_PER_PAGE"
          @update:page="coursePage = $event"
        />
      </div>
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
