<script setup lang="ts">
import { useRoute } from 'vue-router'
import AppHeader from '@/components/AppHeader.vue'
import ExchangeDetailPanel from '@/components/exchange/ExchangeDetailPanel.vue'

const route = useRoute()

const exchangeId = route.params.exchangeId as string
</script>

<template>
  <AppHeader guest />
  <main class="min-h-screen bg-dark">
    <section class="page-container page-container--wide">
      <ExchangeDetailPanel :exchange-id="exchangeId" guest />
    </section>
  </main>
</template>
