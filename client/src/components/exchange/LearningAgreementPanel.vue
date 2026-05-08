<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import { useI18n } from 'vue-i18n'
import ForeignCoursePanel from '@/components/exchange/ForeignCoursePanel.vue'
import StatusBadge from '@/components/common/StatusBadge.vue'
import UnsavedChangesBar from '@/components/common/UnsavedChangesBar.vue'
import { useExchangeStore } from '@/stores/exchange.store'
import { useExchangePermissions } from '@/composables/useExchangePermissions'
import type { CourseSlotResponse, LocalSlotMapping, SlotMode } from '@/types/exchange.types'
import type { ForeignCourseResponse } from '@/types/institution.types'

const props = defineProps<{
  exchangeId: string
  studyProfileName: string
}>()

const { t, locale } = useI18n()
const exchangeStore = useExchangeStore()
const { isCoordinator, isEditable } = useExchangePermissions()

const isSavingLa = ref(false)
const saveError = ref<string | null>(null)
const coordinatorMessage = ref('')
const isEditingMessage = ref(false)
const isSavingMessage = ref(false)

async function saveLa() {
  isSavingLa.value = true
  saveError.value = null
  try {
    await exchangeStore.saveLearningAgreement(props.exchangeId)
  } catch {
    saveError.value = t('la.saveError')
  } finally {
    isSavingLa.value = false
  }
}

async function discardLa() {
  await exchangeStore.fetchLearningAgreement(props.exchangeId)
}

async function submitExchange() {
  if (exchangeStore.isDirty) {
    await exchangeStore.saveLearningAgreement(props.exchangeId)
  }
  await exchangeStore.updateLearningAgreementStatus(props.exchangeId, { status: 'Submitted' })
  await exchangeStore.fetchExchange(props.exchangeId)
}

async function backToDraft() {
  await exchangeStore.updateLearningAgreementStatus(props.exchangeId, { status: 'Draft' })
  await exchangeStore.fetchExchange(props.exchangeId)
}

async function approveExchange() {
  await exchangeStore.updateLearningAgreementStatus(props.exchangeId, { status: 'Approved' })
  await exchangeStore.fetchExchange(props.exchangeId)
}

async function rejectExchange() {
  await exchangeStore.updateLearningAgreementStatus(props.exchangeId, {
    status: 'Rejected',
    message: coordinatorMessage.value.trim() || null,
  })
  await exchangeStore.fetchExchange(props.exchangeId)
  isEditingMessage.value = false
}

function startEditingMessage() {
  coordinatorMessage.value = exchangeStore.exchange?.coordinatorMessage ?? ''
  isEditingMessage.value = true
}

function cancelEditingMessage() {
  coordinatorMessage.value = exchangeStore.exchange?.coordinatorMessage ?? ''
  isEditingMessage.value = false
}

async function saveMessage() {
  isSavingMessage.value = true
  await exchangeStore.updateCoordinatorMessage(props.exchangeId, {
    message: coordinatorMessage.value.trim() || null,
  })
  isEditingMessage.value = false
  isSavingMessage.value = false
}

const TOTAL_COLS = 30
const SEMESTERS = [1, 2, 3, 4]
const modes: SlotMode[] = ['AtHome', 'AtExchange', 'AfterExchange']

const modeOutlineColor: Record<SlotMode, string> = {
  AtHome: '#4472C4',
  AtExchange: '#FF0000',
  AfterExchange: '#000000',
}

const isDragging = computed(() => !!exchangeStore.draggingCourse)
const dragOverSlotId = ref<string | null>(null)
const pendingDrop = ref<{ slot: CourseSlotResponse; course: ForeignCourseResponse } | null>(null)
const pendingEcts = ref<number>(0)
const editingMapping = ref<{ courseSlotId: string; localId: string } | null>(null)
const editingEcts = ref(0)
const ectsInputRef = ref<HTMLInputElement | null>(null)

function lineFor(courseSlotId: string) {
  return exchangeStore.localSlotStates.find((s) => s.courseSlotId === courseSlotId)
}

function deletedEntriesForSlot(slotId: string) {
  const serverEntries = (exchangeStore.serverLearningAgreement?.entries ?? [])
    .filter(e => e.courseSlotId === slotId && e.foreignCourseId !== null)
  const localIds = new Set((lineFor(slotId)?.mappings ?? []).map(m => m.foreignCourseId))
  return serverEntries.filter(e => e.isDeleted || !localIds.has(e.foreignCourseId!))
}

function slotsForSemester(sem: number): CourseSlotResponse[] {
  return exchangeStore.slots
    .filter((s) => s.semester === sem)
    .sort((a, b) => a.slotPosition - b.slotPosition)
}

function mappedEcts(slot: CourseSlotResponse): number {
  return lineFor(slot.id)?.mappings.reduce((sum, m) => sum + m.awardedEcts, 0) ?? 0
}

function ectsLabel(slot: CourseSlotResponse): string {
  const state = lineFor(slot.id)
  if (!state || state.mode !== 'AtExchange') return ''
  return `${mappedEcts(slot)}/${slot.ects}`
}

function ectsColor(slot: CourseSlotResponse): string {
  const state = lineFor(slot.id)
  if (!state || state.mode !== 'AtExchange') return 'transparent'
  const mapped = mappedEcts(slot)
  if (mapped === 0) return '#94a3b8'
  if (mapped < slot.ects) return '#f59e0b'
  if (mapped === slot.ects) return '#22c55e'
  return '#ef4444'
}

function alreadyMappedEcts(courseId: string): number {
  let sum = 0
  for (const state of exchangeStore.localSlotStates) {
    for (const m of state.mappings) {
      if (m.foreignCourseId === courseId) sum += m.awardedEcts
    }
  }
  return sum
}

const remainingEcts = computed(() => {
  if (!pendingDrop.value) return 0
  const course = pendingDrop.value.course
  return Math.round((course.ects - alreadyMappedEcts(course.id)) * 10) / 10
})

watch(
  () => pendingDrop.value,
  (val) => {
    if (val) {
      pendingEcts.value = Math.round((val.course.ects - alreadyMappedEcts(val.course.id)) * 10) / 10
    }
  },
)

function cellStyle(slot: CourseSlotResponse): Record<string, string> {
  const state = lineFor(slot.id)
  const bg = slot.color

  if (dragOverSlotId.value === slot.id && state?.mode === 'AtExchange') {
    return {
      backgroundColor: 'color-mix(in srgb, var(--color-primary) 20%, transparent)',
      outline: '2px dashed var(--color-primary)',
      outlineOffset: '-2px',
      cursor: 'copy',
    }
  }

  if (isDragging.value && state?.mode === 'AtExchange') {
    return {
      backgroundColor: bg,
      outline: '2px dashed var(--color-primary)',
      outlineOffset: '-2px',
      cursor: 'copy',
    }
  }

  const outline = state ? `3px solid ${modeOutlineColor[state.mode]}` : `1px solid #aaa`
  return {
    backgroundColor: bg,
    outline,
    outlineOffset: state ? '-3px' : '-1px',
    cursor: !isEditable.value || slot.categoryCode === 'Thesis' ? 'default' : 'pointer',
  }
}

function onDragOver(event: DragEvent) {
  event.preventDefault()
}
function onDragEnter(slot: CourseSlotResponse) {
  dragOverSlotId.value = slot.id
}
function onDragLeave() {
  dragOverSlotId.value = null
}

function onDrop(event: DragEvent, slot: CourseSlotResponse) {
  event.preventDefault()
  dragOverSlotId.value = null
  const course = exchangeStore.draggingCourse
  if (!course) return
  if (lineFor(slot.id)?.mode !== 'AtExchange') {
    exchangeStore.localSetSlotMode(slot.id, 'AtExchange')
  }
  pendingDrop.value = { slot, course }
  exchangeStore.endDrag()
}

function confirmDrop() {
  if (!pendingDrop.value) return
  const { slot, course } = pendingDrop.value
  const mapping: LocalSlotMapping = {
    localId: crypto.randomUUID(),
    foreignCourseId: course.id,
    foreignCourseCode: course.code,
    foreignCourseNameEn: course.nameEn,
    foreignCourseNameHr: course.nameHr ?? null,
    awardedEcts: pendingEcts.value,
  }
  exchangeStore.localAddSlotMapping(slot.id, mapping)
  pendingDrop.value = null
}

function cancelDrop() {
  pendingDrop.value = null
}

function cycleMode(slot: CourseSlotResponse) {
  if (!isEditable.value || slot.categoryCode === 'Thesis') return
  const state = lineFor(slot.id)
  if (!state) {
    exchangeStore.localSetSlotMode(slot.id, 'AtHome')
  } else {
    const currentIndex = modes.indexOf(state.mode)
    if (currentIndex === modes.length - 1) {
      exchangeStore.localRemoveSlotState(slot.id)
    } else {
      exchangeStore.localSetSlotMode(slot.id, modes[currentIndex + 1]!)
    }
  }
}

function removeMapping(courseSlotId: string, localId: string) {
  exchangeStore.localRemoveSlotMapping(courseSlotId, localId)
}

function startEditEcts(courseSlotId: string, mapping: LocalSlotMapping) {
  if (!isEditable.value) return
  editingMapping.value = { courseSlotId, localId: mapping.localId }
  editingEcts.value = mapping.awardedEcts
  nextTick(() => ectsInputRef.value?.focus())
}

function saveEditEcts() {
  if (!editingMapping.value) return
  const captured = editingMapping.value
  editingMapping.value = null
  const val = Math.max(0.5, editingEcts.value)
  exchangeStore.localUpdateMappingEcts(captured.courseSlotId, captured.localId, val)
}

function cancelEditEcts() {
  editingMapping.value = null
}
</script>

<template>
  <div>
    <!-- Status + Actions bar -->
    <div class="relative mb-4 flex flex-wrap items-center justify-between gap-3">
      <div class="flex items-center gap-3">
        <StatusBadge :status="exchangeStore.serverLearningAgreement?.status ?? 'Draft'" />
      </div>
      <span
        class="pointer-events-none absolute left-1/2 -translate-x-1/2 text-sm font-semibold text-light/80"
      >
        {{ studyProfileName }}
      </span>
      <div class="flex flex-wrap gap-2">
        <!-- Student actions -->
        <template v-if="!isCoordinator">
          <button
            v-if="exchangeStore.serverLearningAgreement?.status === 'Draft'"
            type="button"
            class="rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
            @click="submitExchange"
          >
            {{ t('exchange.actions.submit') }}
          </button>
          <template v-else-if="exchangeStore.serverLearningAgreement?.status === 'Submitted'">
            <span
              class="inline-block rounded-lg border border-primary/20 px-4 py-2 text-sm text-light/60"
            >
              {{ t('exchange.status.waitingApproval') }}
            </span>
            <button
              type="button"
              class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40"
              @click="backToDraft"
            >
              {{ t('exchange.actions.backToDraft') }}
            </button>
          </template>
          <button
            v-else-if="exchangeStore.serverLearningAgreement?.status === 'Rejected'"
            type="button"
            class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40"
            @click="backToDraft"
          >
            {{ t('exchange.actions.backToDraft') }}
          </button>
        </template>
        <!-- Coordinator actions -->
        <template v-if="isCoordinator">
          <template v-if="exchangeStore.serverLearningAgreement?.status === 'Submitted'">
            <button
              type="button"
              class="rounded-lg bg-green-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-green-500"
              @click="approveExchange"
            >
              {{ t('exchange.actions.approve') }}
            </button>
            <button
              type="button"
              class="rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-red-500"
              @click="rejectExchange"
            >
              {{ t('exchange.actions.reject') }}
            </button>
          </template>
          <button
            v-if="
              exchangeStore.serverLearningAgreement?.status === 'Approved' ||
              exchangeStore.serverLearningAgreement?.status === 'Rejected'
            "
            type="button"
            class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40"
            @click="backToDraft"
          >
            {{ t('exchange.actions.backToDraft') }}
          </button>
        </template>
      </div>
    </div>

    <!-- Coordinator message (visible to student) -->
    <div
      v-if="!isCoordinator && exchangeStore.exchange?.coordinatorMessage"
      class="mb-4 rounded-lg border border-amber-400/40 bg-amber-500/10 px-4 py-3"
    >
      <p class="text-xs font-semibold uppercase tracking-wide text-amber-400">
        {{ t('exchange.coordinatorMessage') }}
      </p>
      <p class="mt-1 text-sm text-amber-200 whitespace-pre-wrap">
        {{ exchangeStore.exchange.coordinatorMessage }}
      </p>
    </div>

    <!-- Coordinator message (editable by coordinator) -->
    <div v-if="isCoordinator" class="mb-4">
      <template v-if="isEditingMessage">
        <label class="block text-xs font-semibold uppercase tracking-wide text-primary-light mb-1">
          {{ t('exchange.coordinatorMessage') }}
        </label>
        <textarea
          v-model="coordinatorMessage"
          rows="3"
          class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder-light/60 focus:border-primary focus:outline-none"
          :placeholder="t('exchange.coordinatorMessagePlaceholder')"
        ></textarea>
        <div class="mt-2 flex gap-2">
          <button
            type="button"
            class="rounded-lg bg-primary px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-60"
            :disabled="isSavingMessage"
            @click="saveMessage"
          >
            {{ isSavingMessage ? t('common.loading') : t('exchange.saveMessage') }}
          </button>
          <button
            type="button"
            class="rounded-lg border border-slate-500 px-3 py-1.5 text-xs text-slate-200 transition hover:bg-slate-700/40"
            @click="cancelEditingMessage"
          >
            {{ t('common.cancel') }}
          </button>
        </div>
      </template>
      <template v-else>
        <div
          v-if="exchangeStore.exchange?.coordinatorMessage"
          class="rounded-lg border border-amber-400/40 bg-amber-500/10 px-4 py-3"
        >
          <div class="flex items-start justify-between gap-2">
            <div>
              <p class="text-xs font-semibold uppercase tracking-wide text-amber-400">
                {{ t('exchange.coordinatorMessage') }}
              </p>
              <p class="mt-1 text-sm text-amber-200 whitespace-pre-wrap">
                {{ exchangeStore.exchange.coordinatorMessage }}
              </p>
            </div>
            <button
              type="button"
              class="shrink-0 text-xs text-primary-light hover:text-white transition"
              @click="startEditingMessage"
            >
              {{ t('exchange.editMessage') }}
            </button>
          </div>
        </div>
        <button
          v-else
          type="button"
          class="text-xs text-light/60 hover:text-primary-light transition"
          @click="startEditingMessage"
        >
          + {{ t('exchange.addMessage') }}
        </button>
      </template>
    </div>

    <!-- Save bar -->
    <UnsavedChangesBar
      v-if="isEditable && exchangeStore.isDirty"
      :saving="isSavingLa"
      @save="saveLa"
      @discard="discardLa"
    />

    <!-- Table -->
    <div v-if="exchangeStore.serverLearningAgreement" class="overflow-x-auto doc-table-wrap">
      <table style="border-collapse: collapse; width: 100%; min-width: 900px; table-layout: fixed">
        <colgroup>
          <col style="width: 60px" />
          <col v-for="c in TOTAL_COLS" :key="c" />
        </colgroup>
        <thead>
          <tr>
            <th
              style="
                border: 1px solid #aaa;
                background: #d9d9d9;
                font-size: 10px;
                padding: 4px 4px;
                text-align: center;
                color: #000;
              "
            >
              {{ t('table.semester') }}
            </th>
            <th
              v-for="col in TOTAL_COLS"
              :key="col"
              style="
                border: 1px solid #aaa;
                background: #d9d9d9;
                font-size: 10px;
                padding: 4px 0;
                text-align: center;
                font-weight: normal;
                color: #000;
              "
            >
              {{ col }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="sem in SEMESTERS" :key="sem" :style="{ height: sem === 4 ? '50px' : '90px' }">
            <td
              style="
                border: 1px solid #aaa;
                background: #f2f2f2;
                text-align: center;
                font-size: 14px;
                font-weight: bold;
                color: #000;
                padding: 4px 2px;
                vertical-align: middle;
              "
            >
              {{ sem }}
            </td>
            <td
              v-for="slot in slotsForSemester(sem)"
              :key="slot.id"
              :colspan="slot.ects"
              :style="cellStyle(slot)"
              class="la-slot-cell"
              @click="cycleMode(slot)"
              @dragover="onDragOver($event)"
              @dragenter="onDragEnter(slot)"
              @dragleave="onDragLeave()"
              @drop="onDrop($event, slot)"
            >
              <div class="la-cell-name">
                {{ slot.courseCode ?? slot.courseName }}
              </div>
              <div class="la-cell-sub">
                {{
                  slot.courseCode
                    ? locale === 'en' && slot.courseNameEn
                      ? slot.courseNameEn
                      : slot.courseName
                    : t(`courseSlotCategory.${slot.categoryCode}`)
                }}
              </div>

              <div v-if="lineFor(slot.id)" style="margin-top: 3px">
                <span
                  style="
                    display: inline-block;
                    font-size: 10px;
                    padding: 1px 4px;
                    border-radius: 2px;
                    font-weight: 600;
                  "
                  :style="{
                    color: modeOutlineColor[lineFor(slot.id)!.mode],
                    border: `1px solid ${modeOutlineColor[lineFor(slot.id)!.mode]}`,
                    background: 'rgba(255,255,255,0.6)',
                  }"
                >
                  {{ t(`slotMode.${lineFor(slot.id)!.mode}`) }}
                </span>
              </div>

              <div v-if="ectsLabel(slot)" style="margin-top: 3px">
                <span
                  style="
                    display: inline-block;
                    font-size: 10px;
                    padding: 1px 4px;
                    border-radius: 2px;
                    font-weight: 700;
                  "
                  :style="{
                    color: ectsColor(slot),
                    border: `1px solid ${ectsColor(slot)}`,
                    background: 'rgba(255,255,255,0.6)',
                  }"
                >
                  {{ ectsLabel(slot) }} ECTS
                </span>
              </div>

              <div
                v-for="removed in deletedEntriesForSlot(slot.id)"
                :key="`removed-${removed.id}`"
                class="la-mapping-item la-mapping-removed"
              >
                <svg class="la-mapping-x" aria-hidden="true" preserveAspectRatio="none">
                  <line x1="0" y1="0" x2="100%" y2="100%" stroke="rgba(204,0,0,0.75)" stroke-width="1.5"/>
                  <line x1="100%" y1="0" x2="0" y2="100%" stroke="rgba(204,0,0,0.75)" stroke-width="1.5"/>
                </svg>
                <span class="la-mapping-text">
                  <span style="font-weight: 700">{{ removed.foreignCourseCode }}</span><br />
                  <span style="font-size: 10px; color: var(--color-primary-light)">{{ removed.foreignCourseNameEn }}</span><br />
                  <span style="font-size: 10px; color: #777">{{ removed.foreignCourseNameHr ?? '-' }}</span><br />
                  <span style="color: #555; font-size: 10px">{{ removed.awardedEcts }} ECTS</span>
                </span>
              </div>

              <div
                v-for="mapping in lineFor(slot.id)?.mappings ?? []"
                :key="mapping.localId"
                class="la-mapping-item"
                @click.stop
              >
                <span class="la-mapping-text">
                  <span style="font-weight: 700">{{ mapping.foreignCourseCode }}</span><br />
                  <span style="font-size: 10px; color: var(--color-primary-light)">{{ mapping.foreignCourseNameEn }}</span><br />
                  <span style="font-size: 10px; color: #777">{{ mapping.foreignCourseNameHr ?? '-' }}</span><br />
                  <template
                    v-if="editingMapping?.localId === mapping.localId"
                    :key="`edit-${mapping.localId}`"
                  >
                    <input
                      ref="ectsInputRef"
                      v-model.number="editingEcts"
                      type="number"
                      min="0.5"
                      step="0.5"
                      style="
                        width: 52px;
                        font-size: 11px;
                        padding: 1px 3px;
                        background: var(--color-dark);
                        color: var(--color-light);
                        border: 1px solid var(--color-primary);
                        border-radius: 3px;
                      "
                      @blur="saveEditEcts()"
                      @keydown.enter.prevent="saveEditEcts()"
                      @keydown.escape.prevent="cancelEditEcts()"
                      @click.stop
                    />
                    <span style="color: #555; margin-left: 2px">ECTS</span>
                  </template>
                  <span
                    v-else
                    :key="`show-${mapping.localId}`"
                    :style="{
                      color: '#555',
                      cursor: !isEditable ? 'default' : 'pointer',
                      textDecoration: !isEditable ? 'none' : 'underline dotted',
                    }"
                    :title="isEditable ? 'Klikni za izmjenu' : ''"
                    @click.stop="startEditEcts(slot.id, mapping)"
                    >{{ mapping.awardedEcts }} ECTS</span
                  >
                </span>
                <button
                  v-if="isEditable"
                  type="button"
                  style="
                    color: #cc0000;
                    font-size: 14px;
                    line-height: 1;
                    background: none;
                    border: none;
                    cursor: pointer;
                    padding: 0;
                    margin-left: 4px;
                  "
                  @click.stop="removeMapping(slot.id, mapping.localId)"
                >
                  &times;
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Legend -->
      <div class="doc-legend">
        <div v-for="mode in modes" :key="mode" style="display: flex; align-items: center; gap: 6px">
          <span
            style="display: inline-block; width: 12px; height: 12px"
            :style="{ background: modeOutlineColor[mode] }"
          />
          <span style="font-size: 11px; color: var(--color-primary-light)">{{
            t(`slotMode.${mode}`)
          }}</span>
        </div>
        <span style="font-size: 11px; color: var(--color-light); opacity: 0.6; margin-left: 8px">
          {{ t('table.clickToChange') }}
        </span>
      </div>
    </div>

    <!-- ECTS input popup -->
    <div
      v-if="pendingDrop"
      style="
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 50;
      "
      @mousedown.self="cancelDrop"
    >
      <div
        style="
          background: var(--color-dark-2);
          border: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent);
          border-radius: 8px;
          padding: 24px;
          min-width: 320px;
        "
      >
        <h3
          style="color: var(--color-light); font-size: 14px; font-weight: 600; margin-bottom: 16px"
        >
          {{ t('foreignCourses.addMapping') }}
        </h3>
        <div style="color: var(--color-primary-light); font-size: 12px; margin-bottom: 4px">
          {{ pendingDrop.course.code }} — {{ pendingDrop.course.nameEn }}
        </div>
        <div style="color: var(--color-light); opacity: 0.6; font-size: 11px; margin-bottom: 16px">
          {{ t('foreignCourses.availableEcts') }}: {{ remainingEcts }} /
          {{ pendingDrop.course.ects }} ECTS
        </div>
        <label
          style="display: block; color: var(--color-light); font-size: 12px; margin-bottom: 6px"
        >
          {{ t('foreignCourses.awardedEcts') }}
        </label>
        <input
          v-model.number="pendingEcts"
          type="number"
          :min="0.5"
          :max="remainingEcts"
          step="0.5"
          style="
            width: 100%;
            background: var(--color-dark);
            border: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent);
            color: var(--color-light);
            padding: 8px;
            border-radius: 4px;
            font-size: 13px;
            margin-bottom: 16px;
          "
        />
        <div style="display: flex; gap: 8px; justify-content: flex-end">
          <button
            type="button"
            style="
              padding: 8px 16px;
              border: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent);
              background: transparent;
              color: var(--color-primary-light);
              border-radius: 4px;
              cursor: pointer;
              font-size: 13px;
            "
            @click="cancelDrop"
          >
            {{ t('common.cancel') }}
          </button>
          <button
            type="button"
            style="
              padding: 8px 16px;
              background: var(--color-primary);
              border: none;
              color: white;
              border-radius: 4px;
              cursor: pointer;
              font-size: 13px;
              font-weight: 600;
            "
            @click="confirmDrop"
          >
            {{ t('common.confirm') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Course panels (editable only) -->
    <div v-if="isEditable && exchangeStore.exchange" class="mt-6 flex gap-6 items-start">
      <div class="min-w-0 basis-[65%] rounded-xl border border-primary/20 bg-dark-2 p-4">
        <h3 class="mb-2 text-sm font-semibold text-primary-light">
          {{ t('foreignCourses.availableCourses') }}
        </h3>
        <p class="mb-3 text-xs text-light/60">{{ t('foreignCourses.dragHint') }}</p>
        <ForeignCoursePanel
          :foreign-program-id="exchangeStore.exchange.foreignProgram.id"
          :exchange-id="exchangeId"
          variant="available"
        />
      </div>
      <div class="min-w-0 basis-[35%] rounded-xl border border-primary/20 bg-dark-2 p-4">
        <h3 class="mb-2 text-sm font-semibold text-green-400">
          {{ t('foreignCourses.mappedCourses') }}
        </h3>
        <ForeignCoursePanel
          :foreign-program-id="exchangeStore.exchange.foreignProgram.id"
          :exchange-id="exchangeId"
          variant="mapped"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Shared doc-table styles */
.doc-table-wrap {
  font-family: Calibri, Arial, sans-serif;
}

.doc-legend {
  margin-top: 8px;
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
}

/* Slot cell */
.la-slot-cell {
  border: 1px solid #aaa;
  vertical-align: top;
  padding: 8px;
}

.la-cell-name {
  font-size: 13px;
  font-weight: 700;
  color: #000;
  line-height: 1.3;
}

.la-cell-sub {
  font-size: 11px;
  font-weight: 400;
  color: #222;
  line-height: 1.3;
  margin-top: 1px;
}

.la-mapping-item {
  margin-top: 3px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  background: rgba(0, 0, 0, 0.08);
  padding: 2px 4px;
  font-size: 11px;
}

.la-mapping-text {
  color: #000;
  line-height: 1.3;
}

/* Removed mapping */
.la-mapping-removed {
  position: relative;
  opacity: 0.65;
  pointer-events: none;
}
.la-mapping-x {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  overflow: visible;
}
</style>
