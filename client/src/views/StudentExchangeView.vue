<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import CreateExchangeModal from '@/components/exchange/CreateExchangeModal.vue'
import ExchangeCourseCard from '@/components/exchange/ExchangeCourseCard.vue'
import ExchangeCourseForm from '@/components/exchange/ExchangeCourseForm.vue'
import { useExchangeStore } from '@/stores/exchange.store'
import type { CreateExchangeRequest, ExchangeCourseResponse } from '@/types/exchange.types'

const { t } = useI18n()
const router = useRouter()
const exchangeStore = useExchangeStore()

const showCreateModal = ref(false)
const showAddCourse = ref(false)

onMounted(() => exchangeStore.fetchMyExchange())

const exchange = computed(() => exchangeStore.exchange)
const loading = computed(() => exchangeStore.loading)
const error = computed(() => exchangeStore.error)

const isEditable = computed(() =>
  exchange.value?.status === 'Draft' || exchange.value?.status === 'Submitted'
)

const canEditCourses = computed(() =>
  exchange.value?.status === 'Draft'
)

const statusColors: Record<string, string> = {
  Draft: 'bg-gray-500/20 text-gray-300',
  Submitted: 'bg-blue-500/20 text-blue-300',
  Approved: 'bg-green-500/20 text-green-300',
  Rejected: 'bg-red-500/20 text-red-300',
  Completed: 'bg-purple-500/20 text-purple-300'
}

async function handleCreate(data: CreateExchangeRequest) {
  try {
    await exchangeStore.createExchange(data)
    showCreateModal.value = false
  } catch (e: any) {
    // errors handled by store
  }
}

async function handleDelete() {
  if (!exchange.value) return
  if (!confirm(t('exchange.deleteConfirm'))) return
  try {
    await exchangeStore.deleteExchange(exchange.value.id)
  } catch {
    // ignore
  }
}

async function handleRetract() {
  if (!exchange.value) return
  if (!confirm(t('exchange.retractConfirm'))) return
  try {
    await exchangeStore.retract(exchange.value.id)
  } catch {
    // ignore
  }
}

async function handleSubmit() {
  if (!exchange.value) return
  if (!confirm(t('exchange.submitConfirm'))) return
  try {
    await exchangeStore.submitForReview(exchange.value.id)
  } catch {
    // ignore
  }
}

async function handleAddCourse(data: any) {
  if (!exchange.value) return
  try {
    await exchangeStore.addCourse(exchange.value.id, data)
    showAddCourse.value = false
  } catch {
    // ignore
  }
}

async function handleUpdateCourse(courseId: string, data: any) {
  if (!exchange.value) return
  await exchangeStore.updateCourse(exchange.value.id, courseId, data)
}

async function handleRemoveCourse(courseId: string) {
  if (!exchange.value) return
  if (!confirm(t('exchangeCourse.removeConfirm'))) return
  await exchangeStore.removeCourse(exchange.value.id, courseId)
}

async function handleUpdateGrades(courseId: string, data: any) {
  if (!exchange.value) return
  await exchangeStore.updateGrades(exchange.value.id, courseId, data)
}

function handleCourseUpdated(updated: ExchangeCourseResponse) {
  exchangeStore.updateCourseById(updated)
}

function openMappingBoard() {
  router.push({ name: 'exchange-mapping' })
}
</script>

<template>
  <section class="mx-auto max-w-4xl px-6 py-8">
    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-16 text-[#8AC4ED]">
      {{ t('common.loading') }}
    </div>

    <!-- Error -->
    <div v-else-if="error" class="rounded-xl bg-red-900/30 px-6 py-4 text-red-300">
      {{ error }}
    </div>

    <!-- No exchange -->
    <div v-else-if="!exchange" class="flex flex-col items-center justify-center py-24 text-center">
      <p class="mb-6 text-lg text-[#8AC4ED]">{{ t('exchange.noExchange') }}</p>
      <button
        @click="showCreateModal = true"
        class="rounded-xl bg-[#2E7AB5] px-6 py-3 font-medium text-white hover:bg-[#3A8FD0] transition"
      >
        {{ t('exchange.create') }}
      </button>
    </div>

    <!-- Exchange exists -->
    <div v-else>
      <!-- Header -->
      <div class="mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-[#CAE4F7]">{{ t('exchange.title') }}</h1>
          <div class="mt-1 flex flex-wrap items-center gap-3 text-sm text-[#8AC4ED]">
            <span>{{ exchange.foreignInstitution.nameEn }}</span>
            <span>·</span>
            <span>{{ exchange.academicYear }}</span>
            <span>·</span>
            <span>{{ t(`exchangeSemesters.${exchange.semester}`) }}</span>
            <span v-if="exchange.durationMonths">· {{ exchange.durationMonths }} {{ t('exchange.duration').toLowerCase() }}</span>
            <span v-if="exchange.mentor">· {{ exchange.mentor }}</span>
          </div>
        </div>
        <div class="flex items-center gap-3">
          <span
            :class="[statusColors[exchange.status] ?? 'bg-gray-500/20 text-gray-300', 'rounded-full px-3 py-1 text-sm font-medium']"
          >
            {{ t(`exchangeStatus.${exchange.status}`) }}
          </span>
          <button
            v-if="exchange.status === 'Submitted'"
            @click="handleRetract"
            class="rounded-lg border border-[#1E4A6E] px-4 py-1.5 text-sm font-medium text-[#8AC4ED] hover:text-[#CAE4F7] transition"
          >
            {{ t('exchange.retract') }}
          </button>
          <button
            v-if="exchange.status === 'Draft'"
            @click="handleSubmit"
            class="rounded-lg bg-[#2E7AB5] px-4 py-1.5 text-sm font-medium text-white hover:bg-[#3A8FD0] transition"
          >
            {{ t('exchange.submit') }}
          </button>
          <button
            v-if="exchange.status === 'Draft'"
            @click="handleDelete"
            class="rounded-lg border border-red-900/50 px-3 py-1.5 text-sm text-red-400 hover:text-red-300 transition"
          >
            {{ t('settings.institutions.remove') }}
          </button>
        </div>
      </div>

      <!-- Mapping board link -->
      <div class="mb-6">
        <button
          @click="openMappingBoard"
          class="rounded-lg border border-[#218CD9] px-4 py-2 text-sm font-medium text-[#8AC4ED] hover:bg-[#218CD9]/10 hover:text-[#CAE4F7] transition"
        >
          {{ t('coordinator.openMappingBoard') }} &rarr;
        </button>
      </div>

      <!-- Courses -->
      <div class="space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-semibold text-[#CAE4F7]">
            Learning Agreement
          </h2>
          <button
            v-if="canEditCourses"
            @click="showAddCourse = !showAddCourse"
            class="rounded-lg bg-[#1E4A6E] px-3 py-1.5 text-sm text-[#CAE4F7] hover:bg-[#2E5A7E] transition"
          >
            + {{ t('exchangeCourse.add') }}
          </button>
        </div>

        <!-- Add course form -->
        <div v-if="showAddCourse" class="rounded-xl bg-[#0E2A3D] border border-[#1E4A6E] p-4">
          <h3 class="mb-3 text-sm font-semibold text-[#8AC4ED]">{{ t('exchangeCourse.add') }}</h3>
          <ExchangeCourseForm
            @submit="handleAddCourse"
            @cancel="showAddCourse = false"
          />
        </div>

        <!-- Course cards -->
        <ExchangeCourseCard
          v-for="course in exchange.courses"
          :key="course.id"
          :course="course"
          :exchange-id="exchange.id"
          :readonly="!isEditable"
          :can-edit-structure="canEditCourses"
          @update="(data) => handleUpdateCourse(course.id, data)"
          @remove="handleRemoveCourse(course.id)"
          @update-grades="(data) => handleUpdateGrades(course.id, data)"
          @course-updated="handleCourseUpdated"
        />

        <div v-if="exchange.courses.length === 0" class="py-8 text-center text-sm text-[#5A8AAD]">
          {{ t('mapping.noMappings') }}
        </div>
      </div>
    </div>
  </section>

  <!-- Create exchange modal -->
  <CreateExchangeModal
    v-if="showCreateModal"
    @submit="handleCreate"
    @close="showCreateModal = false"
  />
</template>
