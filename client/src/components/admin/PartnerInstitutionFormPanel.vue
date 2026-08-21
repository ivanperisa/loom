<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { PartnerInstitutionAdminResponse } from '@/types/institution.types'
import SearchableSelect, { type SelectOption } from '@/components/common/SearchableSelect.vue'
import { ISO_COUNTRIES } from '@/constants/countries'

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
  <div class="rounded-xl border border-primary/20 bg-dark-2 p-5">
    <h3 class="mb-4 text-sm font-semibold text-primary-light">{{ institution ? t('admin.institutions.editTitle') : t('admin.institutions.addTitle') }}</h3>
    <div class="grid grid-cols-2 gap-3 sm:grid-cols-3">
      <div>
        <label class="mb-1 block text-xs text-light/60">{{ t('admin.institutions.name') }} *</label>
        <input v-model="form.name" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
      </div>
      <div>
        <label class="mb-1 block text-xs text-light/60">{{ t('admin.institutions.nameHr') }}</label>
        <input v-model="form.nameHr" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
      </div>
      <div>
        <label class="mb-1 block text-xs text-light/60">{{ t('admin.institutions.country') }} *</label>
        <SearchableSelect
          v-model="form.country"
          :options="countryOptions"
          :placeholder="t('admin.institutions.countryPlaceholder')"
          :search-placeholder="t('admin.institutions.countryPlaceholder')"
          :no-results-label="t('admin.institutions.noResults')"
        />
      </div>
      <div>
        <label class="mb-1 block text-xs text-light/60">{{ t('admin.institutions.city') }} <span class="text-light/30">({{ t('admin.institutions.optional') }})</span></label>
        <input v-model="form.city" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
      </div>
      <div>
        <label class="mb-1 block text-xs text-light/60">{{ t('admin.institutions.erasmusCode') }} <span class="text-light/30">({{ t('admin.institutions.optional') }})</span></label>
        <input v-model="form.erasmusCode" type="text" class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none" />
      </div>
    </div>
    <div class="mt-4 flex gap-2">
      <button
        type="button"
        class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
        :disabled="saving || !form.name.trim() || !form.country.trim()"
        @click="submit"
      >
        {{ saving ? t('common.loading') : (institution ? t('admin.institutions.saveEdit') : t('admin.institutions.save')) }}
      </button>
      <button type="button" class="rounded-lg border border-hairline px-4 py-2 text-sm text-light/60 transition hover:text-light" @click="emit('cancel')">
        {{ t('admin.institutions.cancel') }}
      </button>
    </div>
  </div>
</template>
