<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import CourseUrlLink from '@/components/common/CourseUrlLink.vue'
import { useTheme } from '@/composables/useTheme'
import { slotMode } from '@/utils/slotMode'
import { slotDisplayName, slotCodeLabel } from '@/utils/slotDisplay'
import { ectsIndicatorColor } from '@/utils/ectsIndicator'
import { DOC_TABLE_MODE_OUTLINE_COLOR } from '@/utils/docTable'
import type {
  HomeSlotResponse,
  LearningAgreementEntryResponse,
  LocalSlotMapping,
  LocalSlotState,
} from '@/types/learningAgreement.types'

/** One home slot of the LA grid: its mode, the courses placed in it, and approved courses taken out (struck through). */
const props = defineProps<{
  homeSlot: HomeSlotResponse
  state: LocalSlotState | undefined
  /** Approved courses no longer in the draft, shown struck through. */
  removed: LearningAgreementEntryResponse[]
  editable: boolean
  /** Number of approved versions, for the amendment label of new courses. */
  signedCount: number
  /** A course is being dragged or is armed: every slot is a target. */
  targeting: boolean
  dragOver: boolean
}>()

const emit = defineEmits<{
  activate: []
  drop: []
  dragenter: []
  dragleave: []
  startDrag: [localId: string]
  endDrag: []
  remove: [localId: string]
  updateEcts: [localId: string, ects: number]
}>()

const { t, locale } = useI18n()
const { theme } = useTheme()

const isThesis = computed(() => props.homeSlot.courseTypeNameEn === 'Master thesis')
const interactive = computed(() => props.editable && !isThesis.value)

const mappings = computed(() =>
  (props.state?.mappings ?? []).slice().sort((a, b) => a.partnerCourseName.localeCompare(b.partnerCourseName)),
)

const hasCourses = computed(() => props.state?.mode === slotMode.AtExchange && mappings.value.length > 0)
const placedEcts = computed(() => Math.round(mappings.value.reduce((sum, m) => sum + m.awardedEcts, 0) * 10) / 10)
const ectsLabel = computed(() => (hasCourses.value ? `${placedEcts.value}/${props.homeSlot.ects}` : ''))
const ectsColor = computed(() =>
  hasCourses.value ? ectsIndicatorColor(placedEcts.value, props.homeSlot.ects, theme.value === 'light') : 'transparent',
)

/** "A1", "A2", ... (or "I1" in Croatian): the amendment that added a course; new courses join the next one. */
function amendment(amendmentNumber: number | null | undefined): number | null {
  const n = amendmentNumber ?? props.signedCount
  return n >= 1 ? n : null
}

const cellStyle = computed<Record<string, string>>(() => {
  if (props.dragOver) {
    return {
      backgroundColor: 'color-mix(in srgb, var(--color-primary) 20%, transparent)',
      outline: '2px dashed var(--color-primary)',
      outlineOffset: '-2px',
      cursor: 'copy',
    }
  }
  if (props.targeting) {
    return { backgroundColor: props.homeSlot.color, outline: '2px dashed var(--color-primary)', outlineOffset: '-2px', cursor: 'copy' }
  }
  const atHome = props.state?.mode === slotMode.AtHome
  return {
    backgroundColor: props.homeSlot.color,
    outline: atHome ? `3px solid ${DOC_TABLE_MODE_OUTLINE_COLOR[slotMode.AtHome]}` : '1px solid #aaa',
    outlineOffset: atHome ? '-3px' : '-1px',
    cursor: interactive.value ? 'pointer' : 'default',
  }
})

// Inline ECTS editing of a placed course
const editingId = ref<string | null>(null)
const editingEcts = ref(0)
const ectsInput = ref<HTMLInputElement | null>(null)
// Function ref: a plain ref inside v-for would collect an array of inputs.
function setEctsInput(el: unknown) {
  ectsInput.value = el instanceof HTMLInputElement ? el : null
}

function startEdit(mapping: LocalSlotMapping) {
  if (!props.editable) return
  editingId.value = mapping.localId
  editingEcts.value = mapping.awardedEcts
  nextTick(() => ectsInput.value?.focus())
}

function saveEdit() {
  if (editingId.value === null) return
  const id = editingId.value
  editingId.value = null
  emit('updateEcts', id, Math.max(0.5, editingEcts.value))
}
</script>

<template>
  <td
    :colspan="homeSlot.ects"
    :style="cellStyle"
    class="la-slot-cell"
    :role="interactive ? 'button' : undefined"
    :tabindex="interactive ? 0 : undefined"
    :aria-label="interactive ? `${slotCodeLabel(homeSlot)} ${slotDisplayName(homeSlot, locale)}`.trim() : undefined"
    @click="interactive && emit('activate')"
    @keydown.enter.self.prevent="interactive && emit('activate')"
    @keydown.space.self.prevent="interactive && emit('activate')"
    @dragover.prevent
    @dragenter="emit('dragenter')"
    @dragleave="emit('dragleave')"
    @drop.prevent="emit('drop')"
  >
    <div class="la-cell-head">
      <div style="min-width: 0">
        <div class="la-cell-code">{{ slotCodeLabel(homeSlot) }}</div>
        <div class="la-cell-name">{{ slotDisplayName(homeSlot, locale) }}</div>
      </div>
      <span
        v-if="ectsLabel"
        class="la-cell-ects"
        :style="{
          color: ectsColor,
          border: `1px solid ${ectsColor}`,
          background: theme === 'light' ? `${ectsColor}18` : 'rgba(255,255,255,0.08)',
        }"
      >
        {{ ectsLabel }}
      </span>
    </div>

    <div v-for="entry in removed" :key="`removed-${entry.id}`" class="la-mapping-item la-mapping-removed">
      <svg class="la-mapping-x" aria-hidden="true" preserveAspectRatio="none">
        <line x1="0" y1="0" x2="100%" y2="100%" stroke="rgba(204,0,0,0.75)" stroke-width="1.5" />
        <line x1="100%" y1="0" x2="0" y2="100%" stroke="rgba(204,0,0,0.75)" stroke-width="1.5" />
      </svg>
      <!-- Removed in an earlier amendment, or (still in the approved version) removed by the next one. -->
      <span v-if="amendment(entry.isDeleted ? entry.amendmentNumber : null) !== null" class="la-mapping-amendment">
        {{ t('la.amendmentLabel', { n: amendment(entry.isDeleted ? entry.amendmentNumber : null) }) }}
      </span>
      <span class="la-mapping-text">
        <span class="la-mapping-code-line">
          <span class="la-mapping-code">{{ entry.partnerCourseCode }}</span>
          <CourseUrlLink v-if="entry.partnerCourseUrl" block :url="entry.partnerCourseUrl" :title="t('admin.institutions.courseUrl')" />
        </span><br />
        <span class="la-mapping-name">{{ entry.partnerCourseName }}</span><br />
        <span class="la-mapping-name-hr">{{ entry.partnerCourseNameHr ?? '-' }}</span><br />
        <span class="la-mapping-ects">{{ entry.awardedEcts }} ECTS</span>
      </span>
    </div>

    <div
      v-for="mapping in mappings"
      :key="mapping.localId"
      class="la-mapping-item"
      :draggable="editable"
      @click.stop
      @dragstart.stop="editable && emit('startDrag', mapping.localId)"
      @dragend.stop="emit('endDrag')"
    >
      <span v-if="amendment(mapping.amendmentNumber) !== null" class="la-mapping-amendment">
        {{ t('la.amendmentLabel', { n: amendment(mapping.amendmentNumber) }) }}
      </span>
      <span class="la-mapping-text">
        <span class="la-mapping-code-line">
          <span class="la-mapping-code">{{ mapping.partnerCourseCode }}</span>
          <CourseUrlLink v-if="mapping.partnerCourseUrl" block :url="mapping.partnerCourseUrl" :title="t('admin.institutions.courseUrl')" />
        </span><br />
        <span class="la-mapping-name">{{ mapping.partnerCourseName }}</span><br />
        <span class="la-mapping-name-hr">{{ mapping.partnerCourseNameHr ?? '-' }}</span><br />
        <template v-if="editingId === mapping.localId">
          <input
            :ref="setEctsInput"
            v-model.number="editingEcts"
            type="number"
            min="0.5"
            step="0.5"
            class="la-ects-input"
            :aria-label="t('la.clickToEditEcts')"
            @blur="saveEdit()"
            @keydown.enter.prevent="saveEdit()"
            @keydown.escape.prevent="editingId = null"
            @click.stop
          />
          <span class="la-mapping-ects" style="margin-left: 2px">ECTS</span>
        </template>
        <button
          v-else-if="editable"
          type="button"
          class="la-mapping-ects la-mapping-ects--editable"
          :title="t('la.clickToEditEcts')"
          @click.stop="startEdit(mapping)"
        >
          {{ mapping.awardedEcts }} ECTS
        </button>
        <span v-else class="la-mapping-ects">{{ mapping.awardedEcts }} ECTS</span>
      </span>
      <button
        v-if="editable"
        type="button"
        class="la-mapping-remove"
        :aria-label="`${t('common.remove')}: ${mapping.partnerCourseCode}`"
        @click.stop="emit('remove', mapping.localId)"
      >
        &times;
      </button>
    </div>
  </td>
</template>

<style scoped>
.la-slot-cell {
  border: 1px solid #aaa;
  vertical-align: top;
  padding: 8px;
}

.la-slot-cell:focus-visible {
  outline: 2px solid var(--color-primary) !important;
  outline-offset: -2px !important;
}

.la-cell-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 4px;
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

.la-cell-ects {
  display: inline-block;
  flex-shrink: 0;
  font-size: 10px;
  padding: 1px 4px;
  border-radius: 2px;
  font-weight: 700;
  white-space: nowrap;
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

.la-mapping-code-line {
  display: inline-flex;
  align-items: center;
  gap: 3px;
}

.la-mapping-code {
  font-size: 10px;
  color: #333;
}

.la-mapping-name {
  font-weight: 700;
  color: #000;
}

.la-mapping-name-hr {
  font-size: 10px;
  color: #777;
}

.la-mapping-ects {
  color: #555;
  font-size: 10px;
}

.la-mapping-ects--editable {
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  text-decoration: underline dotted;
}

.la-ects-input {
  width: 52px;
  font-size: 11px;
  padding: 1px 3px;
  background: var(--color-dark);
  color: var(--color-light);
  border: 1px solid var(--color-primary);
  border-radius: 3px;
}

.la-mapping-remove {
  color: #cc0000;
  font-size: 14px;
  line-height: 1;
  background: none;
  border: none;
  cursor: pointer;
  padding: 0;
  margin-left: 4px;
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
