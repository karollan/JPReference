// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },

  modules: ['@pinia/nuxt'],

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
    '~/styles/settings.scss',
    '~/styles/fonts.css'
  ],

  // Build configuration
  build: {
    transpile: ['vuetify'],
  },

  // Runtime config for API URL
  runtimeConfig: {
    apiUrl: process.env.API_URL || 'http://backend:5000/api',
    public: {
      apiUrl: process.env.NUXT_PUBLIC_API_URL || 'http://localhost:5000/api'
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