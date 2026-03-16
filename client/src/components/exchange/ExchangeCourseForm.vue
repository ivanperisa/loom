<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { UpsertExchangeCourseRequest, ExchangeCourseResponse, ExchangeCourseStatus } from '@/types/exchange.types'

const props = defineProps<{
  initial?: ExchangeCourseResponse
}>()

const emit = defineEmits<{
  (e: 'submit', data: UpsertExchangeCourseRequest): void
  (e: 'cancel'): void
}>()

const { t } = useI18n()

const form = ref<UpsertExchangeCourseRequest>({
  code: props.initial?.code ?? '',
  name: props.initial?.name ?? '',
  nameEn: props.initial?.nameEn ?? '',
  nameHr: props.initial?.nameHr ?? '',
  ects: props.initial?.ects ?? undefined,
  status: (props.initial?.status ?? 'OriginallyEnrolled') as ExchangeCourseStatus,
  lecturesHours: props.initial?.lecturesHours ?? undefined,
  auditoryHours: props.initial?.auditoryHours ?? undefined,
  labHours: props.initial?.labHours ?? undefined
})

watch(() => props.initial, (val) => {
  if (val) {
    form.value = {
      code: val.code ?? '',
      name: val.name,
      nameEn: val.nameEn,
      nameHr: val.nameHr ?? '',
      ects: val.ects,
      status: val.status,
      lecturesHours: val.lecturesHours,
      auditoryHours: val.auditoryHours,
      labHours: val.labHours
    }
  }
}, { immediate: true })

function handleSubmit() {
  const data = { ...form.value }
  if (!data.name?.trim()) data.name = data.nameEn
  emit('submit', data)
}
</script>

<template>
  <form @submit.prevent="handleSubmit" class="space-y-3">
    <div class="grid grid-cols-2 gap-3">
      <div>
        <label class="mb-1 block text-xs font-medium text-[#8AC4ED]">{{ t('exchangeCourse.code') }}</label>
        <input
          v-model="form.code"
          type="text"
          class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]"
        />
      </div>
      <div>
        <label class="mb-1 block text-xs font-medium text-[#8AC4ED]">{{ t('exchangeCourse.ects') }}</label>
        <input
          v-model.number="form.ects"
          type="number"
          min="0"
          step="0.5"
          class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]"
        />
      </div>
    </div>

    <div>
      <label class="mb-1 block text-xs font-medium text-[#8AC4ED]">{{ t('exchangeCourse.nameEn') }} *</label>
      <input
        v-model="form.nameEn"
        type="text"
        required
        class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]"
      />
    </div>

    <div>
      <label class="mb-1 block text-xs font-medium text-[#8AC4ED]">Naziv (originalni jezik)</label>
      <input
        v-model="form.name"
        type="text"
        :placeholder="form.nameEn"
        class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]"
      />
    </div>

    <div>
      <label class="mb-1 block text-xs font-medium text-[#8AC4ED]">{{ t('exchangeCourse.nameHr') }}</label>
      <input
        v-model="form.nameHr"
        type="text"
        class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]"
      />
    </div>

    <div class="grid grid-cols-3 gap-3">
      <div>
        <label class="mb-1 block text-xs font-medium text-[#8AC4ED]">{{ t('exchangeCourse.lecturesHours') }}</label>
        <input v-model.number="form.lecturesHours" type="number" min="0"
          class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]" />
      </div>
      <div>
        <label class="mb-1 block text-xs font-medium text-[#8AC4ED]">{{ t('exchangeCourse.auditoryHours') }}</label>
        <input v-model.number="form.auditoryHours" type="number" min="0"
          class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]" />
      </div>
      <div>
        <label class="mb-1 block text-xs font-medium text-[#8AC4ED]">{{ t('exchangeCourse.labHours') }}</label>
        <input v-model.number="form.labHours" type="number" min="0"
          class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]" />
      </div>
    </div>

    <div>
      <label class="mb-1 block text-xs font-medium text-[#8AC4ED]">Status</label>
      <select
        v-model="form.status"
        class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]"
      >
        <option value="OriginallyEnrolled">{{ t('exchangeCourseStatus.OriginallyEnrolled') }}</option>
        <option value="Additional">{{ t('exchangeCourseStatus.Additional') }}</option>
      </select>
    </div>

    <div class="flex justify-end gap-2 pt-1">
      <button type="button" @click="emit('cancel')" class="rounded-lg px-3 py-1.5 text-sm text-[#8AC4ED] hover:text-white transition">
        {{ t('settings.institutions.cancel') }}
      </button>
      <button type="submit" class="rounded-lg bg-[#2E7AB5] px-3 py-1.5 text-sm font-medium text-white hover:bg-[#3A8FD0] transition">
        {{ t('settings.institutions.save') }}
      </button>
    </div>
  </form>
</template>
