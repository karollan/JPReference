/**
 * plugins/index.ts
 *
 * Automatically included in `./src/main.ts`
 */

// Types
import type { App } from 'vue'
import router from '../router'
import pinia from '../stores'

// Plugins
import vuetify from './vuetify'
import { createHead } from '@unhead/vue/client'

export function registerPlugins(app: App) {
  const head = createHead()
  app
    .use(head)
    .use(vuetify)
    .use(router)
    .use(pinia)
}
