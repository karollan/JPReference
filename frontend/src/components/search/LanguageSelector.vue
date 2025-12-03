<template>
  <div v-if="displayedLanguages.length > 0" class="language-selector">
    <v-btn
      v-for="lang in displayedLanguages"
      :key="lang"
      v-ripple
      :aria-label="getLanguageName(lang)"
      class="language-btn"
      :color="selectedLanguage === lang ? 'primary' : 'default'"
      density="compact"
      size="x-small"
      :variant="selectedLanguage === lang ? 'tonal' : 'text'"
      @click.stop="selectLanguage(lang)"
    >
      {{ getLanguageFlag(lang) }}
    </v-btn>
  </div>
</template>

<script lang="ts" setup>
  import { computed, ref, watch } from 'vue'
  import { DEFAULT_LANGUAGE, getLanguageFlag, getLanguageName, languageMatches } from '@/utils/language'

  const props = defineProps<{
    availableLanguages: string[]
    defaultLanguage?: string
  }>()

  const emit = defineEmits<{
    'language-changed': [language: string]
  }>()

  const selectedLanguage = ref<string>(props.defaultLanguage || DEFAULT_LANGUAGE)

  const displayedLanguages = computed(() => {
    const seen = new Set<string>()
    return (props.availableLanguages || []).filter(lang => {
      if (!lang) {
        return false
      }
      const normalized = lang.toLowerCase()
      if (seen.has(normalized)) {
        return false
      }
      seen.add(normalized)
      return true
    })
  })

  function emitSelection (language: string) {
    selectedLanguage.value = language
    emit('language-changed', language)
  }

  function synchronizeLanguage (languages: string[]) {
    if (languages.length === 0) {
      return
    }

    const exactMatch = languages.find(lang => languageMatches(lang, selectedLanguage.value))
    if (exactMatch) {
      if (selectedLanguage.value !== exactMatch) {
        emitSelection(exactMatch)
      }
      return
    }

    const fallback = languages.find(lang => languageMatches(lang, DEFAULT_LANGUAGE)) || languages[0]
    if (fallback) {
      emitSelection(fallback)
    }
  }

  watch(() => props.availableLanguages, newLangs => {
    synchronizeLanguage(newLangs || [])
  }, { immediate: true })

  watch(() => props.defaultLanguage, newDefault => {
    if (!newDefault) {
      return
    }
    selectedLanguage.value = newDefault
    synchronizeLanguage(props.availableLanguages || [])
  })

  function selectLanguage (lang: string) {
    emitSelection(lang)
  }
</script>

<style lang="scss" scoped>
.language-selector {
    display: flex;
    gap: 2px;
    align-items: center;
}

.language-btn {
    min-width: 32px !important;
    padding: 0 4px !important;
    font-size: 1.2rem;
}
</style>
