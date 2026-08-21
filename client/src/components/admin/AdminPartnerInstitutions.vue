<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import type { PartnerInstitutionAdminResponse } from '@/types/institution.types'
import SearchInput from '@/components/common/SearchInput.vue'
import Pagination from '@/components/common/Pagination.vue'
import { useConfirm } from '@/composables/useConfirm'
import { useDebouncedRef } from '@/composables/useDebouncedRef'
import PartnerInstitutionRow from '@/components/admin/PartnerInstitutionRow.vue'
import PartnerInstitutionFormPanel from '@/components/admin/PartnerInstitutionFormPanel.vue'

const { t } = useI18n()
const { confirm } = useConfirm()

const INST_PER_PAGE = 25

const institutions = ref<PartnerInstitutionAdminResponse[]>([])
const totalCount = ref(0)
const institutionPage = ref(1)
const totalInstPages = computed(() => Math.ceil(totalCount.value / INST_PER_PAGE))
const loading = ref(true)
const error = ref<string | null>(null)

const institutionSearch = ref('')
const debouncedInstitutionSearch = useDebouncedRef(institutionSearch)
const showDeleted = ref(false)

const showAddInstitution = ref(false)
const addingInstitution = ref(false)
const editingInstitutionId = ref<string | null>(null)
const deletingInstitution = ref<string | null>(null)

const editingInstitution = computed(() =>
  editingInstitutionId.value ? institutions.value.find(i => i.id === editingInstitutionId.value) : undefined,
)

onMounted(loadInstitutions)

watch([institutionPage, debouncedInstitutionSearch, showDeleted], ([newPage, newSearch, newShowDeleted], [, oldSearch, oldShowDeleted]) => {
  if ((newSearch !== oldSearch || newShowDeleted !== oldShowDeleted) && newPage !== 1) {
    institutionPage.value = 1
    return
  }
  loadInstitutions()
})

async function loadInstitutions() {
  loading.value = true
  error.value = null
  try {
    const res = await institutionService.getPartnerInstitutions(showDeleted.value, {
      page: institutionPage.value,
      pageSize: INST_PER_PAGE,
      search: debouncedInstitutionSearch.value,
    })
    institutions.value = res.data.items
    totalCount.value = res.data.totalCount
  } catch {
    error.value = t('admin.institutions.saveError')
  } finally {
    loading.value = false
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

function onCourseCountChanged(inst: PartnerInstitutionAdminResponse, delta: number) {
  inst.courseCount += delta
}
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h2 class="text-xl font-semibold text-light">{{ t('admin.institutions.title') }}</h2>
      <button
        type="button"
        class="rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
        @click="toggleAddPanel"
      >
        {{ t('admin.institutions.addButton') }}
      </button>
    </div>

    <p v-if="error" class="rounded-xl border border-danger/40 bg-danger/10 px-4 py-3 text-sm text-danger">
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

    <div class="flex items-center gap-3">
      <SearchInput
        v-model="institutionSearch"
        :placeholder="t('admin.institutions.searchInstitutions')"
        class="flex-1"
      />
      <label class="flex shrink-0 items-center gap-2 text-xs text-light/60">
        <input v-model="showDeleted" type="checkbox" class="accent-primary" />
        {{ t('admin.institutions.showDeleted') }}
      </label>
    </div>

    <div v-if="loading" class="space-y-3">
      <div v-for="i in 4" :key="i" class="h-16 animate-pulse rounded-xl bg-dark-2"></div>
    </div>

    <div v-else-if="institutions.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-6 text-center text-light/60">
      {{ institutionSearch ? t('admin.institutions.noResults') : t('admin.institutions.empty') }}
    </div>

    <!-- Institutions list -->
    <div v-else class="space-y-3">
      <PartnerInstitutionRow
        v-for="inst in institutions"
        :key="inst.id"
        :institution="inst"
        :busy="deletingInstitution === inst.id"
        @edit="openEditInstitution"
        @delete="deleteInstitution"
        @restore="restoreInstitution"
        @count-changed="onCourseCountChanged(inst, $event)"
      />
    </div>

    <!-- Institution pagination -->
    <Pagination
      :page="institutionPage"
      :total-pages="totalInstPages"
      :total="totalCount"
      :per-page="INST_PER_PAGE"
      @update:page="institutionPage = $event"
    />
  </div>
</template>
