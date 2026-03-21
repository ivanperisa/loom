<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { exchangeService } from '@/services/exchange.service'

const router = useRouter()

onMounted(async () => {
  try {
    const res = await exchangeService.getMine()
    if (res.data.length === 1) {
      router.replace(`/exchange/${res.data[0].id}`)
    } else {
      router.replace('/home')
    }
  } catch {
    router.replace('/home')
  }
})
</script>

<template>
  <main class="flex min-h-screen items-center justify-center bg-[#071C2C]">
    <div class="animate-pulse text-[#5A8AAD]">Loading...</div>
  </main>
</template>
