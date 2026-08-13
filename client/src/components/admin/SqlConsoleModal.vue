<script setup lang="ts">
import { ref } from 'vue'
import { adminService, type SqlExecutionResult } from '@/services/admin.service'
import { extractApiError } from '@/utils/apiError'

const emit = defineEmits<{
  close: []
}>()

const sql = ref('')
const running = ref(false)
const error = ref<string | null>(null)
const result = ref<SqlExecutionResult | null>(null)

const columns = () => {
  const firstRow = result.value?.rows?.[0]
  return firstRow ? Object.keys(firstRow) : []
}

async function run() {
  if (!sql.value.trim() || running.value) return
  running.value = true
  error.value = null
  result.value = null
  try {
    const res = await adminService.executeSql(sql.value)
    result.value = res.data
  } catch (e) {
    const { title, message } = extractApiError(e)
    error.value = message ?? title
  } finally {
    running.value = false
  }
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
    e.preventDefault()
    run()
  }
}
</script>

<template>
  <Teleport to="body">
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 px-4"
      @mousedown.self="emit('close')"
    >
      <div class="flex max-h-[85vh] w-full max-w-3xl flex-col rounded-2xl border border-red-500/30 bg-dark-2 shadow-2xl">
        <div class="flex items-center justify-between border-b border-red-500/30 px-6 py-4">
          <h3 class="font-semibold text-light">SQL Console</h3>
          <button type="button" class="text-light/40 transition hover:text-white" @click="emit('close')">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>

        <div class="flex-1 space-y-3 overflow-y-auto px-6 py-5">
          <textarea
            v-model="sql"
            rows="6"
            spellcheck="false"
            placeholder="SELECT * FROM ..."
            class="w-full resize-y rounded-lg border border-primary/20 bg-dark px-3 py-2 font-mono text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
            @keydown="onKeydown"
          />

          <p v-if="error" class="text-xs text-red-400">{{ error }}</p>

          <div v-if="result">
            <p class="mb-2 text-xs text-light/40">
              {{ result.rows ? `${result.rows.length} row(s)` : `${result.rowsAffected} row(s) affected` }}
            </p>
            <div v-if="result.rows?.length" class="overflow-auto rounded-lg border border-primary/20">
              <table class="w-full text-left text-xs">
                <thead class="bg-dark text-light/60">
                  <tr>
                    <th v-for="col in columns()" :key="col" class="whitespace-nowrap px-3 py-2">{{ col }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(row, i) in result.rows" :key="i" class="border-t border-primary/10 text-light/80">
                    <td v-for="col in columns()" :key="col" class="whitespace-nowrap px-3 py-1.5">{{ row[col] ?? 'NULL' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <div class="flex justify-end gap-2 border-t border-red-500/30 px-6 py-4">
          <button
            type="button"
            class="rounded-lg border border-white/10 px-4 py-2 text-sm text-light/60 transition hover:text-light"
            @click="emit('close')"
          >Close</button>
          <button
            type="button"
            class="rounded-lg bg-red-500 px-5 py-2 text-sm font-semibold text-white transition hover:bg-red-400 disabled:opacity-50"
            :disabled="running || !sql.trim()"
            @click="run"
          >{{ running ? 'Running…' : 'Run' }}</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
