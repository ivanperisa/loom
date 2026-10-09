<script setup lang="ts">
// Local development only (VITE_DEV_LOGIN=true): log in as any seeded persona without Google.
// The API only exposes /api/auth/dev/* when DevAuth is enabled in the Development environment.
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth.store'

interface DevUser {
  email: string
  name: string
  role: string
  isOnboarded: boolean
}

const router = useRouter()
const authStore = useAuthStore()

const users = ref<DevUser[]>([])
const filter = ref('')
const customEmail = ref('')
const busy = ref(false)
const error = ref<string | null>(null)

const visible = computed(() => {
  const term = filter.value.trim().toLowerCase()
  return term
    ? users.value.filter((u) => u.email.includes(term) || u.name.toLowerCase().includes(term))
    : users.value
})

onMounted(async () => {
  try {
    const res = await api.get<DevUser[]>('/api/auth/dev/users', { errorToast: false })
    users.value = res.data
  } catch {
    error.value = 'Dev login is not available (is DevAuth enabled on the API?).'
  }
})

async function loginAs(email: string) {
  if (!email.trim()) return
  busy.value = true
  error.value = null
  try {
    await api.post('/api/auth/dev/login', { email }, { errorToast: false })
    await authStore.init(true)
    await router.push('/home')
  } catch {
    error.value = `Could not log in as ${email}.`
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <section class="mt-6 rounded-xl border border-dashed border-warning-text/50 p-4">
    <p class="text-xs font-semibold uppercase tracking-wide text-warning-text">Dev login</p>
    <input
      v-model="filter"
      type="search"
      placeholder="Filter personas…"
      class="mt-2 w-full rounded-lg border border-primary/30 bg-dark px-3 py-1.5 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
    />
    <ul class="mt-2 max-h-64 space-y-1 overflow-y-auto pr-1">
      <li v-for="user in visible" :key="user.email">
        <button
          type="button"
          :disabled="busy"
          class="flex w-full items-center justify-between gap-2 rounded-lg px-2 py-1.5 text-left text-sm text-light transition hover:bg-primary/10 disabled:opacity-50"
          @click="loginAs(user.email)"
        >
          <span class="min-w-0 truncate">
            {{ user.email }}
            <span class="text-light/50">· {{ user.name }}</span>
          </span>
          <span class="shrink-0 text-xs text-light/60">
            {{ user.role }}<template v-if="!user.isOnboarded"> · new</template>
          </span>
        </button>
      </li>
    </ul>
    <form class="mt-2 flex gap-2" @submit.prevent="loginAs(customEmail)">
      <input
        v-model="customEmail"
        type="email"
        placeholder="or any email (first login)"
        class="min-w-0 flex-1 rounded-lg border border-primary/30 bg-dark px-3 py-1.5 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
      />
      <button
        type="submit"
        :disabled="busy"
        class="shrink-0 rounded-lg bg-primary-strong px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-50"
      >
        Log in
      </button>
    </form>
    <p v-if="error" class="mt-2 text-xs text-danger-text">{{ error }}</p>
  </section>
</template>
