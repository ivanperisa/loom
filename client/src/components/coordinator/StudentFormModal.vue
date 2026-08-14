<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { coordinatorService } from '@/services/coordinator.service'
import SearchableSelect from '@/components/common/SearchableSelect.vue'
import BaseModal from '@/components/common/BaseModal.vue'
import type { CoordinatorStudentResponse } from '@/types/coordinator.types'
import type { InstitutionResponse } from '@/types/institution.types'
import { localizedName } from '@/utils/i18n.utils'

const props = defineProps<{
  mode: 'create' | 'edit'
  institutions: InstitutionResponse[]
  student?: CoordinatorStudentResponse
}>()

const emit = defineEmits<{
  close: []
  saved: [student: CoordinatorStudentResponse]
}>()

const { t } = useI18n()

const name = ref(props.student?.name ?? '')
const jmbag = ref(props.student?.jmbag ?? '')
const institutionId = ref<string | null>(props.student?.institutionId ?? null)
const error = ref<string | null>(null)
const submitting = ref(false)

const institutionOptions = computed(() =>
  props.institutions.map((i) => ({
    value: i.id,
    label: localizedName(i),
    sublabel: i.city ?? undefined,
  })),
)

const isJmbagValid = computed(() => /^\d{10}$/.test(jmbag.value))

async function submit() {
  error.value = null
  if (!name.value.trim()) {
    error.value = t('coordinator.addStudentModal.errors.nameRequired')
    return
  }
  if (!isJmbagValid.value) {
    error.value = t('coordinator.addStudentModal.errors.jmbagInvalid')
    return
  }
  if (!institutionId.value) {
    error.value = t('coordinator.addStudentModal.errors.institutionRequired')
    return
  }
  submitting.value = true
  try {
    const payload = { name: name.value.trim(), jmbag: jmbag.value, institutionId: institutionId.value }
    const res = props.mode === 'edit' && props.student
      ? await coordinatorService.updateStudent(props.student.id, payload)
      : await coordinatorService.createPlaceholderStudent(payload)
    emit('saved', res.data)
  } catch (e: unknown) {
    const err = e as { response?: { data?: { detail?: string } } }
    error.value = err?.response?.data?.detail ?? t('errors.unexpected')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <BaseModal max-width="max-w-lg" labelled-by="student-form-title" @close="emit('close')">
    <div class="rounded-2xl border border-primary/20 bg-dark-2 shadow-2xl">
      <div class="flex items-center justify-between border-b border-primary/20 px-6 py-4">
        <h3 id="student-form-title" class="font-semibold text-light">
          {{ mode === 'edit' ? t('coordinator.editStudentModal.title') : t('coordinator.addStudentModal.title') }}
        </h3>
          <button type="button" class="text-light/40 transition hover:text-white" @click="emit('close')">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>

        <div class="space-y-4 px-6 py-5">
          <!-- Name -->
          <div>
            <label class="mb-1.5 block text-sm text-light/70">{{ t('coordinator.addStudentModal.nameLabel') }} *</label>
            <input
              v-model="name"
              type="text"
              :placeholder="t('coordinator.addStudentModal.namePlaceholder')"
              class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
            />
          </div>

          <!-- JMBAG -->
          <div>
            <label class="mb-1.5 block text-sm text-light/70">{{ t('coordinator.addStudentModal.jmbagLabel') }} *</label>
            <input
              v-model="jmbag"
              type="text"
              inputmode="numeric"
              maxlength="10"
              :placeholder="t('coordinator.addStudentModal.jmbagPlaceholder')"
              class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 font-mono text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
            />
          </div>

          <!-- Institution -->
          <div>
            <label class="mb-1.5 block text-sm text-light/70">{{ t('coordinator.addStudentModal.institutionLabel') }} *</label>
            <SearchableSelect
              v-model="institutionId"
              :options="institutionOptions"
              :search-placeholder="t('coordinator.addStudentModal.searchInstitution')"
            />
          </div>

          <p v-if="error" class="text-xs text-red-400">{{ error }}</p>
        </div>

        <div class="flex justify-end gap-2 border-t border-primary/20 px-6 py-4">
          <button
            type="button"
            class="rounded-lg border border-white/10 px-4 py-2 text-sm text-light/60 transition hover:text-light"
            @click="emit('close')"
          >{{ t('coordinator.addStudentModal.cancel') }}</button>
          <button
            type="button"
            class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
            :disabled="submitting"
            @click="submit"
          >{{ submitting ? t('common.loading') : t('coordinator.addStudentModal.submit') }}</button>
        </div>
      </div>
  </BaseModal>
</template>
