<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'
import { useI18n } from 'vue-i18n'
import PartnerCoursePanel from '@/components/exchange/PartnerCoursePanel.vue'
import DocTableGrid from '@/components/exchange/DocTableGrid.vue'
import StatusBadge from '@/components/common/StatusBadge.vue'
import UnsavedChangesBar from '@/components/common/UnsavedChangesBar.vue'
import LearningAgreementHistoryDrawer from '@/components/exchange/LearningAgreementHistoryDrawer.vue'
import ImportPreviewModal from '@/components/exchange/ImportPreviewModal.vue'
import ActionButton from '@/components/common/ActionButton.vue'
import EctsAmountDialog from '@/components/common/EctsAmountDialog.vue'
import PanelHeaderBar from '@/components/common/PanelHeaderBar.vue'
import AuditInfo from '@/components/common/AuditInfo.vue'
import { useExchangeStore } from '@/stores/exchange.store'
import { useExchangePermissions } from '@/composables/useExchangePermissions'
import { useNotification } from '@/composables/useNotification'
import type { HomeSlotResponse, LocalSlotMapping, SlotMode, MappingExportDto } from '@/types/learningAgreement.types'
import type { PartnerCourseResponse } from '@/types/institution.types'
import { documentStatus } from '@/utils/documentStatus'
import { slotMode } from '@/utils/slotMode'
import { useTheme } from '@/composables/useTheme'
import { useConfirm } from '@/composables/useConfirm'
import { slotDisplayName, slotCodeLabel } from '@/utils/slotDisplay'
import { ectsIndicatorColor } from '@/utils/ectsIndicator'
import { useDragAutoScroll } from '@/utils/dragAutoScroll'
import { DOC_TABLE_SEMESTERS, DOC_TABLE_MODE_OUTLINE_COLOR } from '@/utils/docTable'

const props = defineProps<{
  exchangeId: string
  homeProfileName: string
}>()

const { t, locale } = useI18n()
const exchangeStore = useExchangeStore()
const { isCoordinator, isEditable } = useExchangePermissions()
const { theme } = useTheme()
const { confirm } = useConfirm()
const { notifyError } = useNotification()
useDragAutoScroll()

const isSavingLa = ref(false)
const saveError = ref<string | null>(null)
const showHistory = ref(false)
const importDto = ref<MappingExportDto | null>(null)
const importFileInput = ref<HTMLInputElement | null>(null)

async function handleExport() {
  await exchangeStore.exportMappings(props.exchangeId)
}

function handleImportFileChange(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return
  const reader = new FileReader()
  reader.onload = () => {
    try {
      importDto.value = JSON.parse(reader.result as string) as MappingExportDto
    } catch {
      notifyError(t('la.import.invalidJson'))
    }
  }
  reader.readAsText(file)
  ;(e.target as HTMLInputElement).value = ''
}

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

async function backToDraft() {
  await exchangeStore.updateLearningAgreementStatus(props.exchangeId, {
    status: documentStatus.Draft,
  })
  await exchangeStore.fetchExchange(props.exchangeId)
}

async function signExchange() {
  await exchangeStore.updateLearningAgreementStatus(props.exchangeId, {
    status: documentStatus.Approved,
  })
  await exchangeStore.fetchExchange(props.exchangeId)
}

const SEMESTERS = DOC_TABLE_SEMESTERS
const modes: SlotMode[] = [slotMode.AtHome]
const modeOutlineColor = DOC_TABLE_MODE_OUTLINE_COLOR

const isDragging = computed(() => !!exchangeStore.draggingCourse || !!exchangeStore.draggingSlotMapping)
const dragOverSlotId = ref<string | null>(null)
const pendingDrop = ref<{ slot: HomeSlotResponse; course: PartnerCourseResponse } | null>(null)
const pendingEcts = ref<number>(0)
const pendingMove = ref<{
  fromSlotId: string
  toSlotId: string
  localId: string
  max: number
  courseCode: string
  courseName: string
} | null>(null)
const moveEcts = ref<number>(0)
const editingMapping = ref<{ homeSlotId: string; localId: string } | null>(null)
const editingEcts = ref(0)
const ectsInputRef = ref<HTMLInputElement | null>(null)
const mappedCoursesPanel = ref<InstanceType<typeof PartnerCoursePanel> | null>(null)

function lineFor(homeSlotId: string) {
  return exchangeStore.localSlotStates.find((s) => s.homeSlotId === homeSlotId)
}

function sortedMappingsFor(homeSlotId: string) {
  return (lineFor(homeSlotId)?.mappings ?? [])
    .slice()
    .sort((a, b) => a.partnerCourseName.localeCompare(b.partnerCourseName))
}

const totalAwardedEcts = computed(() => {
  let sum = 0
  for (const state of exchangeStore.localSlotStates) {
    for (const m of state.mappings) sum += m.awardedEcts
  }
  return Math.round(sum * 10) / 10
})

const amendmentBadge = computed<number | null>(() => {
  const la = exchangeStore.serverLearningAgreement
  if (!la || la.signedCount < 1) return null
  const n = la.status === documentStatus.Approved ? la.signedCount - 1 : la.signedCount
  return n >= 1 ? n : null
})

function mappingAmendment(amendmentNumber: number | null | undefined): number | null {
  const n = amendmentNumber ?? (exchangeStore.serverLearningAgreement?.signedCount ?? 0)
  return n >= 1 ? n : null
}

function deletedEntriesForSlot(slotId: string) {
  const serverEntries = (exchangeStore.serverLearningAgreement?.entries ?? []).filter(
    (e) => e.homeSlotId === slotId && e.partnerCourseId !== null,
  )
  const localIds = new Set((lineFor(slotId)?.mappings ?? []).map((m) => m.partnerCourseId))
  const wasSigned = (exchangeStore.serverLearningAgreement?.signedCount ?? 0) > 0
  return serverEntries.filter((e) =>
    !localIds.has(e.partnerCourseId!) && (e.isDeleted || wasSigned),
  )
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
  if (!state || state.mode !== slotMode.AtExchange || state.mappings.length === 0) return ''
  return `${mappedEcts(slot)}/${slot.ects}`
}

function ectsColor(slot: HomeSlotResponse): string {
  const state = lineFor(slot.id)
  if (!state || state.mode !== slotMode.AtExchange || state.mappings.length === 0) return 'transparent'
  return ectsIndicatorColor(mappedEcts(slot), slot.ects, theme.value === 'light')
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

  const showOutline = !!state && state.mode === slotMode.AtHome
  const outline = showOutline ? `3px solid ${modeOutlineColor[state!.mode]}` : `1px solid #aaa`
  return {
    backgroundColor: bg,
    outline,
    outlineOffset: showOutline ? '-3px' : '-1px',
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
  const slotDrag = exchangeStore.draggingSlotMapping
  if (slotDrag) {
    if (slotDrag.fromSlotId !== slot.id) {
      const mapping = lineFor(slotDrag.fromSlotId)?.mappings.find((m) => m.localId === slotDrag.localId)
      if (mapping) {
        pendingMove.value = {
          fromSlotId: slotDrag.fromSlotId,
          toSlotId: slot.id,
          localId: slotDrag.localId,
          max: mapping.awardedEcts,
          courseCode: mapping.partnerCourseCode,
          courseName: mapping.partnerCourseName,
        }
        moveEcts.value = mapping.awardedEcts
      }
    }
    exchangeStore.endDrag()
    return
  }
  const course = exchangeStore.draggingCourse
  if (!course) return
  if (lineFor(slot.id)?.mode !== slotMode.AtExchange) {
    exchangeStore.localSetSlotMode(slot.id, slotMode.AtExchange)
  }
  pendingDrop.value = { slot, course }
  exchangeStore.endDrag()
}

function confirmMove() {
  const p = pendingMove.value
  if (!p) return
  if (moveEcts.value > p.max) return
  exchangeStore.localMoveSlotMapping(p.fromSlotId, p.toSlotId, p.localId, Math.max(moveEcts.value, 0.5))
  pendingMove.value = null
}

function cancelMove() {
  pendingMove.value = null
}

function confirmDrop() {
  if (!pendingDrop.value) return
  if (pendingEcts.value > remainingEcts.value) return
  const { slot, course } = pendingDrop.value
  const mapping: LocalSlotMapping = {
    localId: crypto.randomUUID(),
    partnerCourseId: course.id,
    partnerCourseCode: course.code,
    partnerCourseName: course.name,
    partnerCourseNameHr: course.nameHr ?? null,
    awardedEcts: Math.max(pendingEcts.value, 0.5),
  }
  exchangeStore.localAddSlotMapping(slot.id, mapping)
  pendingDrop.value = null
}

function cancelDrop() {
  pendingDrop.value = null
}

async function cycleMode(slot: HomeSlotResponse) {
  if (!isEditable.value || isThesisSlot(slot)) return
  const state = lineFor(slot.id)
  if (state && state.mappings.length > 0) {
    const ok = await confirm({ title: t('la.cycleModeConfirm') })
    if (!ok) return
  }
  if (!state) {
    exchangeStore.localSetSlotMode(slot.id, slotMode.AtHome)
  } else {
    exchangeStore.localRemoveSlotState(slot.id)
  }
}

function removeMapping(homeSlotId: string, localId: string) {
  const partnerCourseId = lineFor(homeSlotId)?.mappings.find((m) => m.localId === localId)?.partnerCourseId
  exchangeStore.localRemoveSlotMapping(homeSlotId, localId)
  if (partnerCourseId) exchangeStore.unstagePartnerCourse(partnerCourseId)
  const state = lineFor(homeSlotId)
  if (state && state.mode === slotMode.AtExchange && state.mappings.length === 0) {
    exchangeStore.localRemoveSlotState(homeSlotId)
  }
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

</script>

<template>
  <div>
    <!-- History drawer + Import modal -->
    <LearningAgreementHistoryDrawer
      v-if="showHistory"
      :exchange-id="exchangeId"
      :guest-mode="exchangeStore.guestMode"
      @close="showHistory = false"
    />
    <ImportPreviewModal
      v-if="importDto"
      :dto="importDto"
      :exchange-id="exchangeId"
      @close="importDto = null"
      @imported="importDto = null"
    />
    <input
      ref="importFileInput"
      type="file"
      accept=".json"
      style="display: none"
      @change="handleImportFileChange"
    />

    <!-- Status + Actions bar -->
    <PanelHeaderBar :home-profile-name="homeProfileName">
      <template #left>
        <StatusBadge
          v-if="exchangeStore.serverLearningAgreement"
          :status="exchangeStore.serverLearningAgreement.status"
        />
        <span
          v-if="amendmentBadge !== null"
          class="rounded-full border border-primary/30 bg-primary/10 px-2.5 py-0.5 text-xs font-semibold text-primary-light"
        >{{ t('la.amendmentLabel', { n: amendmentBadge }) }}</span>
        <!-- Export / Import / History -->
        <div style="display: flex; gap: 6px;">
          <ActionButton @click="handleExport">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
            {{ t('la.actions.export') }}
          </ActionButton>
          <ActionButton @click="importFileInput?.click()">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
            {{ t('la.actions.import') }}
          </ActionButton>
          <ActionButton @click="showHistory = true">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
            {{ t('la.actions.history') }}
          </ActionButton>
        </div>
      </template>
      <template #right>
        <!-- Coordinator actions -->
        <template v-if="isCoordinator">
          <button
            v-if="exchangeStore.serverLearningAgreement?.status === documentStatus.Draft"
            type="button"
            class="rounded-lg bg-green-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-green-500"
            @click="signExchange"
          >
            {{ t('exchange.actions.sign') }}
          </button>
          <button
            v-else-if="exchangeStore.serverLearningAgreement?.status === documentStatus.Approved"
            type="button"
            class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-muted transition hover:bg-slate-700/40"
            @click="backToDraft"
          >
            {{ t('exchange.actions.backToDraft') }}
          </button>
        </template>
      </template>
    </PanelHeaderBar>

    <!-- Audit info -->
    <AuditInfo
      :last-modified-at="exchangeStore.serverLearningAgreement?.lastModifiedAt"
      :last-modified-by-name="exchangeStore.serverLearningAgreement?.lastModifiedByName"
      :signed-at="exchangeStore.serverLearningAgreement?.signedAt"
      :signed-by-name="exchangeStore.serverLearningAgreement?.signedByName"
    />
    <UnsavedChangesBar
      v-if="isEditable && exchangeStore.isDirty"
      :saving="isSavingLa"
      @save="saveLa"
      @discard="discardLa"
    />

    <!-- Table -->
    <DocTableGrid v-if="exchangeStore.serverLearningAgreement">
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
          <div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 4px;">
            <div style="min-width: 0;">
              <div class="la-cell-code">{{ slotCodeLabel(slot) }}</div>
              <div class="la-cell-name">{{ slotDisplayName(slot, locale) }}</div>
            </div>
            <div style="display: flex; flex-direction: row; align-items: flex-start; gap: 3px; flex-shrink: 0;">
              <span
                v-if="ectsLabel(slot)"
                style="display: inline-block; font-size: 10px; padding: 1px 4px; border-radius: 2px; font-weight: 700; white-space: nowrap;"
                :style="{
                  color: ectsColor(slot),
                  border: `1px solid ${ectsColor(slot)}`,
                  background: theme === 'light' ? `${ectsColor(slot)}18` : 'rgba(255,255,255,0.08)',
                }"
              >
                {{ ectsLabel(slot) }}
              </span>
            </div>
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
            <span
              v-if="mappingAmendment(removed.isDeleted ? removed.amendmentNumber : null) !== null"
              class="la-mapping-amendment"
            >{{ t('la.amendmentLabel', { n: mappingAmendment(removed.isDeleted ? removed.amendmentNumber : null) }) }}</span>
            <span class="la-mapping-text">
              <span style="font-size: 10px; color: #333">{{ removed.partnerCourseCode }}</span><br />
              <span style="font-weight: 700; color: #000">{{ removed.partnerCourseName }}</span><br />
              <span style="font-size: 10px; color: #777">{{ removed.partnerCourseNameHr ?? '-' }}</span><br />
              <span style="color: #555; font-size: 10px">{{ removed.awardedEcts }} ECTS</span>
            </span>
          </div>

          <div
            v-for="mapping in sortedMappingsFor(slot.id)"
            :key="mapping.localId"
            class="la-mapping-item"
            :draggable="isEditable"
            @click.stop
            @dragstart.stop="isEditable && exchangeStore.startSlotDrag(slot.id, mapping.localId)"
            @dragend.stop="exchangeStore.endDrag()"
          >
            <span
              v-if="mappingAmendment(mapping.amendmentNumber) !== null"
              class="la-mapping-amendment"
            >{{ t('la.amendmentLabel', { n: mappingAmendment(mapping.amendmentNumber) }) }}</span>
            <span class="la-mapping-text">
              <span style="font-size: 10px; color: #333">{{ mapping.partnerCourseCode }}</span><br />
              <span style="font-weight: 700; color: #000">{{ mapping.partnerCourseName }}</span><br />
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
                :title="isEditable ? t('la.clickToEditEcts') : ''"
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

      <template #legend>
        <div v-for="mode in modes" :key="mode" style="display: flex; align-items: center; gap: 6px">
          <span style="display: inline-block; width: 12px; height: 12px" :style="{ background: modeOutlineColor[mode] }" />
          <span style="font-size: 11px; color: var(--color-primary-light)">{{ t(`slotMode.${mode}`) }}</span>
        </div>
        <span style="font-size: 11px; color: var(--color-light); opacity: 0.6; margin-left: 8px">
          {{ t('table.clickToChange') }}
        </span>
      </template>
    </DocTableGrid>

    <!-- ECTS input popup -->
    <EctsAmountDialog
      v-if="pendingDrop"
      :title="t('partnerCourses.addMapping')"
      :course-code="pendingDrop.course.code"
      :course-name="pendingDrop.course.name"
      :max="remainingEcts"
      :total-ects="pendingDrop.course.ects"
      :model-value="pendingEcts"
      @update:model-value="pendingEcts = $event"
      @confirm="confirmDrop"
      @cancel="cancelDrop"
    />

    <EctsAmountDialog
      v-if="pendingMove"
      :title="t('partnerCourses.moveMapping')"
      :course-code="pendingMove.courseCode"
      :course-name="pendingMove.courseName"
      :max="pendingMove.max"
      :model-value="moveEcts"
      @update:model-value="moveEcts = $event"
      @confirm="confirmMove"
      @cancel="cancelMove"
    />

    <!-- Course panels (editable only) -->
    <div v-if="isEditable && exchangeStore.exchange" class="mt-6 flex gap-6 items-start">
      <div class="min-w-0 basis-[60%] rounded-xl border border-primary/20 bg-dark-2 p-4">
        <h3 class="mb-2 text-sm font-semibold text-primary-light">
          {{ t('partnerCourses.availableCourses') }}
        </h3>
        <p class="mb-3 text-xs text-light/60">{{ t('partnerCourses.dragHint') }}</p>
        <PartnerCoursePanel
          :partner-institution-id="exchangeStore.exchange.partnerInstitutionId"
          :exchange-id="exchangeId"
          variant="available"
        />
      </div>
      <div class="min-w-0 basis-[40%] rounded-xl border border-primary/20 bg-dark-2 p-4">
        <h3 class="mb-2 flex items-center justify-between text-sm font-semibold text-success">
          <span>{{ t('partnerCourses.mappedCourses') }}</span>
          <span class="text-xs font-normal text-light/60">{{ totalAwardedEcts }} / {{ mappedCoursesPanel?.mappedCoursesTotalEcts ?? 0 }} ECTS</span>
        </h3>
        <PartnerCoursePanel
          ref="mappedCoursesPanel"
          :partner-institution-id="exchangeStore.exchange.partnerInstitutionId"
          :exchange-id="exchangeId"
          variant="mapped"
        />
      </div>
    </div>

  </div>
</template>

<style scoped>
.la-slot-cell {
  border: 1px solid #aaa;
  vertical-align: top;
  padding: 8px;
}

.la-cell-name {
  font-size: 11px;
  font-weight: 700;
  color: #000;
  line-height: 1.3;
}

.la-cell-code {
  font-size: 13px;
  font-weight: 400;
  color: #222;
  line-height: 1.3;
  margin-top: 1px;
}

.la-mapping-item {
  position: relative;
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
  overflow: hidden;
}

.la-mapping-amendment {
  position: absolute;
  top: 4px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 2;
  font-size: 9px;
  font-weight: 800;
  letter-spacing: 0.3px;
  color: #cc0000;
  line-height: 1.2;
  pointer-events: none;
}
</style>
