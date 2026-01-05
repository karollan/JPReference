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
      theme.global.name.value = savedTheme
    } else {
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
      theme.global.name.value = prefersDark ? 'jlptThemeDark' : 'jlptTheme'
    }
  })

  watch(() => theme.global.name.value, (newTheme) => {
    localStorage.setItem('jlpt-theme', newTheme)
  })
</script>
