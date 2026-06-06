<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import { useI18n } from 'vue-i18n'
import PartnerCoursePanel from '@/components/exchange/PartnerCoursePanel.vue'
import StatusBadge from '@/components/common/StatusBadge.vue'
import UnsavedChangesBar from '@/components/common/UnsavedChangesBar.vue'
import { useExchangeStore } from '@/stores/exchange.store'
import { useExchangePermissions } from '@/composables/useExchangePermissions'
import type { HomeSlotResponse, LocalSlotMapping, SlotMode } from '@/types/exchange.types'
import type { PartnerCourseResponse } from '@/types/institution.types'
import { documentStatus } from '@/utils/documentStatus'
import { slotMode } from '@/utils/slotMode'
import { useTheme } from '@/composables/useTheme'

const props = defineProps<{
  exchangeId: string
  homeProfileName: string
}>()

const { t, locale } = useI18n()
const exchangeStore = useExchangeStore()
const { isCoordinator, isEditable } = useExchangePermissions()
const { theme } = useTheme()

const isSavingLa = ref(false)
const saveError = ref<string | null>(null)

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
  await exchangeStore.updateLearningAgreementStatus(props.exchangeId, {
    status: documentStatus.Submitted,
  })
  await exchangeStore.fetchExchange(props.exchangeId)
}

async function backToDraft() {
  await exchangeStore.updateLearningAgreementStatus(props.exchangeId, {
    status: documentStatus.Draft,
  })
  await exchangeStore.fetchExchange(props.exchangeId)
}

async function approveExchange() {
  await exchangeStore.updateLearningAgreementStatus(props.exchangeId, {
    status: documentStatus.Approved,
  })
  await exchangeStore.fetchExchange(props.exchangeId)
}

async function rejectExchange() {
  await exchangeStore.updateLearningAgreementStatus(props.exchangeId, {
    status: documentStatus.Rejected,
  })
  await exchangeStore.fetchExchange(props.exchangeId)
}

const TOTAL_COLS = 30
const SEMESTERS = [1, 2, 3, 4]
const modes: SlotMode[] = [slotMode.AtHome, slotMode.AtExchange, slotMode.AfterExchange]

const modeOutlineColor: Record<SlotMode, string> = {
  AtHome: '#4472C4',
  AtExchange: '#FF0000',
  AfterExchange: '#000000',
}

const isDragging = computed(() => !!exchangeStore.draggingCourse)
const dragOverSlotId = ref<string | null>(null)
const pendingDrop = ref<{ slot: HomeSlotResponse; course: PartnerCourseResponse } | null>(null)
const pendingEcts = ref<number>(0)
const editingMapping = ref<{ homeSlotId: string; localId: string } | null>(null)
const editingEcts = ref(0)
const ectsInputRef = ref<HTMLInputElement | null>(null)

function lineFor(homeSlotId: string) {
  return exchangeStore.localSlotStates.find((s) => s.homeSlotId === homeSlotId)
}

function deletedEntriesForSlot(slotId: string) {
  const serverEntries = (exchangeStore.serverLearningAgreement?.entries ?? []).filter(
    (e) => e.homeSlotId === slotId && e.partnerCourseId !== null,
  )
  const localIds = new Set((lineFor(slotId)?.mappings ?? []).map((m) => m.partnerCourseId))
  return serverEntries.filter((e) => e.isDeleted || !localIds.has(e.partnerCourseId!))
}

function slotsForSemester(sem: number): HomeSlotResponse[] {
  return exchangeStore.slots
    .filter((s) => s.semester === sem)
    .sort((a, b) => a.slotPosition - b.slotPosition)
}

function mappedEcts(slot: HomeSlotResponse): number {
  return lineFor(slot.id)?.mappings.reduce((sum, m) => sum + m.awardedEcts, 0) ?? 0
}

function ectsLabel(slot: HomeSlotResponse): string {
  const state = lineFor(slot.id)
  if (!state || state.mode !== slotMode.AtExchange) return ''
  return `${mappedEcts(slot)}/${slot.ects}`
}

function ectsColor(slot: HomeSlotResponse): string {
  const state = lineFor(slot.id)
  if (!state || state.mode !== slotMode.AtExchange) return 'transparent'
  const mapped = mappedEcts(slot)
  const light = theme.value === 'light'
  if (mapped === 0) return light ? '#78716c' : '#94a3b8'
  if (mapped < slot.ects) return light ? '#b45309' : '#f59e0b'
  if (mapped === slot.ects) return light ? '#16a34a' : '#22c55e'
  return '#ef4444'
}

function alreadyMappedEcts(courseId: string): number {
  let sum = 0
  for (const state of exchangeStore.localSlotStates) {
    for (const m of state.mappings) {
      if (m.partnerCourseId === courseId) sum += m.awardedEcts
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

function isThesisSlot(slot: HomeSlotResponse): boolean {
  return slot.courseTypeNameEn === 'Master thesis'
}

function cellStyle(slot: HomeSlotResponse): Record<string, string> {
  const state = lineFor(slot.id)
  const bg = slot.color

  if (dragOverSlotId.value === slot.id && state?.mode === slotMode.AtExchange) {
    return {
      backgroundColor: 'color-mix(in srgb, var(--color-primary) 20%, transparent)',
      outline: '2px dashed var(--color-primary)',
      outlineOffset: '-2px',
      cursor: 'copy',
    }
  }

  if (isDragging.value && state?.mode === slotMode.AtExchange) {
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
    cursor: !isEditable.value || isThesisSlot(slot) ? 'default' : 'pointer',
  }
}

function onDragOver(event: DragEvent) {
  event.preventDefault()
}
function onDragEnter(slot: HomeSlotResponse) {
  dragOverSlotId.value = slot.id
}
function onDragLeave() {
  dragOverSlotId.value = null
}

function onDrop(event: DragEvent, slot: HomeSlotResponse) {
  event.preventDefault()
  dragOverSlotId.value = null
  const course = exchangeStore.draggingCourse
  if (!course) return
  if (lineFor(slot.id)?.mode !== slotMode.AtExchange) {
    exchangeStore.localSetSlotMode(slot.id, slotMode.AtExchange)
  }
  pendingDrop.value = { slot, course }
  exchangeStore.endDrag()
}

function confirmDrop() {
  if (!pendingDrop.value) return
  const { slot, course } = pendingDrop.value
  const mapping: LocalSlotMapping = {
    localId: crypto.randomUUID(),
    partnerCourseId: course.id,
    partnerCourseCode: course.code,
    partnerCourseNameEn: course.nameEn,
    partnerCourseNameHr: course.nameHr ?? null,
    awardedEcts: pendingEcts.value,
  }
  exchangeStore.localAddSlotMapping(slot.id, mapping)
  pendingDrop.value = null
}

function cancelDrop() {
  pendingDrop.value = null
}

function cycleMode(slot: HomeSlotResponse) {
  if (!isEditable.value || isThesisSlot(slot)) return
  const state = lineFor(slot.id)
  if (!state) {
    exchangeStore.localSetSlotMode(slot.id, slotMode.AtHome)
  } else {
    const currentIndex = modes.indexOf(state.mode)
    if (currentIndex === modes.length - 1) {
      exchangeStore.localRemoveSlotState(slot.id)
    } else {
      exchangeStore.localSetSlotMode(slot.id, modes[currentIndex + 1]!)
    }
  }
}

function removeMapping(homeSlotId: string, localId: string) {
  exchangeStore.localRemoveSlotMapping(homeSlotId, localId)
}

function startEditEcts(homeSlotId: string, mapping: LocalSlotMapping) {
  if (!isEditable.value) return
  editingMapping.value = { homeSlotId, localId: mapping.localId }
  editingEcts.value = mapping.awardedEcts
  nextTick(() => ectsInputRef.value?.focus())
}

function saveEditEcts() {
  if (!editingMapping.value) return
  const captured = editingMapping.value
  editingMapping.value = null
  const val = Math.max(0.5, editingEcts.value)
  exchangeStore.localUpdateMappingEcts(captured.homeSlotId, captured.localId, val)
}

function cancelEditEcts() {
  editingMapping.value = null
}

function slotDisplayName(slot: HomeSlotResponse): string {
  return slot.courseName ?? slot.courseGroupName ?? ''
}

function slotDisplayCode(slot: HomeSlotResponse): string | number | null {
  return slot.courseIsvuCode ?? slot.courseGroupIsvuCode ?? null
}

function slotSubLabel(slot: HomeSlotResponse): string {
  const code = slotDisplayCode(slot)
  if (code !== null) {
    return locale.value === 'en'
      ? (slot.courseNameEn ?? slot.courseGroupNameEn ?? slot.courseTypeName)
      : slotDisplayName(slot)
  }
  return slot.courseTypeName
}
</script>

<template>
  <div>
    <!-- Status + Actions bar -->
    <div class="relative mb-4 flex flex-wrap items-center justify-between gap-3">
      <div class="flex items-center gap-3">
        <StatusBadge
          v-if="exchangeStore.serverLearningAgreement"
          :status="exchangeStore.serverLearningAgreement.status"
        />
      </div>
      <span
        class="pointer-events-none absolute left-1/2 -translate-x-1/2 text-sm font-semibold text-light/80"
      >
        {{ homeProfileName }}
      </span>
      <div class="flex flex-wrap gap-2">
        <!-- Student actions -->
        <template v-if="!isCoordinator">
          <button
            v-if="exchangeStore.serverLearningAgreement?.status === documentStatus.Draft"
            type="button"
            class="rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
            @click="submitExchange"
          >
            {{ t('exchange.actions.submit') }}
          </button>
          <template
            v-else-if="exchangeStore.serverLearningAgreement?.status === documentStatus.Submitted"
          >
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
            v-else-if="exchangeStore.serverLearningAgreement?.status === documentStatus.Rejected"
            type="button"
            class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40"
            @click="backToDraft"
          >
            {{ t('exchange.actions.backToDraft') }}
          </button>
        </template>
        <!-- Coordinator actions -->
        <template v-if="isCoordinator">
          <template
            v-if="exchangeStore.serverLearningAgreement?.status === documentStatus.Submitted"
          >
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
              exchangeStore.serverLearningAgreement?.status === documentStatus.Approved ||
              exchangeStore.serverLearningAgreement?.status === documentStatus.Rejected
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
            <th style="border: 1px solid #aaa; background: #d9d9d9; font-size: 10px; padding: 4px 4px; text-align: center; color: #000;">
              {{ t('table.semester') }}
            </th>
            <th
              v-for="col in TOTAL_COLS"
              :key="col"
              style="border: 1px solid #aaa; background: #d9d9d9; font-size: 10px; padding: 4px 0; text-align: center; font-weight: normal; color: #000;"
            >
              {{ col }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="sem in SEMESTERS" :key="sem" :style="{ height: sem === 4 ? '50px' : '90px' }">
            <td style="border: 1px solid #aaa; background: #f2f2f2; text-align: center; font-size: 14px; font-weight: bold; color: #000; padding: 4px 2px; vertical-align: middle;">
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
                {{ slotDisplayCode(slot) ?? slotDisplayName(slot) }}
              </div>
              <div class="la-cell-sub">
                {{ slotSubLabel(slot) }}
              </div>

              <div v-if="lineFor(slot.id)" style="margin-top: 3px">
                <span
                  style="display: inline-block; font-size: 10px; padding: 1px 4px; border-radius: 2px; font-weight: 600;"
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
                  style="display: inline-block; font-size: 10px; padding: 1px 4px; border-radius: 2px; font-weight: 700;"
                  :style="{
                    color: ectsColor(slot),
                    border: `1px solid ${ectsColor(slot)}`,
                    background: theme === 'light' ? `${ectsColor(slot)}18` : 'rgba(255,255,255,0.08)',
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
                  <line x1="0" y1="0" x2="100%" y2="100%" stroke="rgba(204,0,0,0.75)" stroke-width="1.5" />
                  <line x1="100%" y1="0" x2="0" y2="100%" stroke="rgba(204,0,0,0.75)" stroke-width="1.5" />
                </svg>
                <span class="la-mapping-text">
                  <span style="font-weight: 700">{{ removed.partnerCourseCode }}</span><br />
                  <span style="font-size: 10px; color: var(--color-primary-light)">{{ removed.partnerCourseNameEn }}</span><br />
                  <span style="font-size: 10px; color: #777">{{ removed.partnerCourseNameHr ?? '-' }}</span><br />
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
                  <span style="font-weight: 700">{{ mapping.partnerCourseCode }}</span><br />
                  <span style="font-size: 10px; color: var(--color-primary-light)">{{ mapping.partnerCourseNameEn }}</span><br />
                  <span style="font-size: 10px; color: #777">{{ mapping.partnerCourseNameHr ?? '-' }}</span><br />
                  <template v-if="editingMapping?.localId === mapping.localId" :key="`edit-${mapping.localId}`">
                    <input
                      ref="ectsInputRef"
                      v-model.number="editingEcts"
                      type="number"
                      min="0.5"
                      step="0.5"
                      style="width: 52px; font-size: 11px; padding: 1px 3px; background: var(--color-dark); color: var(--color-light); border: 1px solid var(--color-primary); border-radius: 3px;"
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
                  style="color: #cc0000; font-size: 14px; line-height: 1; background: none; border: none; cursor: pointer; padding: 0; margin-left: 4px;"
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
          <span style="display: inline-block; width: 12px; height: 12px" :style="{ background: modeOutlineColor[mode] }" />
          <span style="font-size: 11px; color: var(--color-primary-light)">{{ t(`slotMode.${mode}`) }}</span>
        </div>
        <span style="font-size: 11px; color: var(--color-light); opacity: 0.6; margin-left: 8px">
          {{ t('table.clickToChange') }}
        </span>
      </div>
    </div>

    <!-- ECTS input popup -->
    <div
      v-if="pendingDrop"
      style="position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 50;"
      @mousedown.self="cancelDrop"
    >
      <div style="background: var(--color-dark-2); border: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent); border-radius: 8px; padding: 24px; min-width: 320px;">
        <h3 style="color: var(--color-light); font-size: 14px; font-weight: 600; margin-bottom: 16px">
          {{ t('partnerCourses.addMapping') }}
        </h3>
        <div style="color: var(--color-primary-light); font-size: 12px; margin-bottom: 4px">
          {{ pendingDrop.course.code }} — {{ pendingDrop.course.nameEn }}
        </div>
        <div style="color: var(--color-light); opacity: 0.6; font-size: 11px; margin-bottom: 16px">
          {{ t('partnerCourses.availableEcts') }}: {{ remainingEcts }} / {{ pendingDrop.course.ects }} ECTS
        </div>
        <label style="display: block; color: var(--color-light); font-size: 12px; margin-bottom: 6px">
          {{ t('partnerCourses.awardedEcts') }}
        </label>
        <input
          v-model.number="pendingEcts"
          type="number"
          :min="0.5"
          :max="remainingEcts"
          step="0.5"
          style="width: 100%; background: var(--color-dark); border: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent); color: var(--color-light); padding: 8px; border-radius: 4px; font-size: 13px; margin-bottom: 16px;"
        />
        <div style="display: flex; gap: 8px; justify-content: flex-end">
          <button
            type="button"
            style="padding: 8px 16px; border: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent); background: transparent; color: var(--color-primary-light); border-radius: 4px; cursor: pointer; font-size: 13px;"
            @click="cancelDrop"
          >
            {{ t('common.cancel') }}
          </button>
          <button
            type="button"
            style="padding: 8px 16px; background: var(--color-primary); border: none; color: white; border-radius: 4px; cursor: pointer; font-size: 13px; font-weight: 600;"
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
          {{ t('partnerCourses.availableCourses') }}
        </h3>
        <p class="mb-3 text-xs text-light/60">{{ t('partnerCourses.dragHint') }}</p>
        <PartnerCoursePanel
          :partner-program-id="exchangeStore.exchange.partnerProgram.id"
          :exchange-id="exchangeId"
          variant="available"
        />
      </div>
      <div class="min-w-0 basis-[35%] rounded-xl border border-primary/20 bg-dark-2 p-4">
        <h3 class="mb-2 text-sm font-semibold text-green-400">
          {{ t('partnerCourses.mappedCourses') }}
        </h3>
        <PartnerCoursePanel
          :partner-program-id="exchangeStore.exchange.partnerProgram.id"
          :exchange-id="exchangeId"
          variant="mapped"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
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
