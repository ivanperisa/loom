<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import type {
  PartnerInstitutionAdminResponse,
  PartnerCourseResponse,
} from '@/types/institution.types'
import SearchInput from '@/components/common/SearchInput.vue'
import PartnerCourseFormModal from '@/components/common/PartnerCourseFormModal.vue'
import SearchableSelect, { type SelectOption } from '@/components/common/SearchableSelect.vue'
import { ISO_COUNTRIES } from '@/constants/countries'
import { useConfirm } from '@/composables/useConfirm'
import { nWord } from '@/utils/plural'

const { t, locale } = useI18n()
const { confirm } = useConfirm()

const INST_PER_PAGE = 10
const COURSE_PER_PAGE = 20

const institutions = ref<PartnerInstitutionAdminResponse[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const institutionSearch = ref('')
const institutionPage = ref(1)
const courseSearch = ref<Record<string, string>>({})
const coursePage = ref<Record<string, number>>({})

const filteredInstitutions = computed(() => {
  const q = institutionSearch.value.trim().toLowerCase()
  if (!q) return institutions.value
  return institutions.value.filter(i =>
    i.name.toLowerCase().includes(q) ||
    i.nameHr?.toLowerCase().includes(q) ||
    i.country.toLowerCase().includes(q) ||
    i.city?.toLowerCase().includes(q) ||
    i.erasmusCode?.toLowerCase().includes(q)
  )
})

const totalInstPages = computed(() => Math.max(1, Math.ceil(filteredInstitutions.value.length / INST_PER_PAGE)))

const pagedInstitutions = computed(() => {
  const p = Math.min(institutionPage.value, totalInstPages.value)
  return filteredInstitutions.value.slice((p - 1) * INST_PER_PAGE, p * INST_PER_PAGE)
})

function onInstSearch() { institutionPage.value = 1 }

function filteredCourses(institutionId: string) {
  const courses = loadedCourses.value[institutionId] ?? []
  const q = (courseSearch.value[institutionId] ?? '').trim().toLowerCase()
  if (!q) return courses
  return courses.filter(c =>
    c.code.toLowerCase().includes(q) ||
    c.name.toLowerCase().includes(q) ||
    c.nameHr?.toLowerCase().includes(q)
  )
}

function pagedCourses(institutionId: string) {
  const all = filteredCourses(institutionId)
  const page = coursePage.value[institutionId] ?? 1
  return all.slice((page - 1) * COURSE_PER_PAGE, page * COURSE_PER_PAGE)
}

function totalCoursePages(institutionId: string) {
  return Math.max(1, Math.ceil(filteredCourses(institutionId).length / COURSE_PER_PAGE))
}

function onCourseSearch(institutionId: string) {
  coursePage.value[institutionId] = 1
}

const expandedInstitutions = ref<Set<string>>(new Set())
const loadedCourses = ref<Record<string, PartnerCourseResponse[]>>({})
const loadingCourses = ref<Set<string>>(new Set())

const showAddInstitution = ref(false)
const addingInstitution = ref(false)
const institutionForm = ref({ name: '', nameHr: '', country: '', city: '', erasmusCode: '' })

const courseModal = ref<{ institutionId: string; institutionName: string; mode: 'create' | 'edit'; course?: PartnerCourseResponse; initialName?: string } | null>(null)
const savingCourse = ref(false)
const courseError = ref<string | null>(null)

const mergeModal = ref<{ institutionId: string; courses: PartnerCourseResponse[]; primaryId: string } | null>(null)
const merging = ref(false)

const mergeSelectInstitutionId = ref<string | null>(null)
const selectedForMerge = ref<Set<string>>(new Set())

function startMergeSelection(institutionId: string) {
  mergeSelectInstitutionId.value = institutionId
  selectedForMerge.value = new Set()
}

function cancelMergeSelection() {
  mergeSelectInstitutionId.value = null
  selectedForMerge.value = new Set()
}

function toggleCourseForMerge(courseId: string) {
  const set = selectedForMerge.value
  if (set.has(courseId)) set.delete(courseId)
  else set.add(courseId)
  selectedForMerge.value = new Set(set)
}

function openMergeModalFromSelection(institutionId: string) {
  const courses = (loadedCourses.value[institutionId] ?? []).filter(c => selectedForMerge.value.has(c.id))
  if (courses.length < 2) return
  mergeModal.value = { institutionId, courses, primaryId: courses[0]!.id }
  cancelMergeSelection()
}

const deletingInstitution = ref<string | null>(null)
const deletingCourse = ref<string | null>(null)

const countryOptions: SelectOption[] = ISO_COUNTRIES.map(c => ({ value: c, label: c }))

onMounted(loadInstitutions)

async function loadInstitutions() {
  loading.value = true
  error.value = null
  try {
    const res = await institutionService.getPartnerInstitutions()
    institutions.value = res.data
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    loading.value = false
  }
}

async function toggleInstitution(inst: PartnerInstitutionAdminResponse) {
  if (expandedInstitutions.value.has(inst.id)) { expandedInstitutions.value.delete(inst.id); return }
  expandedInstitutions.value.add(inst.id)
  if (!loadedCourses.value[inst.id]) {
    loadingCourses.value.add(inst.id)
    try {
      const res = await institutionService.getPartnerCoursesByInstitution(inst.id)
      loadedCourses.value[inst.id] = res.data
    } finally {
      loadingCourses.value.delete(inst.id)
    }
  }
}

function resetInstitutionForm() {
  institutionForm.value = { name: '', nameHr: '', country: '', city: '', erasmusCode: '' }
}

async function addInstitution() {
  const f = institutionForm.value
  if (!f.name.trim() || !f.country.trim()) return
  addingInstitution.value = true
  error.value = null
  try {
    const res = await institutionService.createPartnerInstitution({
      name: f.name.trim(), nameHr: f.nameHr.trim() || f.name.trim(),
      country: f.country.trim(), city: f.city.trim() || undefined,
      erasmusCode: f.erasmusCode.trim() || undefined,
    })
    institutions.value.push(res.data)
    institutions.value.sort((a, b) => a.country.localeCompare(b.country) || a.name.localeCompare(b.name))
    showAddInstitution.value = false
    resetInstitutionForm()
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    addingInstitution.value = false
  }
}

async function deleteInstitution(id: string) {
  if (!await confirm({ title: t('admin.institutions.deleteConfirm') })) return
  deletingInstitution.value = id
  error.value = null
  try {
    await institutionService.deletePartnerInstitution(id)
    institutions.value = institutions.value.filter(i => i.id !== id)
  } catch (e: unknown) {
    const err = e as { response?: { status?: number } }
    error.value = err.response?.status === 409 ? t('admin.institutions.hasExchanges') : t('admin.institutions.saveError')
  } finally {
    deletingInstitution.value = null
  }
}

function openCourseModal(inst: PartnerInstitutionAdminResponse, initialName?: string) {
  courseError.value = null
  courseModal.value = { institutionId: inst.id, institutionName: inst.name, mode: 'create', initialName }
  if (!expandedInstitutions.value.has(inst.id)) toggleInstitution(inst)
}

function openEditCourseModal(inst: PartnerInstitutionAdminResponse, course: PartnerCourseResponse) {
  courseError.value = null
  courseModal.value = { institutionId: inst.id, institutionName: inst.name, mode: 'edit', course }
}

async function submitCourse(payload: {
  code: string; name: string; nameHr?: string; ects: number; semester: string; level: string
  lecturesH?: number; auditoryH?: number; labH?: number
}) {
  if (!courseModal.value) return
  savingCourse.value = true
  courseError.value = null
  const iid = courseModal.value.institutionId
  try {
    if (courseModal.value.mode === 'edit' && courseModal.value.course) {
      const courseId = courseModal.value.course.id
      const res = await institutionService.updatePartnerCourse(courseId, payload)
      const list = loadedCourses.value[iid]
      if (list) {
        const idx = list.findIndex(c => c.id === courseId)
        if (idx !== -1) list[idx] = res.data
      }
    } else {
      const res = await institutionService.createPartnerCourseByInstitution(iid, payload)
      if (!loadedCourses.value[iid]) loadedCourses.value[iid] = []
      loadedCourses.value[iid].push(res.data)
      const inst = institutions.value.find(i => i.id === iid)
      if (inst) inst.courseCount++
    }
    courseModal.value = null
  } catch (e: unknown) {
    const err = e as { response?: { status?: number } }
    courseError.value = err.response?.status === 409 ? t('admin.institutions.duplicateCourseCode') : t('admin.institutions.saveError')
  } finally {
    savingCourse.value = false
  }
}

async function deleteCourse(institutionId: string, courseId: string) {
  if (!await confirm({ title: t('admin.institutions.deleteCourseConfirm') })) return
  deletingCourse.value = courseId
  error.value = null
  try {
    await institutionService.deletePartnerCourse(courseId)
    if (loadedCourses.value[institutionId])
      loadedCourses.value[institutionId] = loadedCourses.value[institutionId].filter(c => c.id !== courseId)
    const inst = institutions.value.find(i => i.id === institutionId)
    if (inst) inst.courseCount--
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    deletingCourse.value = null
  }
}

async function submitMerge() {
  if (!mergeModal.value) return
  const { institutionId, courses, primaryId } = mergeModal.value
  const duplicateIds = courses.filter(c => c.id !== primaryId).map(c => c.id)
  merging.value = true
  error.value = null
  try {
    await institutionService.mergePartnerCourses(primaryId, duplicateIds)
    const list = loadedCourses.value[institutionId]
    if (list) loadedCourses.value[institutionId] = list.filter(c => c.id === primaryId || !duplicateIds.includes(c.id))
    const inst = institutions.value.find(i => i.id === institutionId)
    if (inst) inst.courseCount -= duplicateIds.length
    mergeModal.value = null
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    merging.value = false
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
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h2 class="text-xl font-semibold text-light">{{ t('admin.institutions.title') }}</h2>
      <button
        type="button"
        class="rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
        @click="showAddInstitution = !showAddInstitution"
      >
        {{ t('admin.institutions.addButton') }}
      </button>
    </div>

    <p v-if="error" class="rounded-xl border border-red-400/40 bg-red-500/10 px-4 py-3 text-sm text-red-300">
      {{ error }}
    </p>

    <div v-if="showAddInstitution" class="rounded-xl border border-primary/20 bg-dark-2 p-5">
      <h3 class="mb-4 text-sm font-semibold text-primary-light">{{ t('admin.institutions.addTitle') }}</h3>
      <div class="grid grid-cols-2 gap-3 sm:grid-cols-3">
        <div>
          <label class="mb-1 block text-xs text-light/60">{{ t('admin.institutions.name') }} *</label>
          <input v-model="institutionForm.name" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
        </div>
        <div>
          <label class="mb-1 block text-xs text-light/60">{{ t('admin.institutions.nameHr') }}</label>
          <input v-model="institutionForm.nameHr" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
        </div>
        <div>
          <label class="mb-1 block text-xs text-light/60">{{ t('admin.institutions.country') }} *</label>
          <SearchableSelect
            v-model="institutionForm.country"
            :options="countryOptions"
            :placeholder="t('admin.institutions.countryPlaceholder')"
            :search-placeholder="t('admin.institutions.countryPlaceholder')"
            :no-results-label="t('admin.institutions.noResults')"
          />
        </div>
        <div>
          <label class="mb-1 block text-xs text-light/60">{{ t('admin.institutions.city') }} <span class="text-light/30">({{ t('admin.institutions.optional') }})</span></label>
          <input v-model="institutionForm.city" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
        </div>
        <div>
          <label class="mb-1 block text-xs text-light/60">{{ t('admin.institutions.erasmusCode') }} <span class="text-light/30">({{ t('admin.institutions.optional') }})</span></label>
          <input v-model="institutionForm.erasmusCode" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
        </div>
      </div>
      <div class="mt-4 flex gap-2">
        <button
          type="button"
          class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
          :disabled="addingInstitution || !institutionForm.name.trim() || !institutionForm.country.trim()"
          @click="addInstitution"
        >
          {{ addingInstitution ? t('common.loading') : t('admin.institutions.save') }}
        </button>
        <button type="button" class="rounded-lg border border-white/10 px-4 py-2 text-sm text-light/60 transition hover:text-light" @click="showAddInstitution = false; resetInstitutionForm()">
          {{ t('admin.institutions.cancel') }}
        </button>
      </div>
    </div>

    <SearchInput
      v-model="institutionSearch"
      :placeholder="t('admin.institutions.searchInstitutions')"
      @update:model-value="onInstSearch"
    />

    <div v-if="loading" class="space-y-3">
      <div v-for="i in 4" :key="i" class="h-16 animate-pulse rounded-xl bg-dark-2"></div>
    </div>

    <div v-else-if="filteredInstitutions.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-6 text-center text-light/60">
      {{ institutionSearch ? t('admin.institutions.noResults') : t('admin.institutions.empty') }}
    </div>

    <!-- Institutions list -->
    <div v-else class="space-y-3">
      <div v-for="inst in pagedInstitutions" :key="inst.id" class="rounded-xl border border-primary/20 bg-dark-2">

        <!-- Institution header -->
        <div class="flex items-center gap-3 px-5 py-4">
          <button type="button" class="flex flex-1 items-center gap-3 text-left" @click="toggleInstitution(inst)">
            <svg class="h-4 w-4 flex-shrink-0 text-light/30 transition-transform" :class="expandedInstitutions.has(inst.id) ? 'rotate-90' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
            <div class="min-w-0">
              <div class="flex flex-wrap items-baseline gap-x-2">
                <p class="font-semibold text-light">{{ inst.name }}</p>
                <span v-if="inst.erasmusCode" class="rounded border border-primary/30 bg-primary/10 px-1.5 py-0.5 font-mono text-xs text-primary-light">{{ inst.erasmusCode }}</span>
              </div>
              <p v-if="inst.nameHr && inst.nameHr !== inst.name" class="text-xs text-light/40">{{ inst.nameHr }}</p>
              <p class="mt-0.5 flex items-center gap-1 text-xs text-light/40">
                <span class="flex items-center gap-1">
                  <svg class="h-3 w-3 text-light/30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                  {{ inst.country }}<template v-if="inst.city">, {{ inst.city }}</template>
                </span>
              </p>
            </div>
          </button>
          <div class="flex flex-shrink-0 items-center gap-2">
            <button type="button" class="rounded-lg border border-primary/30 px-3 py-1.5 text-xs font-medium text-primary-light transition hover:bg-primary/10 disabled:opacity-40" :disabled="!!deletingInstitution" @click="openCourseModal(inst, courseSearch[inst.id] || undefined)">
              + {{ t('admin.institutions.addCourse') }}
            </button>
            <button type="button" class="flex h-7 w-7 items-center justify-center rounded-lg border border-red-400/20 text-red-400/60 transition hover:border-red-400/50 hover:bg-red-500/10 hover:text-red-300 disabled:opacity-40" :disabled="deletingInstitution === inst.id" :title="t('admin.institutions.deleteInstitution')" @click="deleteInstitution(inst.id)">
              <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        <!-- Courses section -->
        <div v-if="expandedInstitutions.has(inst.id)" class="border-t border-white/5 px-5 pb-4 pt-3">
          <div v-if="loadingCourses.has(inst.id)" class="space-y-1.5">
            <div v-for="i in 3" :key="i" class="h-7 animate-pulse rounded bg-white/5"></div>
          </div>
          <template v-else>
            <!-- Merge selection toolbar -->
            <div class="mb-2 flex items-center justify-between gap-2">
              <SearchInput
                v-model="courseSearch[inst.id]"
                :placeholder="t('admin.institutions.searchCourses')"
                class="flex-1"
                @update:model-value="onCourseSearch(inst.id)"
              />
              <button
                v-if="mergeSelectInstitutionId !== inst.id"
                type="button"
                class="shrink-0 rounded-lg border border-primary/30 px-3 py-1.5 text-xs font-medium text-primary-light transition hover:bg-primary/10"
                @click="startMergeSelection(inst.id)"
              >
                {{ t('admin.institutions.mergeCourses') }}
              </button>
              <template v-else>
                <button
                  type="button"
                  class="shrink-0 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-40"
                  :disabled="selectedForMerge.size < 2"
                  @click="openMergeModalFromSelection(inst.id)"
                >
                  {{ t('admin.institutions.mergeSelected', { count: selectedForMerge.size }) }}
                </button>
                <button
                  type="button"
                  class="shrink-0 rounded-lg border border-white/10 px-3 py-1.5 text-xs text-light/60 transition hover:text-light"
                  @click="cancelMergeSelection"
                >
                  {{ t('admin.institutions.cancel') }}
                </button>
              </template>
            </div>

            <p v-if="filteredCourses(inst.id).length === 0" class="text-xs text-light/30">
              {{ courseSearch[inst.id] ? t('admin.institutions.noResults') : t('admin.institutions.noCourses') }}
            </p>
            <div v-else>
              <div class="divide-y divide-white/5">
                <div
                  v-for="course in pagedCourses(inst.id)"
                  :key="course.id"
                  class="flex items-center justify-between py-1.5"
                >
                  <div class="flex min-w-0 items-center gap-3">
                    <input
                      v-if="mergeSelectInstitutionId === inst.id"
                      type="checkbox"
                      class="shrink-0 accent-primary"
                      :checked="selectedForMerge.has(course.id)"
                      @change="toggleCourseForMerge(course.id)"
                    />
                    <span class="w-16 flex-shrink-0 font-mono text-xs text-light/50">{{ course.code }}</span>
                    <div class="min-w-0">
                      <span class="text-xs text-light">{{ course.name }}</span>
                      <span v-if="course.nameHr" class="ml-2 text-xs text-light/40">/ {{ course.nameHr }}</span>
                    </div>
                  </div>
                  <div class="flex flex-shrink-0 items-center gap-3 text-xs text-light/40">
                    <span class="w-28 flex-shrink-0 truncate rounded bg-white/5 px-2 py-0.5 text-left text-xs text-light/40">{{ semesterLabel(course.semester) }}</span>
                    <span class="w-28 flex-shrink-0 truncate rounded bg-white/5 px-2 py-0.5 text-left text-xs text-light/40">{{ levelLabel(course.level) }}</span>
                    <span class="font-medium text-light/60">{{ course.ects }} ECTS</span>
                    <button
                      type="button"
                      class="flex h-6 w-6 items-center justify-center rounded text-light/40 transition hover:bg-primary/10 hover:text-primary-light disabled:opacity-40"
                      :title="t('admin.institutions.editCourse')"
                      @click="openEditCourseModal(inst, course)"
                    >
                      <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                      </svg>
                    </button>
                    <button
                      type="button"
                      class="flex h-6 w-6 items-center justify-center rounded text-red-400/50 transition hover:bg-red-500/10 hover:text-red-300 disabled:opacity-40"
                      :disabled="deletingCourse === course.id"
                      :title="t('admin.institutions.deleteCourse')"
                      @click="deleteCourse(inst.id, course.id)"
                    >
                      <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                </div>
              </div>

              <!-- Course pagination -->
              <div v-if="totalCoursePages(inst.id) > 1" class="mt-2 flex items-center justify-end gap-2 text-xs text-light/40">
                <button
                  type="button"
                  class="rounded px-2 py-0.5 transition hover:text-light disabled:opacity-30"
                  :disabled="(coursePage[inst.id] ?? 1) <= 1"
                  @click="coursePage[inst.id] = (coursePage[inst.id] ?? 1) - 1"
                >Prev</button>
                <span>{{ coursePage[inst.id] ?? 1 }} / {{ totalCoursePages(inst.id) }}</span>
                <button
                  type="button"
                  class="rounded px-2 py-0.5 transition hover:text-light disabled:opacity-30"
                  :disabled="(coursePage[inst.id] ?? 1) >= totalCoursePages(inst.id)"
                  @click="coursePage[inst.id] = (coursePage[inst.id] ?? 1) + 1"
                >Next</button>
              </div>
            </div>
          </template>
        </div>
    </div>

    <!-- Institution pagination -->
    <div v-if="totalInstPages > 1" class="flex items-center justify-center gap-3 text-sm text-light/50">
      <button
        type="button"
        class="rounded-lg border border-white/10 px-3 py-1.5 transition hover:text-light disabled:opacity-30"
        :disabled="institutionPage <= 1"
        @click="institutionPage--"
      >â†</button>
      <span>{{ institutionPage }} / {{ totalInstPages }}</span>
      <button
        type="button"
        class="rounded-lg border border-white/10 px-3 py-1.5 transition hover:text-light disabled:opacity-30"
        :disabled="institutionPage >= totalInstPages"
        @click="institutionPage++"
      >â†’</button>
    </div>
  </div>
  </div>

  <!-- Add/Edit Course Modal -->
  <PartnerCourseFormModal
    v-if="courseModal"
    :mode="courseModal.mode"
    :institution-name="courseModal.institutionName"
    :course="courseModal.course"
    :initial-name="courseModal.initialName"
    :saving="savingCourse"
    :error="courseError"
    @submit="submitCourse"
    @close="courseModal = null"
  />

  <!-- Merge Courses Modal -->
  <Teleport to="body">
    <div
      v-if="mergeModal"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 px-4"
      @mousedown.self="mergeModal = null"
    >
      <div class="w-full max-w-md rounded-2xl border border-primary/20 bg-dark-2 shadow-2xl">
        <div class="flex items-center justify-between border-b border-primary/20 px-6 py-4">
          <div>
            <h3 class="font-semibold text-light">{{ t('admin.institutions.mergeCourses') }}</h3>
            <p class="mt-0.5 text-xs text-light/40">{{ t('admin.institutions.mergeDescription') }}</p>
          </div>
          <button type="button" class="text-light/40 transition hover:text-white" @click="mergeModal = null">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
        <div class="space-y-2 px-6 py-5">
          <label
            v-for="course in mergeModal.courses"
            :key="course.id"
            class="flex cursor-pointer items-center gap-3 rounded-lg border border-white/10 bg-dark px-3 py-2.5 transition hover:border-primary/30"
            :class="mergeModal.primaryId === course.id ? 'border-primary bg-primary/10' : ''"
          >
            <input type="radio" :value="course.id" v-model="mergeModal.primaryId" class="accent-primary" />
            <div class="min-w-0 flex-1">
              <span class="font-mono text-xs text-light/50">{{ course.code }}</span>
              <span class="ml-2 text-sm text-light">{{ course.name }}</span>
              <span v-if="course.nameHr" class="ml-1 text-xs text-light/40">/ {{ course.nameHr }}</span>
            </div>
            <span
              class="shrink-0 rounded px-2 py-0.5 text-[11px] font-semibold"
              :class="mergeModal.primaryId === course.id ? 'bg-primary/20 text-primary-light' : 'bg-red-500/10 text-red-300/70'"
            >{{ mergeModal.primaryId === course.id ? t('admin.institutions.mergeKeeps') : t('admin.institutions.mergeDeletes') }}</span>
          </label>
          <p class="pt-1 text-xs text-light/40">{{ t('admin.institutions.mergeHint') }}</p>
        </div>
        <div class="flex justify-end gap-2 border-t border-primary/20 px-6 py-4">
          <button type="button" class="rounded-lg border border-white/10 px-4 py-2 text-sm text-light/60 transition hover:text-light" @click="mergeModal = null">{{ t('admin.institutions.cancel') }}</button>
          <button
            type="button"
            class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
            :disabled="merging"
            @click="submitMerge"
          >
            {{ merging ? t('common.loading') : t('admin.institutions.mergeCourses') }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>

</template>

