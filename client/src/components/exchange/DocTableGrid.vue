<script setup lang="ts">
import { useI18n } from 'vue-i18n'

withDefaults(
  defineProps<{
    totalCols?: number
    minWidth?: number
    firstColWidth?: number
  }>(),
  {
    totalCols: 30,
    minWidth: 900,
    firstColWidth: 50,
  },
)

const { t } = useI18n()
</script>

<template>
  <div class="overflow-x-auto doc-table-wrap">
    <table :style="{ borderCollapse: 'collapse', width: '100%', minWidth: `${minWidth}px`, tableLayout: 'fixed' }">
      <colgroup>
        <col :style="{ width: `${firstColWidth}px` }" />
        <col v-for="c in totalCols" :key="c" />
      </colgroup>
      <thead>
        <tr>
          <th class="doc-head">{{ t('table.semester') }}</th>
          <th v-for="col in totalCols" :key="col" class="doc-head doc-head--num">{{ col }}</th>
        </tr>
      </thead>
      <tbody>
        <slot />
      </tbody>
    </table>

    <div class="doc-legend">
      <slot name="legend" />
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

.doc-head {
  border: 1px solid #aaa;
  background: #d9d9d9;
  font-size: 10px;
  padding: 4px;
  text-align: center;
  color: #000;
}

.doc-head--num {
  font-weight: normal;
  padding: 4px 0;
}
</style>
