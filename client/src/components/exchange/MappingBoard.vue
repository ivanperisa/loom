<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { exchangeService } from '@/services/exchange.service'
import type {
  ExchangeCourseResponse,
  CourseMappingResponse,
  MappingStatus
} from '@/types/exchange.types'
import type { CourseResponse } from '@/types/course.types'

const props = defineProps<{
  course: ExchangeCourseResponse
  exchangeId: string
  readonly?: boolean
}>()

const emit = defineEmits<{
  (e: 'updated', course: ExchangeCourseResponse): void
}>()

const { t } = useI18n()
const authStore = useAuthStore()

const isCoordinator = computed(() => authStore.role === 'Coordinator')

// Local pending rows (courseId + awardedEcts)
interface PendingRow { courseId: string; awardedEcts?: number; courseName: string; courseCode?: string }
const pendingRows = ref<PendingRow[]>([])

// Populate from existing Pending mappings
watch(() => props.course, (course) => {
  pendingRows.value = course.mappings
    .filter(m => m.status === 'Pending')
    .map(m => ({
      courseId: m.courseId,
      awardedEcts: m.awardedEcts,
      courseName: m.courseName,
      courseCode: m.courseCode
    }))
}, { immediate: true })

// Course search
const searchQuery = ref('')
const searchResults = ref<CourseResponse[]>([])
const searching = ref(false)
let searchTimeout: ReturnType<typeof setTimeout>

async function onSearch() {
  clearTimeout(searchTimeout)
  if (!searchQuery.value.trim()) {
    searchResults.value = []
    return
  }
  searchTimeout = setTimeout(async () => {
    searching.value = true
    try {
      const res = await exchangeService.getAvailableCourses(searchQuery.value)
      searchResults.value = res.data
    } finally {
      searching.value = false
    }
  }, 300)
}

function addCourse(course: CourseResponse) {
  if (pendingRows.value.some(r => r.courseId === course.id)) return
  pendingRows.value.push({ courseId: course.id, courseName: course.name, courseCode: course.code })
  searchQuery.value = ''
  searchResults.value = []
}

function removePending(idx: number) {
  pendingRows.value.splice(idx, 1)
}

// Total awarded ECTS warning
const totalAwardedEcts = computed(() =>
  pendingRows.value.reduce((sum, r) => sum + (r.awardedEcts ?? 0), 0)
)
const ectsWarning = computed(() =>
  props.course.ects != null && totalAwardedEcts.value > props.course.ects
)

const saving = ref(false)

async function saveMapping() {
  saving.value = true
  try {
    const res = await exchangeService.proposeMapping(props.exchangeId, props.course.id, {
      mappings: pendingRows.value.map(r => ({ courseId: r.courseId, awardedEcts: r.awardedEcts }))
    })
    emit('updated', res.data)
  } finally {
    saving.value = false
  }
}

// Coordinator review
const reviewingId = ref<string | null>(null)
const reviewForm = ref({ status: 'Approved' as 'Approved' | 'Rejected', coordinatorNote: '', awardedEcts: undefined as number | undefined, convertedGrade: '' })

function startReview(mapping: CourseMappingResponse) {
  reviewingId.value = mapping.id
  reviewForm.value = {
    status: 'Approved',
    coordinatorNote: mapping.coordinatorNote ?? '',
    awardedEcts: mapping.awardedEcts,
    convertedGrade: mapping.convertedGrade ?? ''
  }
}

const submittingReview = ref(false)
async function submitReview(mappingId: string) {
  submittingReview.value = true
  try {
    const res = await exchangeService.reviewMapping(props.exchangeId, props.course.id, mappingId, {
      status: reviewForm.value.status,
      coordinatorNote: reviewForm.value.coordinatorNote || undefined,
      awardedEcts: reviewForm.value.awardedEcts,
      convertedGrade: reviewForm.value.convertedGrade || undefined
    })
    // Update local course state with the returned mapping
    const updatedMapping = res.data
    const updatedCourse = {
      ...props.course,
      mappings: props.course.mappings.map(m => m.id === mappingId ? updatedMapping : m)
    }
    emit('updated', updatedCourse)
    reviewingId.value = null
  } finally {
    submittingReview.value = false
  }
}

const statusColors: Record<MappingStatus, string> = {
  Pending: 'bg-yellow-500/20 text-yellow-300 border-yellow-500/30',
  Approved: 'bg-green-500/20 text-green-300 border-green-500/30',
  Rejected: 'bg-red-500/20 text-red-300 border-red-500/30'
}

const nonPendingMappings = computed(() => props.course.mappings.filter(m => m.status !== 'Pending'))
</script>

<template>
  <div class="mt-3 space-y-3">
    <h4 class="text-sm font-semibold text-[#8AC4ED]">{{ t('mapping.title') }}</h4>

    <!-- Non-pending mappings (Approved / Rejected) -->
    <div v-for="m in nonPendingMappings" :key="m.id" class="rounded-lg border p-3 text-sm" :class="statusColors[m.status]">
      <div class="flex items-center justify-between gap-2">
        <div>
          <span class="font-medium text-[#CAE4F7]">{{ m.courseName }}</span>
          <span v-if="m.courseCode" class="ml-1 text-xs opacity-70">({{ m.courseCode }})</span>
          <span v-if="m.awardedEcts != null" class="ml-2 text-xs">{{ m.awardedEcts }} ECTS</span>
          <span v-if="m.convertedGrade" class="ml-2 text-xs">→ {{ m.convertedGrade }}</span>
        </div>
        <div class="flex items-center gap-2">
          <span class="rounded px-2 py-0.5 text-xs font-medium" :class="statusColors[m.status]">
            {{ t(`mappingStatus.${m.status}`) }}
          </span>
        </div>
      </div>
      <div v-if="m.coordinatorNote" class="mt-1 text-xs opacity-80">{{ m.coordinatorNote }}</div>

      <!-- Coordinator review form -->
      <div v-if="isCoordinator && m.status === 'Approved'" class="mt-2">
        <button @click="startReview(m)" v-if="reviewingId !== m.id"
          class="text-xs text-[#8AC4ED] hover:text-white transition">
          {{ t('exchangeCourse.edit') }}
        </button>
        <div v-else class="mt-2 space-y-2 border-t border-white/10 pt-2">
          <div class="grid grid-cols-2 gap-2">
            <div>
              <label class="mb-1 block text-xs text-[#8AC4ED]">{{ t('mapping.awardedEcts') }}</label>
              <input v-model.number="reviewForm.awardedEcts" type="number" step="0.5" min="0"
                class="w-full rounded bg-[#071C2C] px-2 py-1 text-xs text-[#CAE4F7] border border-[#1E4A6E]" />
            </div>
            <div>
              <label class="mb-1 block text-xs text-[#8AC4ED]">{{ t('mapping.convertedGrade') }}</label>
              <input v-model="reviewForm.convertedGrade" type="text"
                class="w-full rounded bg-[#071C2C] px-2 py-1 text-xs text-[#CAE4F7] border border-[#1E4A6E]" />
            </div>
          </div>
          <div>
            <label class="mb-1 block text-xs text-[#8AC4ED]">{{ t('mapping.coordinatorNote') }}</label>
            <input v-model="reviewForm.coordinatorNote" type="text"
              class="w-full rounded bg-[#071C2C] px-2 py-1 text-xs text-[#CAE4F7] border border-[#1E4A6E]" />
          </div>
          <div class="flex gap-2">
            <button @click="submitReview(m.id)" :disabled="submittingReview"
              class="rounded bg-green-700 px-2 py-1 text-xs text-white hover:bg-green-600 transition disabled:opacity-50">
              {{ t('settings.institutions.save') }}
            </button>
            <button @click="reviewingId = null"
              class="rounded px-2 py-1 text-xs text-[#8AC4ED] hover:text-white transition">
              {{ t('settings.institutions.cancel') }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Coordinator: review pending -->
    <template v-if="isCoordinator">
      <div v-for="m in course.mappings.filter(x => x.status === 'Pending')" :key="m.id"
        class="rounded-lg border border-yellow-500/30 bg-yellow-500/10 p-3 text-sm">
        <div class="flex items-center justify-between gap-2">
          <div>
            <span class="font-medium text-[#CAE4F7]">{{ m.courseName }}</span>
            <span v-if="m.courseCode" class="ml-1 text-xs opacity-70">({{ m.courseCode }})</span>
            <span v-if="m.awardedEcts != null" class="ml-2 text-xs text-yellow-300">{{ m.awardedEcts }} ECTS</span>
          </div>
          <span class="rounded px-2 py-0.5 text-xs font-medium bg-yellow-500/20 text-yellow-300">
            {{ t('mappingStatus.Pending') }}
          </span>
        </div>
        <div v-if="reviewingId !== m.id" class="mt-2 flex gap-2">
          <button @click="startReview(m)"
            class="rounded bg-green-700 px-2 py-1 text-xs text-white hover:bg-green-600 transition">
            {{ t('mapping.approve') }} / {{ t('mapping.reject') }}
          </button>
        </div>
        <div v-else class="mt-2 space-y-2 border-t border-white/10 pt-2">
          <div class="flex gap-2">
            <button @click="reviewForm.status = 'Approved'"
              :class="reviewForm.status === 'Approved' ? 'bg-green-700 text-white' : 'bg-[#071C2C] text-[#8AC4ED]'"
              class="rounded px-2 py-1 text-xs transition border border-green-700">
              {{ t('mapping.approve') }}
            </button>
            <button @click="reviewForm.status = 'Rejected'"
              :class="reviewForm.status === 'Rejected' ? 'bg-red-700 text-white' : 'bg-[#071C2C] text-[#8AC4ED]'"
              class="rounded px-2 py-1 text-xs transition border border-red-700">
              {{ t('mapping.reject') }}
            </button>
          </div>
          <div class="grid grid-cols-2 gap-2">
            <div>
              <label class="mb-1 block text-xs text-[#8AC4ED]">{{ t('mapping.awardedEcts') }}</label>
              <input v-model.number="reviewForm.awardedEcts" type="number" step="0.5" min="0"
                class="w-full rounded bg-[#071C2C] px-2 py-1 text-xs text-[#CAE4F7] border border-[#1E4A6E]" />
            </div>
            <div>
              <label class="mb-1 block text-xs text-[#8AC4ED]">{{ t('mapping.convertedGrade') }}</label>
              <input v-model="reviewForm.convertedGrade" type="text"
                class="w-full rounded bg-[#071C2C] px-2 py-1 text-xs text-[#CAE4F7] border border-[#1E4A6E]" />
            </div>
          </div>
          <div>
            <label class="mb-1 block text-xs text-[#8AC4ED]">{{ t('mapping.coordinatorNote') }}</label>
            <input v-model="reviewForm.coordinatorNote" type="text"
              class="w-full rounded bg-[#071C2C] px-2 py-1 text-xs text-[#CAE4F7] border border-[#1E4A6E]" />
          </div>
          <div class="flex gap-2">
            <button @click="submitReview(m.id)" :disabled="submittingReview"
              class="rounded bg-[#2E7AB5] px-2 py-1 text-xs text-white hover:bg-[#3A8FD0] transition disabled:opacity-50">
              {{ t('settings.institutions.save') }}
            </button>
            <button @click="reviewingId = null"
              class="rounded px-2 py-1 text-xs text-[#8AC4ED] hover:text-white transition">
              {{ t('settings.institutions.cancel') }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- Student: edit pending mappings -->
    <template v-if="!isCoordinator && !readonly">
      <div v-for="(row, idx) in pendingRows" :key="row.courseId"
        class="flex items-center gap-2 rounded-lg border border-yellow-500/20 bg-yellow-500/5 px-3 py-2">
        <div class="flex-1 text-sm text-[#CAE4F7]">
          {{ row.courseName }}
          <span v-if="row.courseCode" class="ml-1 text-xs text-[#5A8AAD]">({{ row.courseCode }})</span>
        </div>
        <input
          v-model.number="row.awardedEcts"
          type="number"
          step="0.5"
          min="0"
          :placeholder="t('mapping.awardedEcts')"
          class="w-24 rounded bg-[#071C2C] px-2 py-1 text-xs text-[#CAE4F7] border border-[#1E4A6E]"
        />
        <button @click="removePending(idx)" class="text-xs text-red-400 hover:text-red-300 transition">✕</button>
      </div>

      <!-- ECTS warning -->
      <div v-if="ectsWarning" class="rounded-lg bg-amber-900/30 px-3 py-2 text-xs text-amber-300">
        ⚠ {{ t('mapping.ectsWarning') }}
      </div>

      <!-- Course search -->
      <div class="relative">
        <input
          v-model="searchQuery"
          @input="onSearch"
          type="text"
          :placeholder="t('mapping.addMapping') + '...'"
          class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] border border-dashed border-[#1E4A6E] focus:outline-none focus:ring-1 focus:ring-[#2E7AB5]"
        />
        <div v-if="searchResults.length > 0"
          class="absolute z-10 mt-1 w-full rounded-lg bg-[#0E2A3D] border border-[#1E4A6E] shadow-xl max-h-48 overflow-y-auto">
          <button
            v-for="c in searchResults"
            :key="c.id"
            @click="addCourse(c)"
            class="flex w-full items-center justify-between px-3 py-2 text-sm text-[#CAE4F7] hover:bg-[#1E4A6E] transition text-left"
          >
            <span>{{ c.name }}</span>
            <span class="text-xs text-[#5A8AAD]">{{ c.code }} · {{ c.ects }} ECTS</span>
          </button>
        </div>
      </div>

      <!-- Save mapping button -->
      <button
        v-if="pendingRows.length > 0"
        @click="saveMapping"
        :disabled="saving"
        class="mt-1 rounded-lg bg-[#2E7AB5] px-4 py-2 text-sm font-medium text-white hover:bg-[#3A8FD0] transition disabled:opacity-50"
      >
        {{ saving ? t('common.loading') : t('mapping.save') }}
      </button>

      <p v-if="pendingRows.length === 0 && nonPendingMappings.length === 0" class="text-xs text-[#5A8AAD]">
        {{ t('mapping.noMappings') }}
      </p>
    </template>
  </div>

</template>
