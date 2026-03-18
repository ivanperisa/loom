<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useExchangeStore } from '@/stores/exchange.store'
import type {
  LearningAgreementResponse,
  CourseSlotResponse,
  SlotStateResponse,
  SlotMode,
} from '@/types/exchange.types'
import type { ForeignCourseResponse } from '@/types/institution.types'

const props = withDefaults(defineProps<{
  learningAgreement: LearningAgreementResponse
  exchangeId: string
  readonly?: boolean
}>(), { readonly: false })

const { t, locale } = useI18n()
const exchangeStore = useExchangeStore()

const TOTAL_COLS = 30
const SEMESTERS = [1, 2, 3, 4]
const modes: SlotMode[] = ['AtHome', 'AtExchange', 'AfterExchange']

function mappedEcts(slot: CourseSlotResponse): number {
  const state = slotState(slot.id)
  if (!state) return 0
  return state.mappings.reduce((sum, m) => sum + m.awardedEcts, 0)
}

function ectsLabel(slot: CourseSlotResponse): string {
  const state = slotState(slot.id)
  if (!state || state.mode !== 'AtExchange') return ''
  const mapped = mappedEcts(slot)
  return `${mapped}/${slot.ects}`
}

function ectsColor(slot: CourseSlotResponse): string {
  const state = slotState(slot.id)
  if (!state || state.mode !== 'AtExchange') return 'transparent'
  const mapped = mappedEcts(slot)
  if (mapped === 0) return '#94a3b8'
  if (mapped < slot.ects) return '#f59e0b'
  if (mapped === slot.ects) return '#22c55e'
  return '#ef4444' // over-mapped
}

function slotsForSemester(sem: number): CourseSlotResponse[] {
  return props.learningAgreement.slots
    .filter(s => s.semester === sem)
    .sort((a, b) => a.colStart - b.colStart)
}

function slotState(courseSlotId: string): SlotStateResponse | undefined {
  return props.learningAgreement.slotStates.find(s => s.courseSlotId === courseSlotId)
}

const modeOutlineColor: Record<SlotMode, string> = {
  AtHome: '#64748b',
  AtExchange: '#218CD9',
  AfterExchange: '#f59e0b',
}

// Drag & drop state
const isDragging = computed(() => !!exchangeStore.draggingCourse)
const dragOverSlotId = ref<string | null>(null)

// ECTS popup state
const pendingDrop = ref<{ slot: CourseSlotResponse; course: ForeignCourseResponse } | null>(null)
const pendingEcts = ref<number>(0)

watch(() => pendingDrop.value, (val) => {
  if (val) pendingEcts.value = val.course.ects
})

function cellStyle(slot: CourseSlotResponse): Record<string, string> {
  const state = slotState(slot.id)
  const bg = slot.color

  // Drag-over highlight
  if (dragOverSlotId.value === slot.id && state?.mode === 'AtExchange') {
    return {
      backgroundColor: '#218CD933',
      outline: '2px dashed #218CD9',
      outlineOffset: '-2px',
      cursor: 'copy',
    }
  }

  // Dragging: show dashed outline on AtExchange cells
  if (isDragging.value && state?.mode === 'AtExchange') {
    return {
      backgroundColor: bg,
      outline: '2px dashed #218CD9',
      outlineOffset: '-2px',
      cursor: 'copy',
    }
  }

  const outline = state ? `3px solid ${modeOutlineColor[state.mode]}` : `1px solid #aaa`
  return {
    backgroundColor: bg,
    outline,
    outlineOffset: state ? '-3px' : '-1px',
    cursor: props.readonly || slot.category === 'Thesis' ? 'default' : 'pointer',
  }
}

// Drag handlers
function onDragOver(event: DragEvent, slot: CourseSlotResponse) {
  const state = slotState(slot.id)
  if (state?.mode === 'AtExchange') {
    event.preventDefault()
  }
}

function onDragEnter(slot: CourseSlotResponse) {
  if (slotState(slot.id)?.mode === 'AtExchange') dragOverSlotId.value = slot.id
}

function onDragLeave() {
  dragOverSlotId.value = null
}

function onDrop(event: DragEvent, slot: CourseSlotResponse) {
  event.preventDefault()
  dragOverSlotId.value = null
  const state = slotState(slot.id)
  if (state?.mode !== 'AtExchange') return
  const course = exchangeStore.draggingCourse
  if (!course) return
  pendingDrop.value = { slot, course }
  exchangeStore.endDrag()
}

async function confirmDrop() {
  if (!pendingDrop.value) return
  await exchangeStore.addSlotMapping(props.exchangeId, {
    courseSlotId: pendingDrop.value.slot.id,
    foreignCourseId: pendingDrop.value.course.id,
    awardedEcts: pendingEcts.value,
  })
  pendingDrop.value = null
}

function cancelDrop() {
  pendingDrop.value = null
}

// Mode cycling
async function cycleMode(slot: CourseSlotResponse) {
  if (props.readonly || slot.category === 'Thesis') return
  const state = slotState(slot.id)
  if (!state) {
    await exchangeStore.setSlotMode(props.exchangeId, { courseSlotId: slot.id, mode: 'AtHome' })
  } else {
    const currentIndex = modes.indexOf(state.mode)
    if (currentIndex === modes.length - 1) {
      await exchangeStore.removeSlotState(props.exchangeId, slot.id)
    } else {
      const next = modes[currentIndex + 1]!
      await exchangeStore.setSlotMode(props.exchangeId, { courseSlotId: slot.id, mode: next })
    }
  }
}

async function removeMapping(slotMappingId: string) {
  await exchangeStore.removeSlotMapping(props.exchangeId, { slotMappingId })
}
</script>

<template>
  <div class="overflow-x-auto" style="font-family: Calibri, Arial, sans-serif;">
    <table style="border-collapse: collapse; width: 100%; min-width: 900px; table-layout: fixed;">
      <colgroup>
        <col style="width: 60px;" />
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
            style="border: 1px solid #aaa; vertical-align: top; padding: 8px 8px;"
            @click="cycleMode(slot)"
            @dragover="onDragOver($event, slot)"
            @dragenter="onDragEnter(slot)"
            @dragleave="onDragLeave()"
            @drop="onDrop($event, slot)"
          >
            <!-- Course code (if exists) or course name -->
            <div style="font-size: 13px; font-weight: 700; color: #000; line-height: 1.3;">
              {{ slot.courseCode ?? slot.courseName }}
            </div>
            <!-- Course name below code, or category if no code -->
            <div style="font-size: 11px; font-weight: 400; color: #222; line-height: 1.3; margin-top: 1px;">
              {{ slot.courseCode
                ? (locale === 'en' && slot.courseNameEn ? slot.courseNameEn : slot.courseName)
                : t(`courseSlotCategory.${slot.category}`) }}
            </div>

            <!-- Mode badge -->
            <div v-if="slotState(slot.id)" style="margin-top: 3px;">
              <span
                style="display: inline-block; font-size: 10px; padding: 1px 4px; border-radius: 2px; font-weight: 600;"
                :style="{
                  color: modeOutlineColor[slotState(slot.id)!.mode],
                  border: `1px solid ${modeOutlineColor[slotState(slot.id)!.mode]}`,
                  background: 'rgba(255,255,255,0.6)',
                }"
              >
                {{ t(`slotMode.${slotState(slot.id)!.mode}`) }}
              </span>
            </div>

            <!-- ECTS progress -->
            <div v-if="ectsLabel(slot)" style="margin-top: 3px;">
              <span
                style="display: inline-block; font-size: 10px; padding: 1px 4px; border-radius: 2px; font-weight: 700;"
                :style="{ color: ectsColor(slot), border: `1px solid ${ectsColor(slot)}`, background: 'rgba(255,255,255,0.6)' }"
              >
                {{ ectsLabel(slot) }} ECTS
              </span>
            </div>

            <!-- Mappings -->
            <div
              v-for="mapping in slotState(slot.id)?.mappings ?? []"
              :key="mapping.id"
              style="margin-top: 3px; display: flex; align-items: flex-start; justify-content: space-between; background: rgba(0,0,0,0.08); padding: 2px 4px; font-size: 11px;"
              @click.stop
            >
              <span style="color: #000; line-height: 1.3;">
                <span style="font-weight: 700;">{{ mapping.foreignCourseCode }}</span><br/>
                <span style="font-size: 10px; color: #5A8AAD;">{{ locale === 'hr' && mapping.foreignCourseNameEn }}</span><br/>
                <span style="color: #555;">{{ mapping.awardedEcts }} ECTS</span>
              </span>
              <button
                v-if="!readonly"
                type="button"
                style="color: #cc0000; font-size: 14px; line-height: 1; background: none; border: none; cursor: pointer; padding: 0; margin-left: 4px;"
                @click.stop="removeMapping(mapping.id)"
              >
                &times;
              </button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>

    <!-- Legend -->
    <div style="margin-top: 8px; display: flex; flex-wrap: wrap; gap: 12px; align-items: center;">
      <div v-for="mode in modes" :key="mode" style="display: flex; align-items: center; gap: 6px;">
        <span
          style="display: inline-block; width: 12px; height: 12px;"
          :style="{ background: modeOutlineColor[mode] }"
        />
        <span style="font-size: 11px; color: #8AC4ED;">{{ t(`slotMode.${mode}`) }}</span>
      </div>
      <span style="font-size: 11px; color: #5A8AAD; margin-left: 8px;">
        {{ t('table.clickToChange') }}
      </span>
    </div>

    <!-- ECTS input popup -->
    <div
      v-if="pendingDrop"
      style="position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 50;"
      @mousedown.self="cancelDrop"
    >
      <div style="background: #0A2235; border: 1px solid #1E4A6E; border-radius: 8px; padding: 24px; min-width: 320px;">
        <h3 style="color: #CAE4F7; font-size: 14px; font-weight: 600; margin-bottom: 16px;">
          {{ t('foreignCourses.addMapping') }}
        </h3>
        <div style="color: #8AC4ED; font-size: 12px; margin-bottom: 4px;">
          {{ pendingDrop.course.code }} — {{ pendingDrop.course.nameEn }}
        </div>
        <div style="color: #5A8AAD; font-size: 11px; margin-bottom: 16px;">
          {{ t('foreignCourses.availableEcts') }}: {{ pendingDrop.course.ects }} ECTS
        </div>
        <label style="display: block; color: #CAE4F7; font-size: 12px; margin-bottom: 6px;">
          {{ t('foreignCourses.awardedEcts') }}
        </label>
        <input
          v-model.number="pendingEcts"
          type="number"
          :min="0.5"
          :max="pendingDrop.course.ects"
          step="0.5"
          style="width: 100%; background: #071C2C; border: 1px solid #1E4A6E; color: #CAE4F7; padding: 8px; border-radius: 4px; font-size: 13px; margin-bottom: 16px;"
        />
        <div style="display: flex; gap: 8px; justify-content: flex-end;">
          <button
            type="button"
            style="padding: 8px 16px; border: 1px solid #1E4A6E; background: transparent; color: #8AC4ED; border-radius: 4px; cursor: pointer; font-size: 13px;"
            @click="cancelDrop"
          >
            {{ t('common.cancel') }}
          </button>
          <button
            type="button"
            style="padding: 8px 16px; background: #218CD9; border: none; color: white; border-radius: 4px; cursor: pointer; font-size: 13px; font-weight: 600;"
            @click="confirmDrop"
          >
            {{ t('common.confirm') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
