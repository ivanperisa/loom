<script setup lang="ts">
import { computed, ref, watch, onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import PartnerCoursePanel from '@/components/exchange/PartnerCoursePanel.vue'
import DocTableGrid from '@/components/exchange/DocTableGrid.vue'
import LaSlotCell from '@/components/exchange/la/LaSlotCell.vue'
import LaDocumentActions from '@/components/exchange/la/LaDocumentActions.vue'
import StatusBadge from '@/components/common/StatusBadge.vue'
import UnsavedChangesBar from '@/components/common/UnsavedChangesBar.vue'
import EctsAmountDialog from '@/components/common/EctsAmountDialog.vue'
import PanelHeaderBar from '@/components/common/PanelHeaderBar.vue'
import AuditInfo from '@/components/common/AuditInfo.vue'
import { useLaDraftStore } from '@/stores/laDraft.store'
import { useExchangeContext } from '@/composables/useExchangeContext'
import { useLaDropFlow } from '@/composables/useLaDropFlow'
import { useLearningAgreementMutations } from '@/queries/exchange.queries'
import { describeApiError } from '@/utils/apiError'
import { formatDate } from '@/utils/formatDate'
import { useNotification } from '@/composables/useNotification'
import { useConfirm } from '@/composables/useConfirm'
import type { HomeSlotResponse, SlotMode } from '@/types/learningAgreement.types'
import { documentStatus } from '@/utils/documentStatus'
import { slotMode } from '@/utils/slotMode'
import { slotDisplayName, slotCodeLabel } from '@/utils/slotDisplay'
import { useDragAutoScroll } from '@/utils/dragAutoScroll'
import { DOC_TABLE_SEMESTERS, DOC_TABLE_MODE_OUTLINE_COLOR } from '@/utils/docTable'

const props = defineProps<{
  exchangeId: string
  homeProfileName: string
}>()

const { t, locale } = useI18n()
const draft = useLaDraftStore()
const { exchange, learningAgreement, isCoordinator, isEditable, isConcluded } = useExchangeContext()
const laMutations = useLearningAgreementMutations(() => props.exchangeId)
const dropFlow = useLaDropFlow()
const { pendingDrop, pendingEcts, remainingEcts, pendingMove, moveEcts } = dropFlow
const { confirm } = useConfirm()
const { notifyError } = useNotification()
useDragAutoScroll()

// The draft follows the saved LA, unless it holds unsaved changes for this exchange.
watch(
  learningAgreement,
  (la) => {
    if (la && draft.canReload(props.exchangeId)) draft.load(props.exchangeId, la)
  },
  { immediate: true },
)

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') draft.disarm()
}
onMounted(() => window.addEventListener('keydown', onKeydown))
onUnmounted(() => window.removeEventListener('keydown', onKeydown))

// Saving and status

function courseLabel(id: number) {
  const mapping = draft.slotStates.flatMap((s) => s.mappings).find((m) => m.partnerCourseId === id)
  return mapping ? `${mapping.partnerCourseCode} (${mapping.partnerCourseName})` : undefined
}

function slotLabel(id: number) {
  const slot = learningAgreement.value?.slots.find((s) => s.id === id)
  return slot ? `${slotCodeLabel(slot)} ${slotDisplayName(slot, locale.value)}`.trim() : undefined
}

async function saveLa() {
  try {
    draft.load(props.exchangeId, await laMutations.save.mutateAsync(draft.toRequest()))
  } catch (error) {
    // Name the course or slot the server refers to (the save does not toast by itself).
    const { title, message } = describeApiError(error, { course: courseLabel, slot: slotLabel })
    notifyError(title, message)
  }
}

function discardLa() {
  if (learningAgreement.value) draft.load(props.exchangeId, learningAgreement.value)
}

function setStatus(status: typeof documentStatus.Draft | typeof documentStatus.Approved) {
  laMutations.setStatus.mutate({ status })
}

/** "A1": the amendment being prepared (draft after an approval) or the latest one (approved). */
const amendmentBadge = computed<number | null>(() => {
  const la = learningAgreement.value
  if (!la || la.signedCount < 1) return null
  const n = la.status === documentStatus.Approved ? la.signedCount - 1 : la.signedCount
  return n >= 1 ? n : null
})

// The grid

const modes: SlotMode[] = [slotMode.AtHome]

/** Slots per semester row, in position order. Computed once per LA, not on every render. */
const slotsBySemester = computed(() => {
  const rows = new Map<number, HomeSlotResponse[]>()
  for (const sem of DOC_TABLE_SEMESTERS) rows.set(sem, [])
  for (const slot of learningAgreement.value?.slots ?? []) rows.get(slot.semester)?.push(slot)
  for (const row of rows.values()) row.sort((a, b) => a.slotPosition - b.slotPosition)
  return rows
})

const stateBySlot = computed(() => new Map(draft.slotStates.map((s) => [s.homeSlotId, s])))

/** Approved courses no longer in the draft, per slot: shown struck through ("removed in An"). */
const removedBySlot = computed(() => {
  const la = learningAgreement.value
  const rows = new Map<number, NonNullable<typeof la>['entries']>()
  if (!la) return rows
  const wasApproved = la.signedCount > 0
  for (const entry of la.entries) {
    if (entry.partnerCourseId === null || !(entry.isDeleted || wasApproved)) continue
    const stillPlaced = stateBySlot.value.get(entry.homeSlotId)?.mappings.some((m) => m.partnerCourseId === entry.partnerCourseId)
    if (stillPlaced) continue
    rows.set(entry.homeSlotId, [...(rows.get(entry.homeSlotId) ?? []), entry])
  }
  return rows
})

const targeting = computed(() => !!draft.draggingCourse || !!draft.draggingSlotMapping || !!draft.armedCourse)
const dragOverSlotId = ref<number | null>(null)

/** Clicking a slot places the armed course, or toggles "taken at home". */
async function activateSlot(slot: HomeSlotResponse) {
  if (draft.armedCourse) {
    dropFlow.place(slot, draft.armedCourse)
    draft.disarm()
    return
  }
  const state = stateBySlot.value.get(slot.id)
  if (state && state.mappings.length > 0 && !(await confirm({ title: t('la.cycleModeConfirm') }))) return
  if (state) draft.removeSlotState(slot.id)
  else draft.setSlotMode(slot.id, slotMode.AtHome)
}

function dropOn(slot: HomeSlotResponse) {
  dragOverSlotId.value = null
  dropFlow.drop(slot)
}

function removeMapping(slot: HomeSlotResponse, localId: string) {
  const state = stateBySlot.value.get(slot.id)
  const courseId = state?.mappings.find((m) => m.localId === localId)?.partnerCourseId
  draft.removeMapping(slot.id, localId)
  if (courseId !== undefined) draft.unstagePartnerCourse(courseId)
  // A slot whose last course was taken out goes back to "not decided".
  if (state?.mode === slotMode.AtExchange && state.mappings.length === 0) draft.removeSlotState(slot.id)
}

// Course lists below the grid

const totalAwardedEcts = computed(
  () => Math.round(draft.slotStates.reduce((sum, s) => sum + s.mappings.reduce((a, m) => a + m.awardedEcts, 0), 0) * 10) / 10,
)
const mappedCoursesPanel = ref<InstanceType<typeof PartnerCoursePanel> | null>(null)
</script>

<template>
  <div>
    <PanelHeaderBar :home-profile-name="homeProfileName">
      <template #left>
        <StatusBadge v-if="learningAgreement" :status="learningAgreement.status" />
        <span
          v-if="amendmentBadge !== null"
          class="rounded-full border border-primary/30 bg-primary/10 px-2.5 py-0.5 text-xs font-semibold text-primary-text"
        >{{ t('la.amendmentLabel', { n: amendmentBadge }) }}</span>
        <LaDocumentActions :exchange-id="exchangeId" :editable="isEditable" />
      </template>
      <template #right>
        <template v-if="isCoordinator && !isConcluded">
          <button
            v-if="learningAgreement?.status === documentStatus.Draft"
            type="button"
            class="rounded-lg bg-green-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-green-500 disabled:opacity-60"
            :disabled="laMutations.setStatus.isPending.value || draft.isDirty"
            :title="draft.isDirty ? t('la.unsavedChanges') : undefined"
            @click="setStatus(documentStatus.Approved)"
          >
            {{ t('exchange.actions.sign') }}
          </button>
          <button
            v-else-if="learningAgreement?.status === documentStatus.Approved"
            type="button"
            class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-muted transition hover:bg-slate-700/40 disabled:opacity-60"
            :disabled="laMutations.setStatus.isPending.value"
            @click="setStatus(documentStatus.Draft)"
          >
            {{ t('exchange.actions.backToDraft') }}
          </button>
        </template>
      </template>
    </PanelHeaderBar>

    <AuditInfo
      :last-modified-at="learningAgreement?.lastModifiedAt"
      :last-modified-by-name="learningAgreement?.lastModifiedByName"
      :signed-at="learningAgreement?.signedAt"
      :signed-by-name="learningAgreement?.signedByName"
    />
    <p v-if="isConcluded" class="la-concluded" role="status">
      {{ t('la.concluded', {
        date: formatDate(learningAgreement?.concludedAt ?? '', locale),
        name: learningAgreement?.concludedByName ?? '—',
      }) }}
    </p>
    <UnsavedChangesBar
      v-if="isEditable && draft.isDirty"
      :saving="laMutations.save.isPending.value"
      @save="saveLa"
      @discard="discardLa"
    />

    <!-- Armed course: click a slot to place it -->
    <div
      v-if="draft.armedCourse"
      class="sticky top-0 z-10 mb-2 flex items-center justify-between gap-3 rounded-lg border border-primary/20 bg-dark-2 px-4 py-2"
      role="status"
    >
      <span class="flex flex-wrap items-baseline gap-x-2">
        <span class="text-xs font-bold text-light">{{ draft.armedCourse.code }}</span>
        <span class="text-sm font-medium text-light">{{ draft.armedCourse.name }}</span>
        <span v-if="draft.armedCourse.nameHr" class="text-xs text-light/60">{{ draft.armedCourse.nameHr }}</span>
        <span class="text-xs text-light/60">- {{ t('partnerCourses.armedHint') }}</span>
      </span>
      <button
        type="button"
        class="shrink-0 rounded-lg border border-primary/20 px-3 py-1 text-xs font-medium text-light/60 transition hover:border-primary hover:text-primary-text"
        @click="draft.disarm()"
      >
        {{ t('partnerCourses.armedCancel') }}
      </button>
    </div>

    <DocTableGrid v-if="learningAgreement">
      <tr v-for="sem in DOC_TABLE_SEMESTERS" :key="sem" :style="{ height: sem === 4 ? '50px' : '90px' }">
        <th scope="row" class="la-semester-cell">{{ sem }}</th>
        <LaSlotCell
          v-for="slot in slotsBySemester.get(sem)"
          :key="slot.id"
          :home-slot="slot"
          :state="stateBySlot.get(slot.id)"
          :removed="removedBySlot.get(slot.id) ?? []"
          :editable="isEditable"
          :signed-count="learningAgreement.signedCount"
          :targeting="targeting"
          :drag-over="dragOverSlotId === slot.id"
          @activate="activateSlot(slot)"
          @dragenter="dragOverSlotId = slot.id"
          @dragleave="dragOverSlotId = null"
          @drop="dropOn(slot)"
          @start-drag="draft.startSlotDrag(slot.id, $event)"
          @end-drag="draft.endDrag()"
          @remove="removeMapping(slot, $event)"
          @update-ects="(localId, ects) => draft.updateMappingEcts(slot.id, localId, ects)"
        />
      </tr>

      <template #legend>
        <div v-for="mode in modes" :key="mode" class="flex items-center gap-1.5">
          <span class="inline-block h-3 w-3" :style="{ background: DOC_TABLE_MODE_OUTLINE_COLOR[mode] }" />
          <span class="text-[11px] text-primary-light">{{ t(`slotMode.${mode}`) }}</span>
        </div>
        <span v-if="isEditable" class="ml-2 text-[11px] text-light opacity-60">{{ t('table.clickToChange') }}</span>
      </template>
    </DocTableGrid>

    <EctsAmountDialog
      v-if="pendingDrop"
      :title="t('partnerCourses.addMapping')"
      :course-code="pendingDrop.course.code"
      :course-name="pendingDrop.course.name"
      :max="remainingEcts"
      :total-ects="pendingDrop.course.ects"
      :model-value="pendingEcts"
      @update:model-value="pendingEcts = $event"
      @confirm="dropFlow.confirmDrop"
      @cancel="dropFlow.cancelDrop"
    />

    <EctsAmountDialog
      v-if="pendingMove"
      :title="t('partnerCourses.moveMapping')"
      :course-code="pendingMove.courseCode"
      :course-name="pendingMove.courseName"
      :max="pendingMove.max"
      :model-value="moveEcts"
      @update:model-value="moveEcts = $event"
      @confirm="dropFlow.confirmMove"
      @cancel="dropFlow.cancelMove"
    />

    <!-- Course lists (editable only) -->
    <div v-if="isEditable && exchange" class="mt-6 flex items-start gap-6">
      <section class="min-w-0 basis-[60%] rounded-xl border border-primary/20 bg-dark-2 p-4" aria-labelledby="la-available-courses">
        <h3 id="la-available-courses" class="mb-2 text-sm font-semibold text-primary-text">
          {{ t('partnerCourses.availableCourses') }}
        </h3>
        <p class="mb-3 text-xs text-light/60">{{ t('partnerCourses.dragHint') }}</p>
        <PartnerCoursePanel :exchange-id="exchangeId" variant="available" />
      </section>
      <section class="min-w-0 basis-[40%] rounded-xl border border-primary/20 bg-dark-2 p-4" aria-labelledby="la-mapped-courses">
        <h3 id="la-mapped-courses" class="mb-2 flex items-center justify-between text-sm font-semibold text-success-text">
          <span>{{ t('partnerCourses.mappedCourses') }}</span>
          <span class="text-xs font-normal text-light/60">{{ totalAwardedEcts }} / {{ mappedCoursesPanel?.mappedCoursesTotalEcts ?? 0 }} ECTS</span>
        </h3>
        <PartnerCoursePanel ref="mappedCoursesPanel" :exchange-id="exchangeId" variant="mapped" />
      </section>
    </div>
  </div>
</template>

<style scoped>
.la-semester-cell {
  border: 1px solid #aaa;
  background: #f2f2f2;
  text-align: center;
  font-size: 14px;
  font-weight: bold;
  color: #000;
  padding: 4px 2px;
  vertical-align: middle;
}

.la-concluded {
  margin: 0 0 12px;
  padding: 8px 12px;
  border-radius: 8px;
  font-size: 12px;
  color: var(--color-light);
  background: color-mix(in srgb, var(--color-primary) 10%, transparent);
  border: 1px solid color-mix(in srgb, var(--color-primary) 30%, transparent);
}
</style>
