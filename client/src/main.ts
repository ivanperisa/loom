import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { VueQueryPlugin } from '@tanstack/vue-query'
import './assets/main.css'

import App from './App.vue'
import { i18n } from './i18n/index'
import router from './router'
import { queryClient } from './queries/queryClient'
import { startErrorTracking } from './errorTracking'

const app = createApp(App)

app.use(createPinia())
app.use(VueQueryPlugin, { queryClient })
app.use(i18n)
app.use(router)

void startErrorTracking(app, router)
app.mount('#app')
