<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  adminService,
  type PartnerInstitutionAdminResponse,
  type PartnerProgramAdminResponse,
} from '@/services/admin.service'
import { institutionService } from '@/services/institution.service'
import type { PartnerCourseResponse } from '@/types/institution.types'
import SearchInput from '@/components/common/SearchInput.vue'
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
const programSearch = ref<Record<string, string>>({})
const courseSearch = ref<Record<string, string>>({})
const coursePage = ref<Record<string, number>>({})

const filteredInstitutions = computed(() => {
  const q = institutionSearch.value.trim().toLowerCase()
  if (!q) return institutions.value
  return institutions.value.filter(i =>
    i.name.toLowerCase().includes(q) ||
    i.nameEn?.toLowerCase().includes(q) ||
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

function filteredPrograms(inst: PartnerInstitutionAdminResponse) {
  const q = (programSearch.value[inst.id] ?? '').trim().toLowerCase()
  if (!q) return inst.programs
  return inst.programs.filter(p =>
    p.name.toLowerCase().includes(q) ||
    p.nameEn?.toLowerCase().includes(q)
  )
}

function filteredCourses(programId: string) {
  const courses = loadedCourses.value[programId] ?? []
  const q = (courseSearch.value[programId] ?? '').trim().toLowerCase()
  if (!q) return courses
  return courses.filter(c =>
    c.code.toLowerCase().includes(q) ||
    c.nameEn.toLowerCase().includes(q) ||
    c.nameHr?.toLowerCase().includes(q)
  )
}

function pagedCourses(programId: string) {
  const all = filteredCourses(programId)
  const page = coursePage.value[programId] ?? 1
  return all.slice((page - 1) * COURSE_PER_PAGE, page * COURSE_PER_PAGE)
}

function totalCoursePages(programId: string) {
  return Math.max(1, Math.ceil(filteredCourses(programId).length / COURSE_PER_PAGE))
}

function onCourseSearch(programId: string) {
  coursePage.value[programId] = 1
}

const expandedInstitutions = ref<Set<string>>(new Set())
const expandedPrograms = ref<Set<string>>(new Set())
const loadedCourses = ref<Record<string, PartnerCourseResponse[]>>({})
const loadingCourses = ref<Set<string>>(new Set())

const showAddInstitution = ref(false)
const addingInstitution = ref(false)
const institutionForm = ref({ name: '', nameEn: '', country: '', city: '', erasmusCode: '' })

const programModal = ref<{ institutionId: string; institutionName: string } | null>(null)
const addingProgram = ref(false)
const programForm = ref({ name: '', nameEn: '', level: 'Undergraduate' })

const courseModal = ref<{ institutionId: string; programId: string; programName: string } | null>(null)
const addingCourse = ref(false)
const courseForm = ref({ code: '', nameEn: '', nameHr: '', ects: '', lecturesH: '', auditoryH: '', labH: '' })

const deletingInstitution = ref<string | null>(null)
const deletingProgram = ref<string | null>(null)
const deletingCourse = ref<string | null>(null)

const levels = ['Undergraduate', 'Graduate', 'Postgraduate']

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

function toggleInstitution(id: string) {
  if (expandedInstitutions.value.has(id)) expandedInstitutions.value.delete(id)
  else expandedInstitutions.value.add(id)
}

async function toggleProgram(programId: string) {
  if (expandedPrograms.value.has(programId)) { expandedPrograms.value.delete(programId); return }
  expandedPrograms.value.add(programId)
  if (!loadedCourses.value[programId]) {
    loadingCourses.value.add(programId)
    try {
      const res = await institutionService.getPartnerCourses(programId)
      loadedCourses.value[programId] = res.data
    } finally {
      loadingCourses.value.delete(programId)
    }
  }
}

function resetInstitutionForm() {
  institutionForm.value = { name: '', nameEn: '', country: '', city: '', erasmusCode: '' }
}

async function addInstitution() {
  const f = institutionForm.value
  if (!f.name.trim() || !f.country.trim()) return
  addingInstitution.value = true
  error.value = null
  try {
    const res = await adminService.createPartnerInstitution({
      name: f.name.trim(), nameEn: f.nameEn.trim() || f.name.trim(),
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
    await adminService.deletePartnerInstitution(id)
    institutions.value = institutions.value.filter(i => i.id !== id)
  } catch (e: unknown) {
    const err = e as { response?: { status?: number } }
    error.value = err.response?.status === 409 ? t('admin.institutions.hasExchanges') : t('admin.institutions.saveError')
  } finally {
    deletingInstitution.value = null
  }
}

function openProgramModal(inst: PartnerInstitutionAdminResponse) {
  programForm.value = { name: '', nameEn: '', level: 'Undergraduate' }
  programModal.value = { institutionId: inst.id, institutionName: inst.name }
  if (!expandedInstitutions.value.has(inst.id)) expandedInstitutions.value.add(inst.id)
}

async function submitProgram() {
  if (!programModal.value) return
  const f = programForm.value
  if (!f.name.trim()) return
  addingProgram.value = true
  error.value = null
  try {
    const res = await adminService.createPartnerProgram(programModal.value.institutionId, {
      name: f.name.trim(), nameEn: f.nameEn.trim() || undefined, level: f.level,
    })
    const inst = institutions.value.find(i => i.id === programModal.value!.institutionId)
    if (inst) inst.programs.push(res.data)
    programModal.value = null
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    addingProgram.value = false
  }
}

async function deleteProgram(institutionId: string, programId: string) {
  if (!await confirm({ title: t('admin.institutions.deleteProgramConfirm') })) return
  deletingProgram.value = programId
  error.value = null
  try {
    await adminService.deletePartnerProgram(programId)
    const inst = institutions.value.find(i => i.id === institutionId)
    if (inst) inst.programs = inst.programs.filter(p => p.id !== programId)
    expandedPrograms.value.delete(programId)
    delete loadedCourses.value[programId]
  } catch (e: unknown) {
    const err = e as { response?: { status?: number } }
    error.value = err.response?.status === 409 ? t('admin.institutions.hasExchanges') : t('admin.institutions.saveError')
  } finally {
    deletingProgram.value = null
  }
}

function openCourseModal(inst: PartnerInstitutionAdminResponse, prog: PartnerProgramAdminResponse) {
  courseForm.value = { code: '', nameEn: '', nameHr: '', ects: '', lecturesH: '', auditoryH: '', labH: '' }
  courseModal.value = { institutionId: inst.id, programId: prog.id, programName: prog.name }
  if (!expandedPrograms.value.has(prog.id)) toggleProgram(prog.id)
}

async function submitCourse() {
  if (!courseModal.value) return
  const f = courseForm.value
  if (!f.code.trim() || !f.nameEn.trim() || !f.ects) return
  addingCourse.value = true
  error.value = null
  try {
    const res = await adminService.createPartnerCourse(courseModal.value.programId, {
      code: f.code.trim(), nameEn: f.nameEn.trim(),
      nameHr: f.nameHr.trim() || undefined, ects: parseFloat(f.ects),
      lecturesH: f.lecturesH ? parseInt(f.lecturesH) : undefined,
      auditoryH: f.auditoryH ? parseInt(f.auditoryH) : undefined,
      labH: f.labH ? parseInt(f.labH) : undefined,
    })
    const pid = courseModal.value.programId
    const iid = courseModal.value.institutionId
    if (!loadedCourses.value[pid]) loadedCourses.value[pid] = []
    loadedCourses.value[pid].push(res.data)
    const prog = institutions.value.find(i => i.id === iid)?.programs.find(p => p.id === pid)
    if (prog) prog.courseCount++
    courseModal.value = null
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    addingCourse.value = false
  }
}

async function deleteCourse(institutionId: string, programId: string, courseId: string) {
  if (!await confirm({ title: t('admin.institutions.deleteCourseConfirm') })) return
  deletingCourse.value = courseId
  error.value = null
  try {
    await adminService.deletePartnerCourse(courseId)
    if (loadedCourses.value[programId])
      loadedCourses.value[programId] = loadedCourses.value[programId].filter(c => c.id !== courseId)
    const prog = institutions.value.find(i => i.id === institutionId)?.programs.find(p => p.id === programId)
    if (prog) prog.courseCount--
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    deletingCourse.value = null
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
          <label class="mb-1 block text-xs text-light/60">{{ t('admin.institutions.nameEn') }}</label>
          <input v-model="institutionForm.nameEn" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
        </div>
        <div>
          <label class="mb-1 block text-xs text-light/60">{{ t('admin.institutions.country') }} *</label>
          <input v-model="institutionForm.country" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
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
          <button type="button" class="flex flex-1 items-center gap-3 text-left" @click="toggleInstitution(inst.id)">
            <svg class="h-4 w-4 flex-shrink-0 text-light/30 transition-transform" :class="expandedInstitutions.has(inst.id) ? 'rotate-90' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
            <div class="min-w-0">
              <div class="flex flex-wrap items-baseline gap-x-2">
                <p class="font-semibold text-light">{{ inst.name }}</p>
                <span v-if="inst.erasmusCode" class="rounded border border-primary/30 bg-primary/10 px-1.5 py-0.5 font-mono text-xs text-primary-light">{{ inst.erasmusCode }}</span>
              </div>
              <p v-if="inst.nameEn && inst.nameEn !== inst.name" class="text-xs text-light/40">{{ inst.nameEn }}</p>
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
            <button type="button" class="rounded-lg border border-primary/30 px-3 py-1.5 text-xs font-medium text-primary-light transition hover:bg-primary/10 disabled:opacity-40" :disabled="!!deletingInstitution" @click="openProgramModal(inst)">
              + {{ t('admin.institutions.addProgram') }}
            </button>
            <button type="button" class="flex h-7 w-7 items-center justify-center rounded-lg border border-red-400/20 text-red-400/60 transition hover:border-red-400/50 hover:bg-red-500/10 hover:text-red-300 disabled:opacity-40" :disabled="deletingInstitution === inst.id" :title="t('admin.institutions.deleteInstitution')" @click="deleteInstitution(inst.id)">
              <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        <!-- Programs section -->
        <div v-if="expandedInstitutions.has(inst.id)" class="border-t border-white/5 px-5 pb-4 pt-3">
          <!-- Program search -->
          <SearchInput
            v-model="programSearch[inst.id]"
            :placeholder="t('admin.institutions.searchPrograms')"
            class="mb-3"
          />

          <p v-if="filteredPrograms(inst).length === 0" class="text-sm text-light/40">
            {{ programSearch[inst.id] ? t('admin.institutions.noResults') : t('admin.institutions.noPrograms') }}
          </p>
          <div class="space-y-2">
            <div v-for="prog in filteredPrograms(inst)" :key="prog.id" class="rounded-lg border border-white/5 bg-dark">
              <!-- Program row -->
              <div class="flex items-center gap-2 px-4 py-2.5">
                <button type="button" class="flex flex-1 items-center gap-2 text-left" @click="toggleProgram(prog.id)">
                  <svg class="h-3.5 w-3.5 flex-shrink-0 text-light/30 transition-transform" :class="expandedPrograms.has(prog.id) ? 'rotate-90' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                  </svg>
                  <span class="text-sm font-medium text-light">{{ prog.name }}</span>
                  <span v-if="prog.nameEn && prog.nameEn !== prog.name" class="text-xs text-light/40">/ {{ prog.nameEn }}</span>
                  <span class="ml-auto flex items-center gap-2 text-xs text-light/30">
                    <span class="rounded bg-white/5 px-2 py-0.5">{{ levelLabel(prog.level) }}</span>
                    <span>{{ nWord(prog.courseCount, locale, { en: ['course', 'courses'], hr: ['predmet', 'predmeta', 'predmeta'] }) }}</span>
                  </span>
                </button>
                <div class="flex flex-shrink-0 gap-1">
                  <button type="button" class="rounded px-2 py-1 text-xs text-primary-light transition hover:bg-primary/10 disabled:opacity-40" :disabled="!!deletingProgram" @click="openCourseModal(inst, prog)">
                    + {{ t('admin.institutions.addCourse') }}
                  </button>
                  <button type="button" class="flex h-6 w-6 items-center justify-center rounded text-red-400/50 transition hover:bg-red-500/10 hover:text-red-300 disabled:opacity-40" :disabled="deletingProgram === prog.id" @click="deleteProgram(inst.id, prog.id)">
                    <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              </div>

              <!-- Courses -->
              <div v-if="expandedPrograms.has(prog.id)" class="border-t border-white/5 px-4 pb-3 pt-2">
                <div v-if="loadingCourses.has(prog.id)" class="space-y-1.5">
                  <div v-for="i in 3" :key="i" class="h-7 animate-pulse rounded bg-white/5"></div>
                </div>
                <template v-else>
                  <!-- Course search -->
                  <SearchInput
                    v-model="courseSearch[prog.id]"
                    :placeholder="t('admin.institutions.searchCourses')"
                    class="mb-2"
                    @update:model-value="onCourseSearch(prog.id)"
                  />

                  <p v-if="filteredCourses(prog.id).length === 0" class="text-xs text-light/30">
                    {{ courseSearch[prog.id] ? t('admin.institutions.noResults') : t('admin.institutions.noCourses') }}
                  </p>
                  <div v-else>
                    <div class="divide-y divide-white/5">
                      <div
                        v-for="course in pagedCourses(prog.id)"
                        :key="course.id"
                        class="flex items-center justify-between py-1.5"
                      >
                        <div class="flex min-w-0 items-center gap-3">
                          <span class="w-24 flex-shrink-0 font-mono text-xs text-light/50">{{ course.code }}</span>
                          <div class="min-w-0">
                            <span class="text-xs text-light">{{ course.nameEn }}</span>
                            <span v-if="course.nameHr" class="ml-2 text-xs text-light/40">/ {{ course.nameHr }}</span>
                          </div>
                        </div>
                        <div class="flex flex-shrink-0 items-center gap-4 text-xs text-light/40">
                          <span class="font-medium text-light/60">{{ course.ects }} ECTS</span>
                          <button
                            type="button"
                            class="flex h-6 w-6 items-center justify-center rounded text-red-400/50 transition hover:bg-red-500/10 hover:text-red-300 disabled:opacity-40"
                            :disabled="deletingCourse === course.id"
                            :title="t('admin.institutions.deleteCourse')"
                            @click="deleteCourse(inst.id, prog.id, course.id)"
                          >
                            <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                            </svg>
                          </button>
                        </div>
                      </div>
                    </div>

                    <!-- Course pagination -->
                    <div v-if="totalCoursePages(prog.id) > 1" class="mt-2 flex items-center justify-end gap-2 text-xs text-light/40">
                      <button
                        type="button"
                        class="rounded px-2 py-0.5 transition hover:text-light disabled:opacity-30"
                        :disabled="(coursePage[prog.id] ?? 1) <= 1"
                        @click="coursePage[prog.id] = (coursePage[prog.id] ?? 1) - 1"
                      >←</button>
                      <span>{{ coursePage[prog.id] ?? 1 }} / {{ totalCoursePages(prog.id) }}</span>
                      <button
                        type="button"
                        class="rounded px-2 py-0.5 transition hover:text-light disabled:opacity-30"
                        :disabled="(coursePage[prog.id] ?? 1) >= totalCoursePages(prog.id)"
                        @click="coursePage[prog.id] = (coursePage[prog.id] ?? 1) + 1"
                      >→</button>
                    </div>
                  </div>
                </template>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Institution pagination -->
    <div v-if="totalInstPages > 1" class="flex items-center justify-center gap-3 text-sm text-light/50">
      <button
        type="button"
        class="rounded-lg border border-white/10 px-3 py-1.5 transition hover:text-light disabled:opacity-30"
        :disabled="institutionPage <= 1"
        @click="institutionPage--"
      >←</button>
      <span>{{ institutionPage }} / {{ totalInstPages }}</span>
      <button
        type="button"
        class="rounded-lg border border-white/10 px-3 py-1.5 transition hover:text-light disabled:opacity-30"
        :disabled="institutionPage >= totalInstPages"
        @click="institutionPage++"
      >→</button>
    </div>
  </div>

  <!-- Add Program Modal -->
  <Teleport to="body">
    <div
      v-if="programModal"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 px-4"
      @mousedown.self="programModal = null"
    >
      <div class="w-full max-w-md rounded-2xl border border-primary/20 bg-dark-2 shadow-2xl">
        <div class="flex items-center justify-between border-b border-primary/20 px-6 py-4">
          <div>
            <h3 class="font-semibold text-light">{{ t('admin.institutions.addProgram') }}</h3>
            <p class="mt-0.5 text-xs text-light/40">{{ programModal.institutionName }}</p>
          </div>
          <button type="button" class="text-light/40 transition hover:text-white" @click="programModal = null">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
        <div class="space-y-4 px-6 py-5">
          <div>
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.programName') }} *</label>
            <input v-model="programForm.name" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" @keydown.enter="submitProgram" />
          </div>
          <div>
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.programNameEn') }} <span class="text-light/30">({{ t('admin.institutions.optional') }})</span></label>
            <input v-model="programForm.nameEn" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" @keydown.enter="submitProgram" />
          </div>
          <div>
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.level') }} *</label>
            <div class="flex gap-2">
              <button
                v-for="lvl in levels"
                :key="lvl"
                type="button"
                class="flex-1 rounded-lg border py-2 text-xs font-medium transition"
                :class="programForm.level === lvl ? 'border-primary bg-primary/10 text-white' : 'border-white/10 bg-dark text-light/60 hover:border-primary/40 hover:text-white'"
                @click="programForm.level = lvl"
              >
                {{ levelLabel(lvl) }}
              </button>
            </div>
          </div>
        </div>
        <div class="flex justify-end gap-2 border-t border-primary/20 px-6 py-4">
          <button type="button" class="rounded-lg border border-white/10 px-4 py-2 text-sm text-light/60 transition hover:text-light" @click="programModal = null">{{ t('admin.institutions.cancel') }}</button>
          <button
            type="button"
            class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
            :disabled="addingProgram || !programForm.name.trim()"
            @click="submitProgram"
          >
            {{ addingProgram ? t('common.loading') : t('admin.institutions.save') }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>

  <!-- Add Course Modal -->
  <Teleport to="body">
    <div
      v-if="courseModal"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 px-4"
      @mousedown.self="courseModal = null"
    >
      <div class="w-full max-w-lg rounded-2xl border border-primary/20 bg-dark-2 shadow-2xl">
        <div class="flex items-center justify-between border-b border-primary/20 px-6 py-4">
          <div>
            <h3 class="font-semibold text-light">{{ t('admin.institutions.addCourse') }}</h3>
            <p class="mt-0.5 text-xs text-light/40">{{ courseModal.programName }}</p>
          </div>
          <button type="button" class="text-light/40 transition hover:text-white" @click="courseModal = null">
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
          <!-- Name EN -->
          <div>
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.courseNameEn') }} *</label>
            <input v-model="courseForm.nameEn" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
          </div>
          <!-- Name HR -->
          <div>
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.courseNameHr') }} <span class="text-light/30">({{ t('admin.institutions.optional') }})</span></label>
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
        </div>
        <div class="flex justify-end gap-2 border-t border-primary/20 px-6 py-4">
          <button type="button" class="rounded-lg border border-white/10 px-4 py-2 text-sm text-light/60 transition hover:text-light" @click="courseModal = null">{{ t('admin.institutions.cancel') }}</button>
          <button
            type="button"
            class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
            :disabled="addingCourse || !courseForm.code.trim() || !courseForm.nameEn.trim() || !courseForm.ects"
            @click="submitCourse"
          >
            {{ addingCourse ? t('common.loading') : t('admin.institutions.save') }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>

</template>

