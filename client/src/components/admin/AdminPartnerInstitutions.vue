<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import type { PartnerInstitutionAdminResponse } from '@/types/institution.types'
import SearchInput from '@/components/common/SearchInput.vue'
import { useConfirm } from '@/composables/useConfirm'
import PartnerInstitutionRow from '@/components/admin/PartnerInstitutionRow.vue'
import PartnerInstitutionFormPanel from '@/components/admin/PartnerInstitutionFormPanel.vue'

const { t } = useI18n()
const { confirm } = useConfirm()

const INST_PER_PAGE = 10

const institutions = ref<PartnerInstitutionAdminResponse[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const institutionSearch = ref('')
const institutionPage = ref(1)
const showDeleted = ref(false)

const showAddInstitution = ref(false)
const addingInstitution = ref(false)
const editingInstitutionId = ref<string | null>(null)
const deletingInstitution = ref<string | null>(null)

const hasDeletedInstitutions = computed(() => institutions.value.some(i => i.isDeleted))

const filteredInstitutions = computed(() => {
  const q = institutionSearch.value.trim().toLowerCase()
  let list = institutions.value
  if (!showDeleted.value) list = list.filter(i => !i.isDeleted)
  if (!q) return list
  return list.filter(i =>
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

const editingInstitution = computed(() =>
  editingInstitutionId.value ? institutions.value.find(i => i.id === editingInstitutionId.value) : undefined,
)

onMounted(loadInstitutions)

async function loadInstitutions() {
  loading.value = true
  error.value = null
  try {
    const res = await institutionService.getPartnerInstitutions(true)
    institutions.value = res.data
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
      const res = await institutionService.updatePartnerInstitution(editingInstitutionId.value, payload)
      const idx = institutions.value.findIndex(i => i.id === editingInstitutionId.value)
      if (idx !== -1) institutions.value[idx] = res.data
    } else {
      const res = await institutionService.createPartnerInstitution(payload)
      institutions.value.push(res.data)
    }
    institutions.value.sort((a, b) => a.country.localeCompare(b.country) || a.name.localeCompare(b.name))
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

    <p v-if="error" class="rounded-xl border border-red-400/40 bg-red-500/10 px-4 py-3 text-sm text-red-300">
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
        @update:model-value="onInstSearch"
      />
      <label v-if="hasDeletedInstitutions" class="flex shrink-0 items-center gap-2 text-xs text-light/60">
        <input v-model="showDeleted" type="checkbox" class="accent-primary" />
        {{ t('admin.institutions.showDeleted') }}
      </label>
    </div>

    <div v-if="loading" class="space-y-3">
      <div v-for="i in 4" :key="i" class="h-16 animate-pulse rounded-xl bg-dark-2"></div>
    </div>

    <div v-else-if="filteredInstitutions.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-6 text-center text-light/60">
      {{ institutionSearch ? t('admin.institutions.noResults') : t('admin.institutions.empty') }}
    </div>

    <!-- Institutions list -->
    <div v-else class="space-y-3">
      <PartnerInstitutionRow
        v-for="inst in pagedInstitutions"
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
</template>
