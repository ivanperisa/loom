<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { exchangeService } from '@/services/exchange.service'

const router = useRouter()

onMounted(async () => {
  try {
    const res = await exchangeService.getMine()
    const firstEx = res.data[0]
    if (res.data.length === 1 && firstEx) {
      router.replace(`/exchange/${firstEx.id}`)
    } else {
      router.replace('/home')
    }
  } catch {
    router.replace('/home')
  }
})
</script>

<template>
  <main class="flex min-h-screen items-center justify-center bg-dark">
    <div class="animate-pulse text-light/60">Loading...</div>
  </main>
</template>
