<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
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
import { useListQuery } from '@/composables/useListQuery'
import { queryKeys } from '@/queries/keys'
import ErrorAlert from '@/components/common/ErrorAlert.vue'

const props = defineProps<{ institutionId: number; institutionName: string; institutionNameHr?: string | null; autoOpenCreate?: boolean }>()
const emit = defineEmits<{ 'count-changed': [delta: number] }>()

const { t } = useI18n()
const { confirm } = useConfirm()

const queryClient = useQueryClient()
/** An action that failed (the list itself shows its own load error). */
const error = ref<string | null>(null)

const semesterFilter = ref<string | null>(null)
const levelFilter = ref<string | null>(null)
const showDeletedCourses = ref(false)

const list = useListQuery<PartnerCourseResponse, { semester: string | null; level: string | null; includeDeleted: boolean }>({
  key: queryKeys.partnerInstitutionCourses(props.institutionId),
  fetch: (params, signal) =>
    institutionService.getPartnerCoursesByInstitution(props.institutionId, params.includeDeleted ?? false, params, signal),
  filters: { semester: semesterFilter, level: levelFilter, includeDeleted: showDeletedCourses },
  pageSize: 10,
  defaultSort: 'name',
})
const courses = list.items
const courseSearch = list.search

/** This list, and the course lists of exchanges at this partner (they may show the course). */
const refresh = () =>
  Promise.all([
    queryClient.invalidateQueries({ queryKey: queryKeys.partnerInstitutionCourses(props.institutionId) }),
    queryClient.invalidateQueries({ predicate: (q) => q.queryKey[0] === 'exchange' && q.queryKey[2] === 'partner-courses' }),
  ])

const codeColumnWidth = computed(() => `${(Math.max(3, ...courses.value.map(c => c.code.length)) + 2) * 7.2}px`)


const courseModal = ref<{ mode: 'create' | 'edit'; course?: PartnerCourseResponse; initialName?: string } | null>(null)
const savingCourse = ref(false)
const courseError = ref<string | null>(null)
const deletingCourse = ref<number | null>(null)

const mergeSelecting = ref(false)
const selectedForMerge = ref<Set<number>>(new Set())
const mergeModal = ref<{ courses: PartnerCourseResponse[] } | null>(null)
const merging = ref(false)

onMounted(() => {
  if (props.autoOpenCreate) openCreate()
})

// A merge selection only makes sense on the page it was made on.
watch(list.page, () => (selectedForMerge.value = new Set()))

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
    await refresh()
    courseModal.value = null
  } catch (e: unknown) {
    const err = e as { response?: { status?: number } }
    courseError.value = err.response?.status === 409 ? t('admin.institutions.duplicateCourseCode') : t('admin.institutions.saveError')
  } finally {
    savingCourse.value = false
  }
}

async function deleteCourse(courseId: number) {
  if (!await confirm({ title: t('admin.institutions.deleteCourseConfirm') })) return
  deletingCourse.value = courseId
  error.value = null
  try {
    await institutionService.deletePartnerCourse(courseId)
    await refresh()
    emit('count-changed', -1)
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    deletingCourse.value = null
  }
}

async function restoreCourse(courseId: number) {
  deletingCourse.value = courseId
  error.value = null
  try {
    await institutionService.restorePartnerCourse(courseId)
    await refresh()
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

function toggleCourseForMerge(courseId: number) {
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

async function submitMerge(primaryId: number) {
  if (!mergeModal.value) return
  const duplicateIds = mergeModal.value.courses.filter(c => c.id !== primaryId).map(c => c.id)
  merging.value = true
  error.value = null
  try {
    await institutionService.mergePartnerCourses(primaryId, duplicateIds)
    await refresh()
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
      :has-deleted="list.hasDeleted.value"
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

    <div v-if="list.isPending.value" class="space-y-3">
      <div v-for="i in 4" :key="i" class="h-14 animate-pulse rounded-xl bg-dark-2"></div>
    </div>

    <ErrorAlert v-else-if="list.error.value" :error="list.error.value" @retry="list.refetch()" />

    <div v-else-if="courses.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-6 text-center text-light/60">
      {{ courseSearch ? t('admin.institutions.noResults') : t('admin.institutions.noCourses') }}
    </div>

    <template v-else>
      <div class="overflow-x-auto rounded-xl border border-primary/20 bg-dark-2">
        <div class="min-w-[980px]" :style="{ '--code-col-width': codeColumnWidth }">
          <div class="admin-course-grid gap-3 border-b border-primary/20 px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-light/40" :class="{ 'has-checkbox': mergeSelecting }">
            <span v-if="mergeSelecting"></span>
            <SortableHeader :label="t('admin.institutions.courseColumns.code')" sort-key="code" :active-key="list.sortKey.value" :dir="list.sortDir.value" @sort="list.toggleSort" />
            <SortableHeader :label="t('admin.institutions.courseColumns.name')" sort-key="name" :active-key="list.sortKey.value" :dir="list.sortDir.value" @sort="list.toggleSort" />
            <SortableHeader :label="t('admin.institutions.courseColumns.nameHr')" sort-key="nameHr" :active-key="list.sortKey.value" :dir="list.sortDir.value" @sort="list.toggleSort" />
            <SortableHeader :label="t('admin.institutions.courseColumns.semester')" sort-key="semester" :active-key="list.sortKey.value" :dir="list.sortDir.value" @sort="list.toggleSort" />
            <SortableHeader :label="t('admin.institutions.courseColumns.level')" sort-key="level" :active-key="list.sortKey.value" :dir="list.sortDir.value" @sort="list.toggleSort" />
            <SortableHeader :label="t('admin.institutions.courseColumns.ects')" sort-key="ects" :active-key="list.sortKey.value" :dir="list.sortDir.value" @sort="list.toggleSort" />
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
        :page="list.page.value"
        :total-pages="list.totalPages.value"
        :total="list.totalCount.value"
        :per-page="list.pageSize"
        @update:page="list.page.value = $event"
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
