<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { ExchangeCourseResponse, UpdateGradesRequest } from '@/types/exchange.types'

const props = defineProps<{
  course: ExchangeCourseResponse
}>()

const emit = defineEmits<{
  (e: 'submit', data: UpdateGradesRequest): void
  (e: 'cancel'): void
}>()

const { t } = useI18n()

const form = ref<UpdateGradesRequest>({
  originalGrade: props.course.originalGrade ?? '',
  ectsGrade: props.course.ectsGrade ?? '',
  examDate: props.course.examDate ?? ''
})
</script>

<template>
  <form @submit.prevent="emit('submit', { ...form })" class="space-y-3">
    <div>
      <label class="mb-1 block text-xs font-medium text-[#8AC4ED]">{{ t('exchangeCourse.originalGrade') }}</label>
      <input
        v-model="form.originalGrade"
        type="text"
        class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]"
      />
    </div>
    <div>
      <label class="mb-1 block text-xs font-medium text-[#8AC4ED]">{{ t('exchangeCourse.ectsGrade') }}</label>
      <input
        v-model="form.ectsGrade"
        type="text"
        maxlength="5"
        class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]"
      />
    </div>
    <div>
      <label class="mb-1 block text-xs font-medium text-[#8AC4ED]">{{ t('exchangeCourse.examDate') }}</label>
      <input
        v-model="form.examDate"
        type="date"
        class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]"
      />
    </div>
    <div class="flex justify-end gap-2 pt-1">
      <button type="button" @click="emit('cancel')" class="rounded-lg px-3 py-1.5 text-sm text-[#8AC4ED] hover:text-white transition">
        {{ t('settings.institutions.cancel') }}
      </button>
      <button type="submit" class="rounded-lg bg-[#2E7AB5] px-3 py-1.5 text-sm font-medium text-white hover:bg-[#3A8FD0] transition">
        {{ t('exchangeCourse.saveGrades') }}
      </button>
    </div>
  </form>
</template>
