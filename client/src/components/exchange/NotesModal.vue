<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useConfirm } from '@/composables/useConfirm'

const props = defineProps<{
  laMessage: string | null
  recognitionMessage: string | null
  saving?: boolean
}>()

const emit = defineEmits<{
  save: [la: string | null, recognition: string | null]
  close: []
}>()

const { t } = useI18n()
const { confirm } = useConfirm()

const laDraft = ref(props.laMessage ?? '')
const recognitionDraft = ref(props.recognitionMessage ?? '')

watch(() => props.laMessage, (v) => { laDraft.value = v ?? '' })
watch(() => props.recognitionMessage, (v) => { recognitionDraft.value = v ?? '' })

const isDirty = computed(() =>
  (laDraft.value.trim() || null) !== (props.laMessage ?? null) ||
  (recognitionDraft.value.trim() || null) !== (props.recognitionMessage ?? null),
)

async function tryClose() {
  if (isDirty.value) {
    const ok = await confirm({ title: t('exchange.notesDiscardConfirm') })
    if (!ok) return
  }
  emit('close')
}

function save() {
  emit('save', laDraft.value.trim() || null, recognitionDraft.value.trim() || null)
}
</script>

<template>
  <Teleport to="body">
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm"
      @mousedown.self="tryClose"
    >
      <div class="w-full max-w-lg rounded-xl border border-primary/30 bg-dark p-6 shadow-xl">
        <div class="mb-5 flex items-center justify-between">
          <h2 class="text-base font-semibold text-light">{{ t('exchange.notes') }}</h2>
          <button
            type="button"
            class="text-light/40 transition hover:text-light"
            @click="tryClose"
          >
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
              <line x1="2" y1="2" x2="14" y2="14" /><line x1="14" y1="2" x2="2" y2="14" />
            </svg>
          </button>
        </div>

        <div class="space-y-4">
          <div>
            <label class="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-light/50">
              {{ t('exchange.tabs.learningAgreement') }}
            </label>
            <textarea
              v-model="laDraft"
              rows="4"
              class="w-full rounded-lg border border-primary/20 bg-dark-2 px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
              :placeholder="t('la.messagePlaceholder')"
            ></textarea>
          </div>

          <div>
            <label class="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-light/50">
              {{ t('exchange.tabs.recognition') }}
            </label>
            <textarea
              v-model="recognitionDraft"
              rows="4"
              class="w-full rounded-lg border border-primary/20 bg-dark-2 px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
              :placeholder="t('recognition.messagePlaceholder')"
            ></textarea>
          </div>
        </div>

        <div class="mt-5 flex justify-end gap-2">
          <button
            type="button"
            class="rounded-lg border border-primary/20 px-4 py-1.5 text-sm text-light/70 transition hover:bg-white/5 hover:text-light"
            @click="tryClose"
          >
            {{ t('common.cancel') }}
          </button>
          <button
            type="button"
            class="rounded-lg bg-primary px-4 py-1.5 text-sm font-medium text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-60"
            :disabled="saving || !isDirty"
            @click="save"
          >
            {{ saving ? t('common.loading') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
