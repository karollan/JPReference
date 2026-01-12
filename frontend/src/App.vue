<template>
  <v-app>
    <router-view />
  </v-app>
</template>

<script lang="ts" setup>
  import { useTheme } from 'vuetify'

  const theme = useTheme()

  onMounted(() => {
    const savedTheme = localStorage.getItem('jlpt-theme')
    if (savedTheme) {
      theme.change(savedTheme)
    } else {
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
      theme.change(prefersDark ? 'jlptThemeDark' : 'jlptTheme')
    }

    // Load AdSense
    const adsenseClientId = import.meta.env.VITE_ADSENSE_CLIENT_ID
    if (adsenseClientId && !document.getElementById('adsense-script')) {
      const script = document.createElement('script')
      script.id = 'adsense-script'
      script.src = `https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=${adsenseClientId}`
      script.async = true
      script.crossOrigin = 'anonymous'
      document.head.appendChild(script)
    }
  })

  watch(() => theme.global.name.value, (newTheme) => {
    localStorage.setItem('jlpt-theme', newTheme)
  })
</script>
