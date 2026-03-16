<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import ExchangeCourseForm from './ExchangeCourseForm.vue'
import GradesForm from './GradesForm.vue'
import MappingBoard from './MappingBoard.vue'
import type { ExchangeCourseResponse, UpsertExchangeCourseRequest, UpdateGradesRequest } from '@/types/exchange.types'

const props = defineProps<{
  course: ExchangeCourseResponse
  exchangeId: string
  readonly?: boolean
  canEditStructure?: boolean
}>()

const emit = defineEmits<{
  (e: 'update', data: UpsertExchangeCourseRequest): void
  (e: 'remove'): void
  (e: 'updateGrades', data: UpdateGradesRequest): void
  (e: 'courseUpdated', course: ExchangeCourseResponse): void
}>()

const { t } = useI18n()

const showEdit = ref(false)
const showGrades = ref(false)
</script>

<template>
  <div class="rounded-xl bg-[#0E2A3D] border border-[#1E4A6E] p-4">
    <div class="flex items-start justify-between gap-2">
      <div class="flex-1">
        <div class="flex items-center gap-2 flex-wrap">
          <h3 class="font-semibold text-[#CAE4F7]">{{ course.nameEn }}</h3>
          <span v-if="course.code" class="rounded bg-[#071C2C] px-1.5 py-0.5 text-xs text-[#5A8AAD]">{{ course.code }}</span>
          <span class="rounded border border-[#1E4A6E] px-1.5 py-0.5 text-xs text-[#8AC4ED]">
            {{ t(`exchangeCourseStatus.${course.status}`) }}
          </span>
        </div>
        <div v-if="course.nameHr" class="mt-0.5 text-sm text-[#5A8AAD]">{{ course.nameHr }}</div>
        <div class="mt-1 flex flex-wrap gap-3 text-xs text-[#5A8AAD]">
          <span v-if="course.ects != null">{{ course.ects }} ECTS</span>
          <span v-if="course.lecturesHours != null || course.auditoryHours != null || course.labHours != null">
            P/A/L: {{ course.lecturesHours ?? 0 }}/{{ course.auditoryHours ?? 0 }}/{{ course.labHours ?? 0 }}
          </span>
        </div>
        <!-- Grades -->
        <div v-if="course.originalGrade || course.ectsGrade || course.examDate" class="mt-2 flex flex-wrap gap-3 text-xs text-[#8AC4ED]">
          <span v-if="course.originalGrade">{{ t('exchangeCourse.originalGrade') }}: <strong class="text-[#CAE4F7]">{{ course.originalGrade }}</strong></span>
          <span v-if="course.ectsGrade">{{ t('exchangeCourse.ectsGrade') }}: <strong class="text-[#CAE4F7]">{{ course.ectsGrade }}</strong></span>
          <span v-if="course.examDate">{{ t('exchangeCourse.examDate') }}: <strong class="text-[#CAE4F7]">{{ course.examDate }}</strong></span>
        </div>
      </div>

      <div v-if="!readonly || canEditStructure" class="flex shrink-0 gap-2">
        <button v-if="!readonly" @click="showGrades = !showGrades" class="rounded px-2 py-1 text-xs text-[#8AC4ED] hover:text-white transition border border-[#1E4A6E] hover:border-[#2E7AB5]">
          {{ t('exchangeCourse.addGrades') }}
        </button>
        <button v-if="canEditStructure" @click="showEdit = !showEdit" class="rounded px-2 py-1 text-xs text-[#8AC4ED] hover:text-white transition border border-[#1E4A6E] hover:border-[#2E7AB5]">
          {{ t('exchangeCourse.edit') }}
        </button>
        <button v-if="canEditStructure" @click="emit('remove')" class="rounded px-2 py-1 text-xs text-red-400 hover:text-red-300 transition border border-red-900/40 hover:border-red-500/40">
          ✕
        </button>
      </div>
    </div>

    <!-- Edit form -->
    <div v-if="showEdit" class="mt-4 border-t border-[#1E4A6E] pt-4">
      <ExchangeCourseForm
        :initial="course"
        @submit="(data) => { emit('update', data); showEdit = false }"
        @cancel="showEdit = false"
      />
    </div>

    <!-- Grades form -->
    <div v-if="showGrades" class="mt-4 border-t border-[#1E4A6E] pt-4">
      <GradesForm
        :course="course"
        @submit="(data) => { emit('updateGrades', data); showGrades = false }"
        @cancel="showGrades = false"
      />
    </div>

    <!-- Mapping board -->
    <MappingBoard
      :course="course"
      :exchange-id="exchangeId"
      :readonly="readonly"
      @updated="(updated) => emit('courseUpdated', updated)"
    />
  </div>
</template>
