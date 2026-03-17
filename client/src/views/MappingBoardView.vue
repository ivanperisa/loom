<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import AppHeader from '@/components/AppHeader.vue'
import { useAuthStore } from '@/stores/auth.store'
import { useExchangeStore } from '@/stores/exchange.store'
import { exchangeService } from '@/services/exchange.service'
import type {
  FerCourseResponse,
  MappingBoardResponse,
  MappingRowResponse,
  MappingStatus,
  ProposeBoardMappingRequest
} from '@/types/exchange.types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()

const isCoordinator = computed(() => authStore.role === 'Coordinator')

const exchangeId = computed<string | null>(() => {
  if (isCoordinator.value) {
    return (route.query.exchangeId as string) ?? null
  }
  return exchangeStore.exchange?.id ?? null
})

interface LocalMappingRow {
  ferCourseId: string
  ferCourseName: string
  ferCourseCode?: string
  awardedEcts?: number
  convertedGrade?: string
  coordinatorNote?: string
  status: MappingStatus
  isNew: boolean
}

interface LocalExchangeCourse {
  exchangeCourseId: string
  mappings: LocalMappingRow[]
}

const board = ref<MappingBoardResponse | null>(null)
const localMappings = ref<LocalExchangeCourse[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const saving = ref(false)
const hasChanges = ref(false)

const draggedCourse = ref<FerCourseResponse | null>(null)
const dragOverId = ref<string | null>(null)

onMounted(async () => {
  if (!isCoordinator.value && !exchangeStore.exchange) {
    await exchangeStore.fetchMyExchange()
  }
  await fetchBoard()
})

watch(exchangeId, () => fetchBoard())

async function fetchBoard() {
  if (!exchangeId.value) return
  loading.value = true
  error.value = null
  try {
    const res = await exchangeService.getMappingBoard(exchangeId.value)
    board.value = res.data
    initLocalMappings(res.data)
    hasChanges.value = false
  } catch (e: any) {
    error.value = e.response?.data?.title ?? t('common.error')
  } finally {
    loading.value = false
  }
}

function initLocalMappings(data: MappingBoardResponse) {
  localMappings.value = data.exchangeCourses.map(ec => ({
    exchangeCourseId: ec.id,
    mappings: ec.mappings.map(m => ({
      ferCourseId: m.ferCourseId,
      ferCourseName: m.ferCourseName,
      ferCourseCode: m.ferCourseCode,
      awardedEcts: m.awardedEcts,
      convertedGrade: m.convertedGrade,
      coordinatorNote: m.coordinatorNote,
      status: m.status,
      isNew: false
    }))
  }))
}

function localizedName(course: { name: string; nameEn: string }): string {
  return course.nameEn || course.name
}

// Drag & drop
function onDragStart(course: FerCourseResponse) {
  draggedCourse.value = course
}

function onDragOver(exchangeCourseId: string) {
  dragOverId.value = exchangeCourseId
}

function onDragLeave() {
  dragOverId.value = null
}

function onDrop(exchangeCourseId: string) {
  if (!draggedCourse.value) return
  addMapping(exchangeCourseId, draggedCourse.value)
  draggedCourse.value = null
  dragOverId.value = null
}

function addMapping(exchangeCourseId: string, ferCourse: FerCourseResponse) {
  const ec = localMappings.value.find(c => c.exchangeCourseId === exchangeCourseId)
  if (!ec) return
  if (ec.mappings.some(m => m.ferCourseId === ferCourse.id)) return
  ec.mappings.push({
    ferCourseId: ferCourse.id,
    ferCourseName: ferCourse.name,
    ferCourseCode: ferCourse.code,
    awardedEcts: undefined,
    status: 'Pending',
    isNew: true
  })
  hasChanges.value = true
}

function removeMapping(exchangeCourseId: string, ferCourseId: string) {
  const ec = localMappings.value.find(c => c.exchangeCourseId === exchangeCourseId)
  if (!ec) return
  ec.mappings = ec.mappings.filter(m =>
    m.ferCourseId !== ferCourseId || m.status === 'Approved' || m.status === 'Rejected'
  )
  hasChanges.value = true
}

function isMapped(ferCourseId: string): boolean {
  return localMappings.value.some(ec =>
    ec.mappings.some(m => m.ferCourseId === ferCourseId)
  )
}

function ectsWarning(exchangeCourseId: string, totalEcts?: number): boolean {
  if (!totalEcts) return false
  const ec = localMappings.value.find(c => c.exchangeCourseId === exchangeCourseId)
  if (!ec) return false
  const sum = ec.mappings.reduce((acc, m) => acc + (m.awardedEcts ?? 0), 0)
  return sum > totalEcts
}

function localForExchangeCourse(exchangeCourseId: string): LocalMappingRow[] {
  return localMappings.value.find(c => c.exchangeCourseId === exchangeCourseId)?.mappings ?? []
}

function statusColor(status: MappingStatus): string {
  switch (status) {
    case 'Pending': return 'border-[#218CD9]/40 bg-[#218CD9]/10 text-[#8AC4ED]'
    case 'Approved': return 'border-green-500/40 bg-green-500/10 text-green-300'
    case 'Rejected': return 'border-red-500/40 bg-red-500/10 text-red-300'
    default: return 'border-[#1E4A6E] bg-[#071C2C] text-[#5A8AAD]'
  }
}

async function proposeMapping() {
  if (!exchangeId.value) return
  saving.value = true
  error.value = null
  try {
    const payload: ProposeBoardMappingRequest = {
      courses: localMappings.value.map(ec => ({
        exchangeCourseId: ec.exchangeCourseId,
        mappings: ec.mappings
          .filter(m => m.status === 'Pending')
          .map(m => ({
            ferCourseId: m.ferCourseId,
            awardedEcts: m.awardedEcts,
            convertedGrade: m.convertedGrade,
            coordinatorNote: m.coordinatorNote
          }))
      }))
    }
    const res = await exchangeService.proposeBoardMapping(exchangeId.value, payload)
    board.value = res.data
    initLocalMappings(res.data)
    hasChanges.value = false
  } catch (e: any) {
    error.value = e.response?.data?.title ?? t('common.error')
  } finally {
    saving.value = false
  }
}

function goBack() {
  router.push({ name: 'exchange' })
}
</script>

<template>
  <main class="flex h-screen flex-col bg-[#071C2C]">
    <AppHeader />

    <!-- Top bar -->
    <div class="flex items-center justify-between border-b border-[#1E4A6E] px-6 py-3">
      <div class="flex items-center gap-4">
        <button @click="goBack" class="text-sm font-medium text-[#8AC4ED] hover:text-[#CAE4F7] transition">
          &larr; {{ t('coordinator.backToList') }}
        </button>
        <h1 class="text-lg font-semibold text-[#CAE4F7]">{{ t('mappingBoard.title') }}</h1>
      </div>
      <div class="flex items-center gap-4">
        <span v-if="hasChanges" class="text-xs text-yellow-400">{{ t('mappingBoard.unsavedChanges') }}</span>
        <button
          @click="proposeMapping"
          :disabled="saving || !hasChanges"
          class="rounded-lg bg-[#2E7AB5] px-4 py-2 text-sm font-medium text-white hover:bg-[#3A8FD0] transition disabled:opacity-50"
        >
          {{ isCoordinator ? t('mappingBoard.proposeCoordinator') : t('mappingBoard.proposeStudent') }}
        </button>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex flex-1 items-center justify-center text-[#8AC4ED]">
      {{ t('common.loading') }}
    </div>

    <!-- Error -->
    <div v-else-if="error" class="p-6">
      <div class="rounded-xl bg-red-900/30 px-6 py-4 text-red-300">{{ error }}</div>
    </div>

    <!-- Board -->
    <div v-else-if="board" class="flex flex-1 overflow-hidden">
      <!-- Left: FER courses -->
      <div class="w-2/5 overflow-y-auto border-r border-[#1E4A6E] p-4">
        <h2 class="mb-4 text-sm font-semibold uppercase tracking-wider text-[#5A8AAD]">
          {{ t('mappingBoard.ferCourses') }}
        </h2>

        <div v-for="group in board.ferCourseGroups" :key="group.type" class="mb-5">
          <h3 class="mb-2 text-xs font-semibold uppercase tracking-wider text-[#218CD9]">
            {{ t(`courseTypes.${group.type}`) }}
          </h3>
          <div class="space-y-2">
            <div
              v-for="course in group.courses"
              :key="course.id"
              draggable="true"
              @dragstart="onDragStart(course)"
              @dragend="draggedCourse = null"
              :class="[
                'cursor-grab select-none rounded-lg border px-3 py-2 transition-colors',
                isMapped(course.id)
                  ? 'border-green-500/40 bg-[#071C2C]'
                  : 'border-[#1E4A6E] bg-[#071C2C] hover:border-[#218CD9]',
                draggedCourse?.id === course.id ? 'opacity-40' : ''
              ]"
            >
              <div class="flex items-center justify-between gap-2">
                <span class="text-sm text-[#CAE4F7]">{{ localizedName(course) }}</span>
                <span class="text-xs text-[#5A8AAD]">{{ course.ects }} ECTS</span>
              </div>
              <div class="mt-0.5 flex items-center gap-2">
                <span v-if="course.code" class="text-xs text-[#5A8AAD]">{{ course.code }}</span>
                <span v-if="isMapped(course.id)" class="text-xs text-green-400">{{ t('mappingBoard.mapped') }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Right: Exchange courses -->
      <div class="w-3/5 overflow-y-auto p-4">
        <h2 class="mb-4 text-sm font-semibold uppercase tracking-wider text-[#5A8AAD]">
          {{ t('mappingBoard.foreignCourses') }}
        </h2>

        <div v-if="board.exchangeCourses.length === 0" class="py-16 text-center text-sm text-[#5A8AAD]">
          {{ t('mappingBoard.noMappings') }}
        </div>

        <div v-for="ec in board.exchangeCourses" :key="ec.id" class="mb-6 rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-4">
          <!-- Exchange course header -->
          <div class="mb-3 flex items-center justify-between">
            <div>
              <div class="flex items-center gap-2">
                <span v-if="ec.code" class="text-xs text-[#5A8AAD]">{{ ec.code }}</span>
                <span class="text-sm font-semibold text-[#CAE4F7]">{{ localizedName(ec) }}</span>
              </div>
              <div class="mt-0.5 flex items-center gap-2 text-xs text-[#5A8AAD]">
                <span v-if="ec.ects">{{ ec.ects }} ECTS</span>
                <span>{{ t(`exchangeCourseStatus.${ec.status}`) }}</span>
              </div>
            </div>
            <span
              v-if="ectsWarning(ec.id, ec.ects)"
              class="text-xs text-yellow-400"
            >{{ t('mappingBoard.ectsWarning') }}</span>
          </div>

          <!-- Mapped rows -->
          <table v-if="localForExchangeCourse(ec.id).length > 0" class="mb-3 w-full">
            <tbody>
              <tr
                v-for="mapping in localForExchangeCourse(ec.id)"
                :key="mapping.ferCourseId"
                class="border-b border-[#1E4A6E]/50 last:border-0"
              >
                <td class="py-1.5 pr-2 text-xs text-[#5A8AAD]">{{ mapping.ferCourseCode ?? '—' }}</td>
                <td class="py-1.5 pr-2 text-sm text-[#CAE4F7]">{{ mapping.ferCourseName }}</td>
                <td class="py-1.5 pr-2">
                  <input
                    v-if="mapping.status === 'Pending'"
                    type="number" min="0" max="30" step="0.5"
                    v-model.number="mapping.awardedEcts"
                    @input="hasChanges = true"
                    class="w-16 rounded bg-[#071C2C] border border-[#1E4A6E] px-2 py-0.5 text-sm text-[#CAE4F7] text-center"
                  />
                  <span v-else class="text-sm text-[#CAE4F7]">{{ mapping.awardedEcts ?? '—' }}</span>
                </td>
                <!-- Coordinator: grade + note -->
                <template v-if="isCoordinator && mapping.status === 'Pending'">
                  <td class="py-1.5 pr-2">
                    <input
                      v-model="mapping.convertedGrade"
                      @input="hasChanges = true"
                      :placeholder="t('mapping.convertedGrade')"
                      class="w-20 rounded bg-[#071C2C] border border-[#1E4A6E] px-2 py-0.5 text-xs text-[#CAE4F7]"
                    />
                  </td>
                  <td class="py-1.5 pr-2">
                    <input
                      v-model="mapping.coordinatorNote"
                      @input="hasChanges = true"
                      :placeholder="t('mapping.coordinatorNote')"
                      class="w-32 rounded bg-[#071C2C] border border-[#1E4A6E] px-2 py-0.5 text-xs text-[#CAE4F7]"
                    />
                  </td>
                </template>
                <td class="py-1.5 pr-2">
                  <span :class="[statusColor(mapping.status), 'rounded-full border px-2 py-0.5 text-xs']">
                    {{ t(`mappingStatus.${mapping.status}`) }}
                  </span>
                </td>
                <td class="py-1.5">
                  <button
                    v-if="mapping.status === 'Pending'"
                    @click="removeMapping(ec.id, mapping.ferCourseId)"
                    class="text-[#5A8AAD] hover:text-red-400 transition text-xs"
                  >&times;</button>
                </td>
              </tr>
            </tbody>
          </table>

          <!-- Drop zone -->
          <div
            @dragover.prevent="onDragOver(ec.id)"
            @dragleave="onDragLeave"
            @drop="onDrop(ec.id)"
            :class="[
              'rounded-lg border-2 border-dashed px-4 py-3 text-center text-xs transition-colors',
              dragOverId === ec.id
                ? 'border-[#218CD9] bg-[#218CD9]/10 text-[#8AC4ED]'
                : 'border-[#1E4A6E] text-[#5A8AAD]'
            ]"
          >
            {{ t('mappingBoard.dropHere') }}
          </div>
        </div>
      </div>
    </div>
  </main>
</template>
