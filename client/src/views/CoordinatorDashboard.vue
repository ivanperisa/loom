<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { coordinatorService } from '@/services/coordinator.service'
import type { CoordinatorStudentResponse } from '@/types/coordinator.types'
import { useCopyAccessLink } from '@/composables/useCopyAccessLink'
import CreateExchangeModal from '@/components/exchange/CreateExchangeModal.vue'
import StudentFormModal from '@/components/coordinator/StudentFormModal.vue'
import StudentRow from '@/components/coordinator/StudentRow.vue'
import ExchangeSwitcherMenu from '@/components/coordinator/ExchangeSwitcherMenu.vue'
import StudentActionsMenu from '@/components/coordinator/StudentActionsMenu.vue'
import { usePopoverMenu } from '@/composables/usePopoverMenu'
import SearchableSelect from '@/components/common/SearchableSelect.vue'
import SearchInput from '@/components/common/SearchInput.vue'
import SortableHeader from '@/components/common/SortableHeader.vue'
import Pagination from '@/components/common/Pagination.vue'
import ErrorAlert from '@/components/common/ErrorAlert.vue'
import { useNotification } from '@/composables/useNotification'
import { useConfirm } from '@/composables/useConfirm'
import { useListQuery } from '@/composables/useListQuery'
import { useHomeInstitutionsQuery } from '@/queries/catalog.queries'
import { queryKeys } from '@/queries/keys'
import { isApiError } from '@/utils/apiError'

const router = useRouter()
const { t } = useI18n()
const { notifySuccess, notifyError } = useNotification()
const { confirm } = useConfirm()
const queryClient = useQueryClient()

const selectedAcademicYear = ref<string | null>(null)
const selectedPartnerInstitution = ref<string | null>(null)

const studentList = useListQuery<CoordinatorStudentResponse, { academicYear: string | null; partnerInstitution: string | null }>({
  key: queryKeys.coordinatorStudents,
  fetch: (params, signal) => coordinatorService.getStudents(params, signal),
  filters: { academicYear: selectedAcademicYear, partnerInstitution: selectedPartnerInstitution },
  pageSize: 10,
  defaultSort: 'name',
  syncToUrl: true,
})
const students = studentList.items
const studentSearch = studentList.search

const filtersQuery = useQuery({
  queryKey: [...queryKeys.coordinatorStudents, 'filters'],
  queryFn: async ({ signal }) => (await coordinatorService.getStudentFilters(signal)).data,
})
const homeInstitutionsQuery = useHomeInstitutionsQuery()
const institutions = computed(() => homeInstitutionsQuery.data.value ?? [])

const exchangesMenu = usePopoverMenu<number>(380)
const actionsMenu = usePopoverMenu<number>(200)

const showStudentModal = ref(false)
const studentModalMode = ref<'create' | 'edit'>('create')
const editingStudent = ref<CoordinatorStudentResponse | null>(null)
const deletingStudentId = ref<number | null>(null)

// Create exchange modal
const showCreateExchangeModal = ref(false)
const createExchangeTargetStudentId = ref<number | null>(null)

const academicYears = computed(() => filtersQuery.data.value?.academicYears ?? [])

const academicYearFilterOptions = computed(() => [
  { value: null, label: t('home.allYears') },
  ...academicYears.value.map((year) => ({ value: year, label: year })),
])

const partnerInstitutionOptions = computed(() => [
  { value: null, label: t('coordinator.filters.allInstitutions') },
  ...(filtersQuery.data.value?.partnerInstitutions ?? []).map((name) => ({ value: name, label: name })),
])

const openMenuStudent = computed(() => students.value.find((s) => s.id === exchangesMenu.openId.value) ?? null)
const actionsMenuStudent = computed(() => students.value.find((s) => s.id === actionsMenu.openId.value) ?? null)

watch([studentList.page, studentList.search, selectedAcademicYear, selectedPartnerInstitution], () => {
  closeMenu()
  closeActionsMenu()
})

function refreshStudents() {
  return queryClient.invalidateQueries({ queryKey: queryKeys.coordinatorStudents })
}

function closeMenu() {
  exchangesMenu.close()
}

function closeActionsMenu() {
  actionsMenu.close()
}

function handleOutsideClick(e: MouseEvent) {
  if (!(e.target as HTMLElement).closest('[data-menu-anchor]')) {
    closeMenu()
    closeActionsMenu()
  }
}

onMounted(() => document.addEventListener('click', handleOutsideClick))
onUnmounted(() => document.removeEventListener('click', handleOutsideClick))

function viewExchange(exchangeGuid: string) {
  closeMenu()
  router.push(`/exchange/${exchangeGuid}`)
}

const { copyAccessLink: copyLink } = useCopyAccessLink()

async function copyAccessLink(exchangeGuid: string) {
  closeMenu()
  await copyLink(exchangeGuid)
}

function openAddModal() {
  studentModalMode.value = 'create'
  editingStudent.value = null
  showStudentModal.value = true
}

function openEditStudent(student: CoordinatorStudentResponse) {
  closeActionsMenu()
  studentModalMode.value = 'edit'
  editingStudent.value = student
  showStudentModal.value = true
}

function onStudentSaved() {
  refreshStudents()
  showStudentModal.value = false
}

async function deleteStudent(student: CoordinatorStudentResponse) {
  closeActionsMenu()
  if (!await confirm({ title: t('coordinator.deleteStudentConfirm') })) return
  deletingStudentId.value = student.id
  try {
    await coordinatorService.deleteStudent(student.id)
    await refreshStudents()
    notifySuccess(t('coordinator.deleteStudent'))
  } catch (e: unknown) {
    notifyError(isApiError(e, 'HAS_EXCHANGES') ? t('coordinator.deleteStudentHasExchanges') : t('coordinator.deleteStudentError'))
  } finally {
    deletingStudentId.value = null
  }
}

function openCreateExchange(studentId: number) {
  closeMenu()
  closeActionsMenu()
  createExchangeTargetStudentId.value = studentId
  showCreateExchangeModal.value = true
}

function onExchangeCreated(exchangeGuid: string) {
  showCreateExchangeModal.value = false
  router.push(`/exchange/${exchangeGuid}`)
}
</script>

<template>
  <main class="min-h-screen bg-dark">
    <section class="page-container">
      <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h1 class="text-2xl font-bold text-light">{{ t('coordinator.title') }}</h1>
        <button
          type="button"
          class="flex items-center gap-2 rounded-lg bg-primary-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
          @click="openAddModal"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          {{ t('coordinator.addStudent') }}
        </button>
      </div>

      <div class="mb-6 flex flex-wrap items-center gap-3 rounded-xl border border-primary/20 bg-dark-2 p-3">
        <div class="min-w-[14rem] flex-1">
          <SearchInput v-model="studentSearch" :placeholder="t('coordinator.filters.search')" />
        </div>
        <div v-if="academicYears.length >= 1" class="w-52">
          <SearchableSelect
            v-model="selectedAcademicYear"
            :searchable="false"
            :options="academicYearFilterOptions"
          />
        </div>
        <div class="w-52">
          <SearchableSelect
            v-model="selectedPartnerInstitution"
            :searchable="true"
            :placeholder="t('coordinator.filters.institution')"
            :options="partnerInstitutionOptions"
          />
        </div>
      </div>

      <div v-if="studentList.isPending.value" class="space-y-4">
        <div v-for="i in 3" :key="i" class="animate-pulse rounded-xl border border-primary/20 bg-dark-2 p-5">
          <div class="h-5 w-48 rounded bg-primary/20"></div>
          <div class="mt-3 h-4 w-72 rounded bg-primary/20"></div>
        </div>
      </div>

      <ErrorAlert v-else-if="studentList.error.value" :error="studentList.error.value" @retry="studentList.refetch()" />

      <div v-else-if="students.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center">
        <svg class="mx-auto h-12 w-12 text-light/60" viewBox="0 0 24 24" fill="none">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
          <circle cx="9" cy="7" r="4" stroke="currentColor" stroke-width="1.5" />
        </svg>
        <p class="mt-3 text-light/60">{{ t('coordinator.noStudents') }}</p>
      </div>

      <!-- Student table -->
      <div v-else class="overflow-x-auto rounded-xl border border-primary/20 bg-dark-2">
        <div class="min-w-[860px]">
          <!-- Header row -->
          <div class="coord-row-grid gap-4 border-b border-primary/20 px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-light/40">
            <SortableHeader :label="t('coordinator.table.student')" sort-key="name" :active-key="studentList.sortKey.value" :dir="studentList.sortDir.value" @sort="studentList.toggleSort" />
            <span>{{ t('coordinator.table.exchange') }}</span>
            <span class="text-center">{{ t('coordinator.table.period') }}</span>
            <span class="text-center">{{ t('coordinator.table.learningAgreement') }}</span>
            <span></span>
            <span></span>
          </div>

          <div class="divide-y divide-primary/10">
            <StudentRow
              v-for="student in students"
              :key="student.id"
              :student="student"
              :actions-open="actionsMenu.openId.value === student.id"
              @open-exchanges="exchangesMenu.toggle(student.id, $event)"
              @open-actions="actionsMenu.toggle(student.id, $event)"
              @create-exchange="openCreateExchange(student.id)"
              @copy-link="copyAccessLink"
              @navigate="exchangesMenu.close()"
            />
          </div>
        </div>
      </div>

      <!-- Student pagination -->
      <Pagination
        :page="studentList.page.value"
        :total-pages="studentList.totalPages.value"
        :total="studentList.totalCount.value"
        :per-page="studentList.pageSize"
        @update:page="studentList.page.value = $event"
      />

      <ExchangeSwitcherMenu
        v-if="openMenuStudent"
        :student="openMenuStudent"
        :position="exchangesMenu.position.value"
        @view="viewExchange"
        @copy-link="copyAccessLink"
        @create-exchange="openCreateExchange(openMenuStudent.id)"
        @close="exchangesMenu.close()"
      />
      <StudentActionsMenu
        v-if="actionsMenuStudent"
        :student="actionsMenuStudent"
        :position="actionsMenu.position.value"
        :deleting="deletingStudentId === actionsMenuStudent.id"
        @edit="openEditStudent(actionsMenuStudent)"
        @create-exchange="openCreateExchange(actionsMenuStudent.id)"
        @delete="deleteStudent(actionsMenuStudent)"
      />
    </section>

    <!-- Add/edit student modal -->
    <StudentFormModal
      v-if="showStudentModal"
      :mode="studentModalMode"
      :institutions="institutions"
      :student="editingStudent ?? undefined"
      @close="showStudentModal = false"
      @saved="onStudentSaved"
    />

    <!-- Create exchange modal -->
    <CreateExchangeModal
      v-if="showCreateExchangeModal"
      :target-student-id="createExchangeTargetStudentId"
      @close="showCreateExchangeModal = false"
      @created="onExchangeCreated"
    />
  </main>
</template>
