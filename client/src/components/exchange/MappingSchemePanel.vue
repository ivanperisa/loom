<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useExchangeStore } from '@/stores/exchange.store'
import { useTheme } from '@/composables/useTheme'
import UnsavedChangesBar from '@/components/common/UnsavedChangesBar.vue'
import type { HomeSlotResponse, SlotMode } from '@/types/learningAgreement.types'
import type { MappingSchemeEntryResponse } from '@/types/mappingScheme.types'
import { slotMode } from '@/utils/slotMode'

const props = defineProps<{
  exchangeId: string
}>()

const { t, locale } = useI18n()
const exchangeStore = useExchangeStore()
const { theme } = useTheme()

const loading = ref(true)
const saving = ref(false)

const TOTAL_COLS = 30
const SEMESTERS = [1, 2, 3, 4]
const modes: SlotMode[] = [slotMode.AtHome, slotMode.AtExchange]
const modeOutlineColor: Record<string, string> = {
  AtHome: '#4472C4',
  AtExchange: '#FF0000',
}

// Editable working copy (homeSlotId + enrollmentStatus only).
const localEntries = ref<MappingSchemeEntryResponse[]>([])

function rebuildLocal() {
  localEntries.value = (exchangeStore.serverMappingScheme?.entries ?? []).map((e) => ({ ...e }))
}

const isActive = computed(() => (exchangeStore.serverMappingScheme?.entries.length ?? 0) > 0)

const isDirty = computed(() => {
  const server = exchangeStore.serverMappingScheme?.entries ?? []
  if (server.length !== localEntries.value.length) return true
  const byId = new Map(server.map((e) => [e.id, e]))
  return localEntries.value.some((e) => {
    const s = byId.get(e.id)
    if (!s) return true
    return (
      s.homeSlotId !== e.homeSlotId ||
      s.awardedEcts !== e.awardedEcts ||
      (s.enrollmentStatus ?? '') !== (e.enrollmentStatus ?? '')
    )
  })
})

// Slot modes come from the learning agreement (the scheme itself doesn't store them).
const laModeBySlot = computed(() => {
  const m = new Map<string, SlotMode>()
  for (const e of exchangeStore.serverLearningAgreement?.entries ?? []) {
    if (!e.isDeleted) m.set(e.homeSlotId, e.mode)
  }
  return m
})

function slotsForSemester(sem: number): HomeSlotResponse[] {
  return exchangeStore.slots
    .filter((s) => s.semester === sem)
    .sort((a, b) => a.slotPosition - b.slotPosition)
}

function entriesForSlot(slotId: string): MappingSchemeEntryResponse[] {
  return localEntries.value.filter((e) => e.homeSlotId === slotId)
}

function mappedEcts(slot: HomeSlotResponse): number {
  return Math.round(entriesForSlot(slot.id).reduce((sum, e) => sum + e.awardedEcts, 0) * 10) / 10
}

function ectsLabel(slot: HomeSlotResponse): string {
  return entriesForSlot(slot.id).length === 0 ? '' : `${mappedEcts(slot)}/${slot.ects}`
}

function ectsColor(slot: HomeSlotResponse): string {
  if (entriesForSlot(slot.id).length === 0) return 'transparent'
  const mapped = mappedEcts(slot)
  const light = theme.value === 'light'
  if (mapped === 0) return light ? '#78716c' : '#94a3b8'
  if (mapped < slot.ects) return light ? '#b45309' : '#f59e0b'
  if (mapped === slot.ects) return light ? '#16a34a' : '#22c55e'
  return '#ef4444'
}

// Drag & drop — drops open a dialog to choose how many ECTS move to the target slot.
const draggingId = ref<string | null>(null)
const dragOverSlotId = ref<string | null>(null)
const isDragging = computed(() => draggingId.value !== null)

let tempId = -1
function round1(n: number): number {
  return Math.round(n * 10) / 10
}

const pendingTransfer = ref<{ entryId: string; toSlotId: string; max: number } | null>(null)
const transferEcts = ref(0)
const transferSource = computed(() =>
  pendingTransfer.value
    ? (localEntries.value.find((e) => e.id === pendingTransfer.value!.entryId) ?? null)
    : null,
)

function onDragStart(entry: MappingSchemeEntryResponse) {
  draggingId.value = entry.id
}
function onDragOver(event: DragEvent) {
  event.preventDefault()
}
function onDrop(slot: HomeSlotResponse) {
  dragOverSlotId.value = null
  const id = draggingId.value
  draggingId.value = null
  if (id === null) return
  const entry = localEntries.value.find((e) => e.id === id)
  if (!entry || entry.homeSlotId === slot.id || entry.awardedEcts <= 0) return
  pendingTransfer.value = { entryId: entry.id, toSlotId: slot.id, max: entry.awardedEcts }
  transferEcts.value = entry.awardedEcts
}

function confirmTransfer() {
  const p = pendingTransfer.value
  if (!p) return
  pendingTransfer.value = null
  const amount = round1(Math.min(Math.max(transferEcts.value, 0), p.max))
  if (amount <= 0) return

  const source = localEntries.value.find((e) => e.id === p.entryId)
  if (!source) return
  const target = localEntries.value.find(
    (e) =>
      e.id !== source.id &&
      String(e.homeSlotId) === String(p.toSlotId) &&
      e.partnerCourseCode === source.partnerCourseCode,
  )

  if (amount >= source.awardedEcts) {
    if (target) {
      target.awardedEcts = round1(target.awardedEcts + source.awardedEcts)
      localEntries.value = localEntries.value.filter((e) => e.id !== source.id)
    } else {
      source.homeSlotId = p.toSlotId
    }
  } else {
    source.awardedEcts = round1(source.awardedEcts - amount)
    if (target) {
      target.awardedEcts = round1(target.awardedEcts + amount)
    } else {
      localEntries.value.push({ ...source, id: String(tempId--), homeSlotId: p.toSlotId, awardedEcts: amount })
    }
  }
}

function cancelTransfer() {
  pendingTransfer.value = null
}

function cellStyle(slot: HomeSlotResponse): Record<string, string> {
  const bg = slot.color

  if (dragOverSlotId.value === slot.id) {
    return {
      backgroundColor: 'color-mix(in srgb, var(--color-primary) 20%, transparent)',
      outline: '2px dashed var(--color-primary)',
      outlineOffset: '-2px',
      cursor: 'copy',
    }
  }
  if (isDragging.value) {
    return {
      backgroundColor: bg,
      outline: '2px dashed var(--color-primary)',
      outlineOffset: '-2px',
      cursor: 'copy',
    }
  }

  const hasEntries = entriesForSlot(slot.id).length > 0
  const mode: SlotMode | undefined = hasEntries ? slotMode.AtExchange : laModeBySlot.value.get(slot.id)
  const showOutline =
    !!mode && mode !== slotMode.AfterExchange && (mode === slotMode.AtHome || hasEntries)
  const outline = showOutline ? `3px solid ${modeOutlineColor[mode!]}` : `1px solid #aaa`
  return {
    backgroundColor: bg,
    outline,
    outlineOffset: showOutline ? '-3px' : '-1px',
  }
}

function isNotPassed(entry: MappingSchemeEntryResponse): boolean {
  return entry.enrollmentStatus === 'NotPassed'
}
function markNotPassed(entry: MappingSchemeEntryResponse) {
  entry.enrollmentStatus = 'NotPassed'
}
function onItemClick(entry: MappingSchemeEntryResponse) {
  if (isNotPassed(entry)) entry.enrollmentStatus = 'Passed'
}

function slotDisplayCode(slot: HomeSlotResponse): string | number | null {
  return slot.courseIsvuCode ?? slot.courseGroupIsvuCode ?? null
}
function slotDisplayName(slot: HomeSlotResponse): string {
  return slot.courseName ?? slot.courseGroupName ?? slot.courseTypeName
}
function slotSubLabel(slot: HomeSlotResponse): string {
  if (slotDisplayCode(slot) !== null) {
    return locale.value === 'en'
      ? (slot.courseNameEn ?? slot.courseGroupNameEn ?? slot.courseTypeName)
      : slotDisplayName(slot)
  }
  return slot.courseTypeName
}

async function save() {
  saving.value = true
  try {
    await exchangeStore.saveMappingScheme(props.exchangeId, {
      entries: localEntries.value.map((e) => ({
        id: Number(e.id),
        homeSlotId: Number(e.homeSlotId),
        partnerCourseId: e.partnerCourseId === null ? null : Number(e.partnerCourseId),
        awardedEcts: e.awardedEcts,
        enrollmentStatus: e.enrollmentStatus || null,
        originalGrade: e.originalGrade,
        ectsGrade: e.ectsGrade,
        hrGrade: e.hrGrade,
        examDate: e.examDate,
      })),
    })
  } finally {
    saving.value = false
  }
}

function discard() {
  rebuildLocal()
}

watch(() => exchangeStore.serverMappingScheme, rebuildLocal, { deep: false })

onMounted(async () => {
  try {
    if (!exchangeStore.serverMappingScheme) {
      await exchangeStore.fetchMappingScheme(props.exchangeId)
    }
    rebuildLocal()
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <div v-if="loading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="h-14 animate-pulse rounded bg-primary/20"></div>
    </div>

    <!-- Phase 1: not yet available -->
    <div
      v-else-if="!isActive"
      class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center text-light/60"
    >
      {{ t('mappingScheme.lockedPhase1') }}
    </div>

    <template v-else>
      <p class="mb-3 text-xs text-light/60">{{ t('mappingScheme.dragHint') }}</p>

      <UnsavedChangesBar v-if="isDirty" :saving="saving" @save="save" @discard="discard" />

      <div class="overflow-x-auto doc-table-wrap">
        <table style="border-collapse: collapse; width: 100%; min-width: 900px; table-layout: fixed">
          <colgroup>
            <col style="width: 60px" />
            <col v-for="c in TOTAL_COLS" :key="c" />
          </colgroup>
          <thead>
            <tr>
              <th class="ms-head">{{ t('table.semester') }}</th>
              <th v-for="col in TOTAL_COLS" :key="col" class="ms-head ms-head--num">{{ col }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="sem in SEMESTERS" :key="sem" :style="{ height: sem === 4 ? '50px' : '90px' }">
              <td class="ms-sem">{{ sem }}</td>
              <td
                v-for="slot in slotsForSemester(sem)"
                :key="slot.id"
                :colspan="slot.ects"
                class="ms-slot-cell"
                :style="cellStyle(slot)"
                @dragover="onDragOver($event)"
                @dragenter="dragOverSlotId = slot.id"
                @dragleave="dragOverSlotId = null"
                @drop="onDrop(slot)"
              >
                <div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 4px;">
                  <div style="min-width: 0;">
                    <div class="ms-cell-name">{{ slotDisplayCode(slot) ?? slotDisplayName(slot) }}</div>
                    <div class="ms-cell-sub">{{ slotSubLabel(slot) }}</div>
                  </div>
                  <span
                    v-if="ectsLabel(slot)"
                    style="display: inline-block; font-size: 10px; padding: 1px 4px; border-radius: 2px; font-weight: 700; white-space: nowrap; flex-shrink: 0;"
                    :style="{
                      color: ectsColor(slot),
                      border: `1px solid ${ectsColor(slot)}`,
                      background: theme === 'light' ? `${ectsColor(slot)}18` : 'rgba(255,255,255,0.08)',
                    }"
                  >
                    {{ ectsLabel(slot) }}
                  </span>
                </div>

                <div
                  v-for="entry in entriesForSlot(slot.id)"
                  :key="entry.id"
                  class="ms-mapping-item"
                  :class="{ 'ms-mapping-notpassed': isNotPassed(entry) }"
                  draggable="true"
                  @dragstart="onDragStart(entry)"
                  @dragend="draggingId = null"
                  @click.stop="onItemClick(entry)"
                >
                  <svg v-if="isNotPassed(entry)" class="ms-mapping-x" aria-hidden="true" preserveAspectRatio="none">
                    <line x1="0" y1="0" x2="100%" y2="100%" stroke="rgba(204,0,0,0.85)" stroke-width="1.5" />
                    <line x1="100%" y1="0" x2="0" y2="100%" stroke="rgba(204,0,0,0.85)" stroke-width="1.5" />
                  </svg>
                  <span class="ms-mapping-text">
                    <span style="font-weight: 700">{{ entry.partnerCourseCode }}</span><br />
                    <span style="font-size: 10px; color: #000">{{ entry.partnerCourseName }}</span><br />
                    <span style="font-size: 10px; color: #777">{{ entry.partnerCourseNameHr ?? '-' }}</span><br />
                    <span style="color: #555; font-size: 10px">{{ entry.awardedEcts }} ECTS</span>
                  </span>
                  <button
                    v-if="!isNotPassed(entry)"
                    type="button"
                    class="ms-x-btn"
                    :title="t('mappingScheme.markNotPassed')"
                    @click.stop="markNotPassed(entry)"
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
        </div>
      </div>
    </template>

    <!-- ECTS transfer dialog -->
    <div
      v-if="pendingTransfer"
      style="position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 50;"
      @mousedown.self="cancelTransfer"
    >
      <div style="background: var(--color-dark-2); border: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent); border-radius: 8px; padding: 24px; min-width: 320px;">
        <h3 style="color: var(--color-light); font-size: 14px; font-weight: 600; margin-bottom: 16px">
          {{ t('partnerCourses.moveMapping') }}
        </h3>
        <div v-if="transferSource" style="color: var(--color-primary-light); font-size: 12px; margin-bottom: 4px">
          {{ transferSource.partnerCourseCode }} — {{ transferSource.partnerCourseName }}
        </div>
        <div style="color: var(--color-light); opacity: 0.6; font-size: 11px; margin-bottom: 16px">
          {{ t('partnerCourses.availableEcts') }}: {{ pendingTransfer.max }} ECTS
        </div>
        <label style="display: block; color: var(--color-light); font-size: 12px; margin-bottom: 6px">
          {{ t('partnerCourses.awardedEcts') }}
        </label>
        <input
          v-model.number="transferEcts"
          type="number"
          :min="0.5"
          :max="pendingTransfer.max"
          step="0.5"
          style="width: 100%; background: var(--color-dark); border: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent); color: var(--color-light); padding: 8px; border-radius: 4px; font-size: 13px; margin-bottom: 16px;"
          @keydown.enter.prevent="confirmTransfer"
        />
        <div style="display: flex; gap: 8px; justify-content: flex-end">
          <button
            type="button"
            style="padding: 8px 16px; border: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent); background: transparent; color: var(--color-primary-light); border-radius: 4px; cursor: pointer; font-size: 13px;"
            @click="cancelTransfer"
          >
            {{ t('common.cancel') }}
          </button>
          <button
            type="button"
            style="padding: 8px 16px; background: var(--color-primary); border: none; color: white; border-radius: 4px; cursor: pointer; font-size: 13px; font-weight: 600;"
            @click="confirmTransfer"
          >
            {{ t('common.confirm') }}
          </button>
        </div>
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
.ms-head {
  border: 1px solid #aaa;
  background: #d9d9d9;
  font-size: 10px;
  padding: 4px;
  text-align: center;
  color: #000;
}
.ms-head--num {
  font-weight: normal;
  padding: 4px 0;
}
.ms-sem {
  border: 1px solid #aaa;
  background: #f2f2f2;
  text-align: center;
  font-size: 14px;
  font-weight: bold;
  color: #000;
  vertical-align: middle;
}
.ms-slot-cell {
  border: 1px solid #aaa;
  vertical-align: top;
  padding: 8px;
}
.ms-cell-name {
  font-size: 13px;
  font-weight: 700;
  color: #000;
  line-height: 1.3;
}
.ms-cell-sub {
  font-size: 11px;
  color: #222;
  line-height: 1.3;
  margin-top: 1px;
}
.ms-mapping-item {
  position: relative;
  margin-top: 3px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 4px;
  background: rgba(0, 0, 0, 0.08);
  padding: 2px 4px;
  font-size: 11px;
  cursor: grab;
}
.ms-mapping-text {
  color: #000;
  line-height: 1.3;
}
.ms-mapping-notpassed {
  opacity: 0.7;
  cursor: pointer;
}
.ms-mapping-x {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  overflow: hidden;
}
.ms-x-btn {
  flex-shrink: 0;
  color: #cc0000;
  font-size: 14px;
  line-height: 1;
  background: none;
  border: none;
  cursor: pointer;
  padding: 0;
}
</style>
