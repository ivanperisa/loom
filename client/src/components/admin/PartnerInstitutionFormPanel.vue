<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { PartnerInstitutionAdminResponse } from '@/types/institution.types'
import SearchableSelect, { type SelectOption } from '@/components/common/SearchableSelect.vue'
import { ISO_COUNTRIES } from '@/constants/countries'
import BaseModal from '@/components/common/BaseModal.vue'

const { t } = useI18n()

const props = defineProps<{ institution?: PartnerInstitutionAdminResponse; saving: boolean }>()
const emit = defineEmits<{
  submit: [payload: {
    name: string
    nameHr: string
    country: string
    city?: string
    erasmusCode?: string
  }]
  cancel: []
}>()

function blankForm() {
  return { name: '', nameHr: '', country: '', city: '', erasmusCode: '' }
}

const form = ref(
  props.institution
    ? {
        name: props.institution.name,
        nameHr: props.institution.nameHr ?? '',
        country: props.institution.country,
        city: props.institution.city ?? '',
        erasmusCode: props.institution.erasmusCode ?? '',
      }
    : blankForm(),
)

const countryOptions = computed<SelectOption[]>(() =>
  ISO_COUNTRIES.map(c => ({ value: c, label: t(`countries.${c}`) })),
)

function submit() {
  const f = form.value
  if (!f.name.trim() || !f.country.trim()) return
  emit('submit', {
    name: f.name.trim(),
    nameHr: f.nameHr.trim() || f.name.trim(),
    country: f.country.trim(),
    city: f.city.trim() || undefined,
    erasmusCode: f.erasmusCode.trim() || undefined,
  })
}
</script>

<template>
  <BaseModal max-width="max-w-2xl" labelled-by="partner-institution-form-title" @close="emit('cancel')">
    <div class="rounded-2xl border border-primary/20 bg-dark-2 shadow-2xl">
      <div class="flex items-center justify-between border-b border-primary/20 px-6 py-4">
        <h3 id="partner-institution-form-title" class="font-semibold text-light">
          {{ institution ? t('admin.institutions.editTitle') : t('admin.institutions.addTitle') }}
        </h3>
        <button :aria-label="t('common.close')" type="button" class="text-light/40 transition hover:text-light" @click="emit('cancel')">
          <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>
      <div class="space-y-4 px-6 py-5">
        <div class="flex gap-3">
          <div class="flex-1">
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.name') }} *</label>
            <input v-model="form.name" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
          </div>
          <div class="flex-1">
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.nameHr') }}</label>
            <input v-model="form.nameHr" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
          </div>
        </div>
        <div class="flex gap-3">
          <div class="flex-1">
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.country') }} *</label>
            <SearchableSelect
              v-model="form.country"
              :options="countryOptions"
              :placeholder="t('admin.institutions.countryPlaceholder')"
              :search-placeholder="t('admin.institutions.countryPlaceholder')"
              :no-results-label="t('admin.institutions.noResults')"
            />
          </div>
          <div class="flex-1">
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.city') }} <span class="text-light/30">({{ t('admin.institutions.optional') }})</span></label>
            <input v-model="form.city" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
          </div>
        </div>
        <div>
          <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.institutions.erasmusCode') }} <span class="text-light/30">({{ t('admin.institutions.optional') }})</span></label>
          <input v-model="form.erasmusCode" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
        </div>
      </div>
      <div class="flex justify-end gap-2 border-t border-primary/20 px-6 py-4">
        <button type="button" class="rounded-lg border border-hairline px-4 py-2 text-sm text-light/60 transition hover:text-light" @click="emit('cancel')">
          {{ t('admin.institutions.cancel') }}
        </button>
        <button
          type="button"
          class="rounded-lg bg-primary-strong px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
          :disabled="saving || !form.name.trim() || !form.country.trim()"
          @click="submit"
        >
          {{ saving ? t('common.loading') : (institution ? t('admin.institutions.saveEdit') : t('admin.institutions.save')) }}
        </button>
      </div>
    </div>
  </BaseModal>
</template>
