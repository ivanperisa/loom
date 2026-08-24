<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import axios from 'axios'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import type { PartnerInstitutionAdminResponse } from '@/types/institution.types'
import SearchInput from '@/components/common/SearchInput.vue'
import SearchableSelect from '@/components/common/SearchableSelect.vue'
import ShowDeletedToggle from '@/components/common/ShowDeletedToggle.vue'
import Pagination from '@/components/common/Pagination.vue'
import SortableHeader from '@/components/common/SortableHeader.vue'
import { useConfirm } from '@/composables/useConfirm'
import { useDebouncedRef } from '@/composables/useDebouncedRef'
import { minSearchTerm } from '@/utils/searchTerm'
import { ISO_COUNTRIES } from '@/constants/countries'
import PartnerInstitutionFormPanel from '@/components/admin/PartnerInstitutionFormPanel.vue'
import PartnerCourseList from '@/components/admin/PartnerCourseList.vue'

const { t } = useI18n()
const { confirm } = useConfirm()

const INST_PER_PAGE = 10

const institutions = ref<PartnerInstitutionAdminResponse[]>([])
const totalCount = ref(0)
const institutionPage = ref(1)
const totalInstPages = computed(() => Math.max(1, Math.ceil(totalCount.value / INST_PER_PAGE)))
const loading = ref(true)
const error = ref<string | null>(null)

const institutionSearch = ref('')
const debouncedInstitutionSearch = useDebouncedRef(institutionSearch, 400)
const countryFilter = ref<string | null>(null)
const showDeleted = ref(false)
const hasDeleted = ref(false)

const sortBy = ref('erasmusCode')
const sortDir = ref<'asc' | 'desc'>('asc')

function toggleSort(key: string) {
  if (sortBy.value === key) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortBy.value = key
    sortDir.value = 'asc'
  }
}

const countryOptions = computed(() => [
  { value: null, label: t('admin.institutions.allCountries') },
  ...ISO_COUNTRIES.map(c => ({ value: c, label: t(`countries.${c}`) })),
])

const showAddInstitution = ref(false)
const addingInstitution = ref(false)
const editingInstitutionId = ref<string | null>(null)
const deletingInstitution = ref<string | null>(null)

const editingInstitution = computed(() =>
  editingInstitutionId.value ? institutions.value.find(i => i.id === editingInstitutionId.value) : undefined,
)

// Drill-down into a single institution's courses
const openInstitution = ref<PartnerInstitutionAdminResponse | null>(null)
const autoOpenCreateCourse = ref(false)

function drillInto(inst: PartnerInstitutionAdminResponse, autoOpenCreate = false) {
  openInstitution.value = inst
  autoOpenCreateCourse.value = autoOpenCreate
}

function backToInstitutions() {
  openInstitution.value = null
  autoOpenCreateCourse.value = false
}

onMounted(loadInstitutions)

watch(
  [institutionPage, debouncedInstitutionSearch, countryFilter, showDeleted, sortBy, sortDir],
  ([newPage], [oldPage]) => {
    if (newPage === oldPage && newPage !== 1) {
      institutionPage.value = 1
      return
    }
    loadInstitutions()
  },
)

let loadInstitutionsController: AbortController | null = null

async function loadInstitutions() {
  loadInstitutionsController?.abort()
  const controller = new AbortController()
  loadInstitutionsController = controller
  loading.value = true
  error.value = null
  try {
    const res = await institutionService.getPartnerInstitutions(showDeleted.value, {
      page: institutionPage.value,
      pageSize: INST_PER_PAGE,
      search: minSearchTerm(debouncedInstitutionSearch.value),
      country: countryFilter.value,
      sortBy: sortBy.value,
      sortDir: sortDir.value,
    }, controller.signal)
    institutions.value = res.data.items
    totalCount.value = res.data.totalCount
    hasDeleted.value = res.data.hasDeleted
    if (institutionPage.value > totalInstPages.value) institutionPage.value = totalInstPages.value
  } catch (err) {
    if (axios.isCancel(err)) return
    error.value = t('admin.institutions.saveError')
  } finally {
    if (loadInstitutionsController === controller) loading.value = false
  }
}

function toggleAddPanel() {
  editingInstitutionId.value = null
  showAddInstitution.value = !showAddInstitution.value
}

function openEditInstitution(inst: PartnerInstitutionAdminResponse) {
  editingInstitutionId.value = inst.id
  showAddInstitution.value = true
}

function closeInstitutionForm() {
  showAddInstitution.value = false
  editingInstitutionId.value = null
}

async function submitInstitutionForm(payload: { name: string; nameHr: string; country: string; city?: string; erasmusCode?: string }) {
  addingInstitution.value = true
  error.value = null
  try {
    if (editingInstitutionId.value) {
      await institutionService.updatePartnerInstitution(editingInstitutionId.value, payload)
    } else {
      await institutionService.createPartnerInstitution(payload)
    }
    await loadInstitutions()
    closeInstitutionForm()
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
    await loadInstitutions()
  } catch (e: unknown) {
    const err = e as { response?: { status?: number } }
    error.value = err.response?.status === 409 ? t('admin.institutions.hasExchanges') : t('admin.institutions.saveError')
  } finally {
    deletingInstitution.value = null
  }
}

async function restoreInstitution(id: string) {
  deletingInstitution.value = id
  error.value = null
  try {
    await institutionService.restorePartnerInstitution(id)
    await loadInstitutions()
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    deletingInstitution.value = null
  }
}

function onCourseCountChanged(delta: number) {
  if (openInstitution.value) openInstitution.value.courseCount += delta
  const inst = institutions.value.find(i => i.id === openInstitution.value?.id)
  if (inst) inst.courseCount += delta
}
</script>

<template>
  <div v-if="openInstitution" class="space-y-4">
    <button type="button" class="flex items-center gap-1.5 text-sm text-light/60 transition hover:text-light" @click="backToInstitutions">
      <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
      </svg>
      {{ t('admin.institutions.backToInstitutions') }}
    </button>
    <PartnerCourseList
      :key="openInstitution.id"
      :institution-id="openInstitution.id"
      :institution-name="openInstitution.name"
      :institution-name-hr="openInstitution.nameHr"
      :auto-open-create="autoOpenCreateCourse"
      @count-changed="onCourseCountChanged"
    />
  </div>

  <div v-else class="space-y-6">
    <div class="flex items-center justify-between">
      <h2 class="text-xl font-semibold text-light">{{ t('admin.institutions.title') }}</h2>
      <button
        type="button"
        class="flex items-center gap-1.5 rounded-xl bg-primary-strong px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
        @click="toggleAddPanel"
      >
        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        {{ t('admin.institutions.addButton') }}
      </button>
    </div>

    <p v-if="error" class="rounded-xl border border-danger-text/35 bg-danger-fill px-4 py-3 text-sm text-danger-text">
      {{ error }}
    </p>

    <PartnerInstitutionFormPanel
      v-if="showAddInstitution"
      :key="editingInstitutionId ?? ''"
      :institution="editingInstitution"
      :saving="addingInstitution"
      @submit="submitInstitutionForm"
      @cancel="closeInstitutionForm"
    />

    <div class="flex flex-wrap items-center gap-3">
      <SearchInput
        v-model="institutionSearch"
        :placeholder="t('admin.institutions.searchInstitutions')"
        class="min-w-[200px] flex-1"
      />
      <SearchableSelect
        v-model="countryFilter"
        :options="countryOptions"
        :placeholder="t('admin.institutions.allCountries')"
        :search-placeholder="t('admin.institutions.countryPlaceholder')"
        :no-results-label="t('admin.institutions.noResults')"
        class="min-w-[180px] max-w-[260px] flex-none"
      />
      <ShowDeletedToggle v-if="hasDeleted" v-model="showDeleted" />
    </div>

    <div v-if="loading && institutions.length === 0" class="space-y-3">
      <div v-for="i in 4" :key="i" class="h-16 animate-pulse rounded-xl bg-dark-2"></div>
    </div>

    <div v-else-if="institutions.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-6 text-center text-light/60">
      {{ institutionSearch ? t('admin.institutions.noResults') : t('admin.institutions.empty') }}
    </div>

    <template v-else>
      <div class="overflow-x-auto rounded-xl border border-primary/20 bg-dark-2">
        <div class="min-w-[900px]">
          <div class="admin-institution-grid gap-4 border-b border-primary/20 px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-light/40">
            <SortableHeader :label="t('admin.institutions.columns.erasmusCode')" sort-key="erasmusCode" :active-key="sortBy" :dir="sortDir" @sort="toggleSort" />
            <SortableHeader :label="t('admin.institutions.columns.institution')" sort-key="name" :active-key="sortBy" :dir="sortDir" @sort="toggleSort" />
            <SortableHeader :label="t('admin.institutions.columns.country')" sort-key="country" :active-key="sortBy" :dir="sortDir" @sort="toggleSort" />
            <span>{{ t('admin.institutions.columns.city') }}</span>
            <span></span>
          </div>

          <div class="divide-y divide-hairline-soft">
            <div
              v-for="inst in institutions"
              :key="inst.id"
              class="admin-institution-grid cursor-pointer gap-4 px-4 py-3 text-sm transition hover:bg-dark"
              :class="inst.isDeleted ? 'opacity-60' : ''"
              @click="drillInto(inst)"
            >
              <span>
                <span v-if="inst.erasmusCode" class="rounded border border-primary/30 bg-primary/10 px-1.5 py-0.5 font-mono text-xs text-primary-text">{{ inst.erasmusCode }}</span>
                <span v-else class="text-light/40">—</span>
              </span>

              <div class="min-w-0">
                <p class="truncate text-light/70">{{ inst.name }}</p>
                <p v-if="inst.nameHr && inst.nameHr !== inst.name" class="truncate text-xs text-light/40">{{ inst.nameHr }}</p>
              </div>

              <span class="truncate text-light/70">{{ t(`countries.${inst.country}`) }}</span>
              <span class="truncate text-light/70">{{ inst.city || '—' }}</span>

              <div class="flex items-center justify-end gap-1" @click.stop>
                <template v-if="!inst.isDeleted">
                  <button
                    type="button"
                    class="flex h-7 w-7 items-center justify-center rounded-lg text-light/40 transition hover:bg-fill hover:text-primary-text"
                    :title="t('admin.institutions.addCourse')"
                    @click="drillInto(inst, true)"
                  >
                    <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                      <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
                    </svg>
                  </button>
                  <button
                    type="button"
                    class="flex h-7 w-7 items-center justify-center rounded-lg text-light/40 transition hover:bg-fill hover:text-light"
                    :title="t('admin.institutions.editInstitution')"
                    @click="openEditInstitution(inst)"
                  >
                    <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button
                    type="button"
                    class="flex h-7 w-7 items-center justify-center rounded-lg text-danger/50 transition hover:bg-danger-fill hover:text-danger-text disabled:opacity-40"
                    :disabled="deletingInstitution === inst.id"
                    :title="t('admin.institutions.deleteInstitution')"
                    @click="deleteInstitution(inst.id)"
                  >
                    <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </template>
                <button
                  v-else
                  type="button"
                  class="rounded-lg border border-success-text/35 px-3 py-1.5 text-xs font-medium text-success-text transition hover:bg-success-fill disabled:opacity-40"
                  :disabled="deletingInstitution === inst.id"
                  @click="restoreInstitution(inst.id)"
                >
                  {{ t('admin.institutions.restore') }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <Pagination
        :page="institutionPage"
        :total-pages="totalInstPages"
        :total="totalCount"
        :per-page="INST_PER_PAGE"
        @update:page="institutionPage = $event"
      />
    </template>
  </div>
</template>

<style scoped>
.admin-institution-grid {
  display: grid;
  grid-template-columns: 110px minmax(180px, 1.6fr) minmax(110px, 0.9fr) minmax(100px, 0.9fr) 108px;
  align-items: center;
}
</style>
