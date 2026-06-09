<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import ExchangeDetailPanel from '@/components/exchange/ExchangeDetailPanel.vue'

const route = useRoute()
const exchangeId = computed(() => route.params.exchangeId as string)
</script>

<template>
  <main class="min-h-screen bg-dark">
    <section class="page-container">
      <ExchangeDetailPanel :key="exchangeId" :exchange-id="exchangeId" />
    </section>
  </main>
</template>
