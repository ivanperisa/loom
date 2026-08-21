<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { onBeforeRouteUpdate, useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { coordinatorService } from '@/services/coordinator.service'
import { institutionService } from '@/services/institution.service'
import type { CoordinatorStudentResponse } from '@/types/coordinator.types'
import type { ExchangeSummaryResponse } from '@/types/exchange.types'
import type { InstitutionResponse } from '@/types/institution.types'
import { statusColorClass, statusDotClass } from '@/utils/statusColors'
import { buildAccessLink } from '@/utils/accessLink'
import CreateExchangeModal from '@/components/exchange/CreateExchangeModal.vue'
import StudentFormModal from '@/components/coordinator/StudentFormModal.vue'
import SearchableSelect from '@/components/common/SearchableSelect.vue'
import SearchInput from '@/components/common/SearchInput.vue'
import SortableHeader from '@/components/common/SortableHeader.vue'
import Pagination from '@/components/common/Pagination.vue'
import { useNotification } from '@/composables/useNotification'
import { useConfirm } from '@/composables/useConfirm'
import { useDebouncedRef } from '@/composables/useDebouncedRef'
import type { SortDir } from '@/composables/useSortable'
import { useQuerySync } from '@/composables/useQuerySync'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const { notifySuccess, notifyError } = useNotification()
const { confirm } = useConfirm()

const students = ref<CoordinatorStudentResponse[]>([])
const studentPage = ref(1)
const studentSortDir = ref<SortDir>('asc')
const studentsTotalCount = ref(0)
const STUDENTS_PER_PAGE = 25
const totalStudentPages = computed(() => Math.ceil(studentsTotalCount.value / STUDENTS_PER_PAGE))
const exchanges = ref<ExchangeSummaryResponse[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const openMenuId = ref<string | null>(null)
const menuPos = ref({ top: 0, left: 0 })
const MENU_WIDTH = 380

const actionsMenuId = ref<string | null>(null)
const actionsMenuPos = ref({ top: 0, left: 0 })
const ACTIONS_MENU_WIDTH = 200

const showStudentModal = ref(false)
const studentModalMode = ref<'create' | 'edit'>('create')
const editingStudent = ref<CoordinatorStudentResponse | null>(null)
const institutions = ref<InstitutionResponse[]>([])
const deletingStudentId = ref<string | null>(null)

// Create exchange modal
const showCreateExchangeModal = ref(false)
const createExchangeTargetStudentId = ref<string | null>(null)

const selectedAcademicYear = ref<string | null>(null)
const selectedPartnerInstitution = ref<string | null>(null)
const studentSearch = ref<string>(typeof route.query.q === 'string' ? route.query.q : '')
const debouncedStudentSearch = useDebouncedRef(studentSearch)

useQuerySync({
  year: selectedAcademicYear,
  institution: selectedPartnerInstitution,
  q: debouncedStudentSearch,
})

const academicYears = computed(() => {
  const years = new Set(exchanges.value.map((ex) => ex.academicYear))
  return Array.from(years).sort().reverse()
})

const academicYearFilterOptions = computed(() => [
  { value: null, label: t('home.allYears') },
  ...academicYears.value.map((year) => ({ value: year, label: year })),
])

const partnerInstitutionOptions = computed(() => {
  const names = new Set(exchanges.value.map((ex) => ex.partnerInstitutionName))
  return [
    { value: null, label: t('coordinator.filters.allInstitutions') },
    ...Array.from(names)
      .sort((a, b) => a.localeCompare(b))
      .map((name) => ({ value: name, label: name })),
  ]
})

const filteredExchanges = computed(() =>
  exchanges.value.filter(
    (ex) =>
      (!selectedAcademicYear.value || ex.academicYear === selectedAcademicYear.value) &&
      (!selectedPartnerInstitution.value || ex.partnerInstitutionName === selectedPartnerInstitution.value),
  ),
)

const exchangesByStudent = computed(() => {
  const map = new Map<string, ExchangeSummaryResponse[]>()
  for (const ex of filteredExchanges.value) {
    const list = map.get(ex.studentId) ?? []
    list.push(ex)
    map.set(ex.studentId, list)
  }
  return map
})

const primaryExchangeByStudent = computed(() => {
  const map = new Map<string, ExchangeSummaryResponse>()
  for (const [studentId, list] of exchangesByStudent.value) {
    if (list[0]) map.set(studentId, list[0])
  }
  return map
})

function extraExchangeCount(studentId: string): number {
  return Math.max((exchangesByStudent.value.get(studentId)?.length ?? 0) - 1, 0)
}

const openMenuExchanges = computed(() =>
  openMenuId.value ? (exchangesByStudent.value.get(openMenuId.value) ?? []) : [],
)
const openMenuStudent = computed(() => students.value.find((s) => s.id === openMenuId.value) ?? null)
const actionsMenuStudent = computed(
  () => students.value.find((s) => s.id === actionsMenuId.value) ?? null,
)

// Text search (name/jmbag) is server-side and already reflected in `students` (the current page).
// The academic-year/institution filters are local: the server doesn't know about exchanges,
// so they narrow the current page further using the (unpaged) exchange data.
const filteredStudents = computed(() => {
  if (!selectedAcademicYear.value && !selectedPartnerInstitution.value) return students.value
  return students.value.filter((s) => exchangesByStudent.value.has(s.id))
})

// Only the student column is sortable: the server orders by name so this stays correct
// across pages. The exchange/period/status columns come from a separate, unpaged exchange
// list and can't be sorted coherently against a paginated student list, so they're plain
// headers (see final-review-fix4-report.md for the investigation).
function toggleStudentSort() {
  studentSortDir.value = studentSortDir.value === 'asc' ? 'desc' : 'asc'
  studentPage.value = 1
  fetchStudents()
}

async function fetchData() {
  closeMenu()
  loading.value = true
  error.value = null
  try {
    const [studentsRes, exchangesRes, institutionsRes] = await Promise.allSettled([
      coordinatorService.getStudents({ page: studentPage.value, pageSize: STUDENTS_PER_PAGE, search: debouncedStudentSearch.value, sortDir: studentSortDir.value }),
      coordinatorService.getStudentsExchanges(),
      institutionService.getHomeInstitutions(),
    ])
    if (studentsRes.status === 'fulfilled') {
      students.value = studentsRes.value.data.items
      studentsTotalCount.value = studentsRes.value.data.totalCount
    }
    if (exchangesRes.status === 'fulfilled') exchanges.value = exchangesRes.value.data
    if (institutionsRes.status === 'fulfilled') institutions.value = institutionsRes.value.data
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
}

async function fetchStudents() {
  closeMenu()
  try {
    const res = await coordinatorService.getStudents({ page: studentPage.value, pageSize: STUDENTS_PER_PAGE, search: debouncedStudentSearch.value, sortDir: studentSortDir.value })
    students.value = res.data.items
    studentsTotalCount.value = res.data.totalCount
  } catch {
    error.value = t('common.error')
  }
}

onMounted(fetchData)

onBeforeRouteUpdate((to) => {
  const q = typeof to.query.q === 'string' ? to.query.q : ''
  if (q !== studentSearch.value) studentSearch.value = q
})

watch([studentPage, debouncedStudentSearch], ([newPage, newSearch], [, oldSearch]) => {
  if (newSearch !== oldSearch && newPage !== 1) {
    studentPage.value = 1
    return
  }
  fetchStudents()
})

function closeMenu() {
  openMenuId.value = null
}

function closeActionsMenu() {
  actionsMenuId.value = null
}

function toggleMenu(studentId: string, event: MouseEvent) {
  if (openMenuId.value === studentId) {
    closeMenu()
    return
  }
  const rect = (event.currentTarget as HTMLElement).getBoundingClientRect()
  menuPos.value = {
    top: rect.bottom + 6,
    left: Math.min(rect.left, window.innerWidth - MENU_WIDTH - 12),
  }
  openMenuId.value = studentId
}

function toggleActionsMenu(studentId: string, event: MouseEvent) {
  if (actionsMenuId.value === studentId) {
    closeActionsMenu()
    return
  }
  const rect = (event.currentTarget as HTMLElement).getBoundingClientRect()
  actionsMenuPos.value = {
    top: rect.bottom + 6,
    left: Math.min(rect.left, window.innerWidth - ACTIONS_MENU_WIDTH - 12),
  }
  actionsMenuId.value = studentId
}

function handleOutsideClick(e: MouseEvent) {
  if (!(e.target as HTMLElement).closest('[data-menu-anchor]')) {
    closeMenu()
    closeActionsMenu()
  }
}

onMounted(() => document.addEventListener('click', handleOutsideClick))
onUnmounted(() => document.removeEventListener('click', handleOutsideClick))

watch([debouncedStudentSearch, selectedAcademicYear, selectedPartnerInstitution], () => {
  closeMenu()
  closeActionsMenu()
})

function rowAriaLabel(student: CoordinatorStudentResponse, primary: ExchangeSummaryResponse): string {
  return `${student.name} — ${primary.partnerInstitutionName}`
}

function viewExchange(exchangeGuid: string) {
  closeMenu()
  router.push(`/exchange/${exchangeGuid}`)
}

async function copyAccessLink(exchangeGuid: string) {
  closeMenu()
  await navigator.clipboard.writeText(buildAccessLink(exchangeGuid))
  notifySuccess(t('exchangeAccess.linkCopied'))
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

async function onStudentSaved(student: CoordinatorStudentResponse) {
  if (studentModalMode.value === 'edit') {
    const idx = students.value.findIndex((s) => s.id === student.id)
    if (idx !== -1) students.value[idx] = student
  } else {
    await fetchStudents()
  }
  showStudentModal.value = false
}

async function deleteStudent(student: CoordinatorStudentResponse) {
  closeActionsMenu()
  if (!await confirm({ title: t('coordinator.deleteStudentConfirm') })) return
  deletingStudentId.value = student.id
  try {
    await coordinatorService.deleteStudent(student.id)
    await fetchStudents()
    notifySuccess(t('coordinator.deleteStudent'))
  } catch (e: unknown) {
    const err = e as { response?: { status?: number } }
    notifyError(err?.response?.status === 409 ? t('coordinator.deleteStudentHasExchanges') : t('coordinator.deleteStudentError'))
  } finally {
    deletingStudentId.value = null
  }
}

function openCreateExchange(studentId: string) {
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
          class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
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

      <div v-if="loading" class="space-y-4">
        <div v-for="i in 3" :key="i" class="animate-pulse rounded-xl border border-primary/20 bg-dark-2 p-5">
          <div class="h-5 w-48 rounded bg-primary/20"></div>
          <div class="mt-3 h-4 w-72 rounded bg-primary/20"></div>
        </div>
      </div>

      <div v-else-if="error" class="rounded-xl border border-red-400/30 bg-red-900/20 p-8 text-center">
        <p class="text-danger">{{ error }}</p>
      </div>

      <div v-else-if="filteredStudents.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center">
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
            <SortableHeader :label="t('coordinator.table.student')" sort-key="student" active-key="student" :dir="studentSortDir" @sort="toggleStudentSort" />
            <span>{{ t('coordinator.table.exchange') }}</span>
            <span class="text-center">{{ t('coordinator.table.period') }}</span>
            <span class="text-center">{{ t('coordinator.table.learningAgreement') }}</span>
            <span></span>
            <span></span>
          </div>

          <div class="divide-y divide-primary/10">
            <div v-for="student in filteredStudents" :key="student.id" class="group relative" data-menu-anchor>
              <!-- Stretched link: click anywhere on the row to open its primary exchange -->
              <RouterLink
                v-if="primaryExchangeByStudent.get(student.id)"
                :to="`/exchange/${primaryExchangeByStudent.get(student.id)!.guid}`"
                class="absolute inset-0 z-0 rounded-none focus-visible:outline focus-visible:outline-2 focus-visible:outline-primary focus-visible:-outline-offset-2"
                :aria-label="rowAriaLabel(student, primaryExchangeByStudent.get(student.id)!)"
                @click="closeMenu"
              />
              <button
                v-else
                type="button"
                class="absolute inset-0 z-0 text-left focus-visible:outline focus-visible:outline-2 focus-visible:outline-primary focus-visible:-outline-offset-2"
                :aria-label="`${t('coordinator.createExchange')} — ${student.name}`"
                @click="openCreateExchange(student.id)"
              />
              
              <div class="coord-row-grid relative pointer-events-none gap-4 px-4 py-3 transition group-hover:bg-dark">
                <div class="min-w-0">
                  <p class="truncate text-sm font-semibold text-light">{{ student.name }}</p>
                  <!-- Metadata line: JMBAG and the not-yet-claimed state share one row so every
                       student cell is the same height whether or not the badge is present. -->
                  <div class="mt-0.5 flex min-w-0 items-center gap-1.5 text-xs">
                    <span v-if="student.jmbag" class="truncate font-mono text-light/40">{{ student.jmbag }}</span>
                    <span
                      v-if="student.isPlaceholder"
                      class="shrink-0 rounded-full border border-warning/30 bg-warning/10 px-2 py-0.5 text-[11px] font-medium text-warning"
                    >
                      {{ t('coordinator.placeholder') }}
                    </span>
                  </div>
                </div>

                <div class="min-w-0">
                  <template v-if="primaryExchangeByStudent.get(student.id)">
                    <div class="flex flex-wrap items-center gap-2">
                      <span class="truncate text-sm text-light">{{ primaryExchangeByStudent.get(student.id)!.partnerInstitutionName }}</span>
                      <button
                        v-if="extraExchangeCount(student.id) > 0"
                        type="button"
                        class="pointer-events-auto shrink-0 rounded-full bg-fill px-2 py-0.5 text-[11px] font-medium text-light/60 transition hover:bg-primary hover:text-white"
                        @click.stop="toggleMenu(student.id, $event)"
                      >
                        {{ t('coordinator.table.moreExchanges', { n: extraExchangeCount(student.id) }) }}
                      </button>
                    </div>
                    <p class="mt-0.5 truncate text-xs text-light/40">
                      {{ primaryExchangeByStudent.get(student.id)!.homeProgramName
                      }}<span v-if="primaryExchangeByStudent.get(student.id)!.homeProfileName"> &middot; {{ primaryExchangeByStudent.get(student.id)!.homeProfileName }}</span>
                    </p>
                  </template>
                  <span v-else class="text-sm text-light/30">{{ t('coordinator.table.none') }}</span>
                </div>

                <div class="min-w-0 text-center">
                  <template v-if="primaryExchangeByStudent.get(student.id)">
                    <p class="truncate text-sm text-light/70">{{ primaryExchangeByStudent.get(student.id)!.academicYear }}</p>
                    <p class="truncate text-xs text-light/40">{{ t(`exchangeSemester.${primaryExchangeByStudent.get(student.id)!.semesterType}`) }}</p>
                  </template>
                  <span v-else class="text-sm text-light/30">{{ t('coordinator.table.none') }}</span>
                </div>

                <div class="flex justify-center">
                  <span
                    v-if="primaryExchangeByStudent.get(student.id)"
                    class="rounded-full border px-2.5 py-0.5 text-xs font-semibold"
                    :class="statusColorClass[primaryExchangeByStudent.get(student.id)!.learningAgreementStatus]"
                  >
                    {{ t(`documentStatus.${primaryExchangeByStudent.get(student.id)!.learningAgreementStatus}`) }}
                  </span>
                  <span v-else class="text-sm text-light/30">{{ t('coordinator.table.none') }}</span>
                </div>

                <div class="flex items-center justify-self-center gap-0.5">
                  <button
                    v-if="student.isPlaceholder && primaryExchangeByStudent.get(student.id)"
                    type="button"
                    :title="t('exchangeAccess.copyLink')"
                    class="pointer-events-auto flex h-7 w-7 shrink-0 items-center justify-center rounded-lg text-light/40 transition hover:bg-primary/10 hover:text-primary-light"
                    @click.stop="copyAccessLink(primaryExchangeByStudent.get(student.id)!.guid)"
                  >
                    <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                      <path d="M12.586 4.586a2 2 0 112.828 2.828l-3 3a2 2 0 01-2.828 0 1 1 0 00-1.414 1.414 4 4 0 005.656 0l3-3a4 4 0 00-5.656-5.656l-1.5 1.5a1 1 0 101.414 1.414l1.5-1.5z" />
                      <path d="M7.414 15.414a2 2 0 01-2.828-2.828l3-3a2 2 0 012.828 0 1 1 0 001.414-1.414 4 4 0 00-5.656 0l-3 3a4 4 0 105.656 5.656l1.5-1.5a1 1 0 10-1.414-1.414l-1.5 1.5z" />
                    </svg>
                  </button>
                  <span v-else class="h-7 w-7 shrink-0"></span>

                  <a
                    v-if="primaryExchangeByStudent.get(student.id)?.ewpLink"
                    :href="primaryExchangeByStudent.get(student.id)!.ewpLink!"
                    target="_blank"
                    rel="noopener noreferrer"
                    :title="t('exchange.ewpLink')"
                    class="pointer-events-auto flex h-7 w-7 shrink-0 items-center justify-center rounded-lg text-light/40 transition hover:bg-primary/10 hover:text-primary-light"
                    @click.stop
                  >
                    <svg width="12" height="12" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M5 2H2a1 1 0 0 0-1 1v7a1 1 0 0 0 1 1h7a1 1 0 0 0 1-1V7" />
                      <path d="M8 1h3v3" /><line x1="11" y1="1" x2="5" y2="7" />
                    </svg>
                  </a>
                  <span v-else class="h-7 w-7 shrink-0"></span>
                </div>

                <button
                  type="button"
                  class="pointer-events-auto flex h-7 w-7 items-center justify-center justify-self-center rounded-lg text-lg leading-none text-light/40 transition hover:bg-fill hover:text-light"
                  :aria-expanded="actionsMenuId === student.id"
                  aria-haspopup="true"
                  :aria-label="`${t('coordinator.table.actions')} — ${student.name}`"
                  @click.stop="toggleActionsMenu(student.id, $event)"
                >⋯</button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Student pagination -->
      <Pagination
        :page="studentPage"
        :total-pages="totalStudentPages"
        :total="studentsTotalCount"
        :per-page="STUDENTS_PER_PAGE"
        @update:page="studentPage = $event"
      />

      <!-- Exchange switcher menu -->
      <Teleport to="body">
        <div
          v-if="openMenuId"
          data-menu-anchor
          class="fixed z-50 w-[380px] rounded-xl border border-primary/20 bg-dark-2 p-1.5 shadow-2xl shadow-black/50"
          :style="{ top: menuPos.top + 'px', left: menuPos.left + 'px' }"
        >
          <p class="px-2.5 pb-1 pt-1 text-[10px] font-semibold uppercase tracking-wider text-light/40">
            {{ t('coordinator.table.exchange') }}
          </p>

          <div v-if="openMenuExchanges.length === 0" class="px-2.5 py-3 text-center text-xs text-light/40">
            {{ t('coordinator.noExchanges') }}
          </div>
          <div v-else class="space-y-0.5">
            <div
              v-for="ex in openMenuExchanges"
              :key="ex.id"
              class="flex cursor-pointer items-start justify-between gap-2 rounded-lg px-2 py-1.5 transition hover:bg-fill-soft"
              @click="viewExchange(ex.guid)"
            >
              <div class="flex min-w-0 items-start gap-2">
                <span class="mt-0.5 w-3 shrink-0 text-center text-xs font-bold text-primary-light">
                  {{ ex.id === primaryExchangeByStudent.get(openMenuId!)?.id ? '✓' : '' }}
                </span>
                <div class="min-w-0">
                  <p class="text-sm font-medium text-light">{{ ex.partnerInstitutionName }}</p>
                  <p class="mt-0.5 text-xs text-light/40">
                    {{ ex.homeProgramName }}<span v-if="ex.homeProfileName"> &middot; {{ ex.homeProfileName }}</span>
                  </p>
                  <p class="mt-0.5 flex items-center gap-1.5 text-xs text-light/50">
                    <span class="h-1.5 w-1.5 shrink-0 rounded-full" :class="statusDotClass[ex.learningAgreementStatus]"></span>
                    <span class="truncate">
                      {{ ex.academicYear }} &middot; {{ t(`exchangeSemester.${ex.semesterType}`) }}
                      &middot; {{ t(`documentStatus.${ex.learningAgreementStatus}`) }}
                    </span>
                  </p>
                </div>
              </div>
              <div class="flex shrink-0 items-center gap-1">
                <a
                  v-if="ex.ewpLink"
                  :href="ex.ewpLink"
                  target="_blank"
                  rel="noopener noreferrer"
                  :title="t('exchange.ewpLink')"
                  class="flex h-6 w-6 items-center justify-center rounded text-light/40 transition hover:bg-primary/10 hover:text-primary-light"
                  @click.stop="closeMenu"
                >
                  <svg width="11" height="11" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M5 2H2a1 1 0 0 0-1 1v7a1 1 0 0 0 1 1h7a1 1 0 0 0 1-1V7" />
                    <path d="M8 1h3v3" /><line x1="11" y1="1" x2="5" y2="7" />
                  </svg>
                </a>
                <button
                  v-if="openMenuStudent?.isPlaceholder"
                  type="button"
                  :title="t('exchangeAccess.copyLink')"
                  class="flex h-6 w-6 items-center justify-center rounded text-light/40 transition hover:bg-primary/10 hover:text-primary-light"
                  @click.stop="copyAccessLink(ex.guid)"
                >
                  <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                    <path d="M12.586 4.586a2 2 0 112.828 2.828l-3 3a2 2 0 01-2.828 0 1 1 0 00-1.414 1.414 4 4 0 005.656 0l3-3a4 4 0 00-5.656-5.656l-1.5 1.5a1 1 0 101.414 1.414l1.5-1.5z" />
                    <path d="M7.414 15.414a2 2 0 01-2.828-2.828l3-3a2 2 0 012.828 0 1 1 0 001.414-1.414 4 4 0 00-5.656 0l-3 3a4 4 0 105.656 5.656l1.5-1.5a1 1 0 10-1.414-1.414l-1.5 1.5z" />
                  </svg>
                </button>
              </div>
            </div>
          </div>

          <div class="my-1.5 border-t border-primary/20"></div>
          <button
            type="button"
            class="flex w-full items-center gap-2 rounded-lg px-2.5 py-2 text-left text-sm font-semibold text-primary-light transition hover:bg-primary/10"
            @click="openCreateExchange(openMenuId!)"
          >
            <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
            </svg>
            {{ t('coordinator.createExchange') }}
          </button>
        </div>
      </Teleport>

      <!-- Per-row actions menu: teleported for the same reason as the switcher above -->
      <Teleport to="body">
        <div
          v-if="actionsMenuId"
          data-menu-anchor
          class="fixed z-50 w-[200px] space-y-0.5 rounded-xl border border-primary/20 bg-dark-2 p-1.5 shadow-2xl shadow-black/50"
          :style="{ top: actionsMenuPos.top + 'px', left: actionsMenuPos.left + 'px' }"
        >
          <button
            v-if="actionsMenuStudent?.isPlaceholder && actionsMenuStudent?.isMyStudent"
            type="button"
            class="flex w-full items-center gap-2 rounded-lg px-2.5 py-1.5 text-left text-sm font-medium text-light transition hover:bg-fill-soft"
            @click="openEditStudent(actionsMenuStudent!)"
          >
            <svg class="h-3.5 w-3.5 text-light/50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
            {{ t('common.edit') }}
          </button>
          <button
            v-if="actionsMenuStudent?.isMyStudent"
            type="button"
            class="flex w-full items-center gap-2 rounded-lg px-2.5 py-1.5 text-left text-sm font-medium text-primary-light transition hover:bg-primary/10"
            @click="openCreateExchange(actionsMenuId!)"
          >
            <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
            </svg>
            {{ t('coordinator.createExchange') }}
          </button>
          <button
            v-if="actionsMenuStudent?.isPlaceholder && actionsMenuStudent?.isMyStudent"
            type="button"
            class="flex w-full items-center gap-2 rounded-lg px-2.5 py-1.5 text-left text-sm font-medium text-danger transition hover:bg-danger/10 disabled:opacity-50"
            :disabled="deletingStudentId === actionsMenuStudent.id"
            @click="deleteStudent(actionsMenuStudent)"
          >
            <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
            {{ deletingStudentId === actionsMenuStudent.id ? t('common.loading') : t('coordinator.deleteStudent') }}
          </button>
          <p v-if="!actionsMenuStudent?.isMyStudent" class="px-2.5 py-1.5 text-xs text-light/40">
            {{ t('coordinator.table.reassignedNote') }}
          </p>
        </div>
      </Teleport>
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

<style scoped>
.coord-row-grid {
  display: grid;
  grid-template-columns: minmax(140px, 0.9fr) minmax(200px, 1.6fr) 200px 150px 60px 28px;
  align-items: center;
}
</style>
