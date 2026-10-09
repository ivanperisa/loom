import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { VueQueryPlugin } from '@tanstack/vue-query'
import 'flag-icons/css/flag-icons.min.css'
import './assets/main.css'

import App from './App.vue'
import { i18n } from './i18n/index'
import router from './router'
import { queryClient } from './queries/queryClient'

const app = createApp(App)

app.use(createPinia())
app.use(VueQueryPlugin, { queryClient })
app.use(i18n)
app.use(router)

app.mount('#app')
