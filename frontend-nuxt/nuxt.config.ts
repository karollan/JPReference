// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },

  modules: ['@pinia/nuxt', '@nuxt/fonts'],

  // Components auto-import configuration
  components: {
    dirs: [
      {
        path: '~/components',
        pathPrefix: false, // Disable path prefix so components use filename only
      }
    ]
  },

  // CSS
  css: [
    'vuetify/styles',
    '@mdi/font/css/materialdesignicons.css',
    '~/styles/settings.scss'
  ],

  // Font configuration
  fonts: {
    defaults: {
      weights: [300, 400, 500, 600, 700, 800],
      styles: ['normal', 'italic'],
      families: [
        { name: 'Noto Sans JP', provider: 'google' },
        { name: 'Roboto', provider: 'google' }
      ]
    }
  },

  // Build configuration
  build: {
    transpile: ['vuetify'],
  },

  // Runtime config for API URL
  runtimeConfig: {
    // Server-side: internal docker network
    apiUrl: process.env.API_URL || 'http://backend:5000/api',
    public: {
      // Client-side: relative path, nginx proxies to backend
      apiUrl: '/api',
      adsenseClientId: process.env.NUXT_PUBLIC_ADSENSE_CLIENT_ID || '',
      adsenseSlotId: process.env.NUXT_PUBLIC_ADSENSE_SLOT_ID || ''
    }
  },

  // App directory structure
  srcDir: 'app/',

  // Server-side rendering
  ssr: true,

  // Pinia configuration
  pinia: {
    storesDirs: ['app/stores/**']
  },

  // TypeScript
  typescript: {
    strict: true
  },

  // Vite configuration
  vite: {
    ssr: {
      noExternal: ['vuetify'],
    }
  },
})