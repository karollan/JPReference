<template>
  <v-container
    class="proper-noun-details-container fill-height align-start"
    fluid
  >
    <v-row
      class="w-100"
      justify="center"
    >
      <v-col
        cols="12"
        lg="10"
        xl="8"
      >
        <!-- Loading State -->
        <div
          v-if="store.loading"
          class="d-flex justify-center align-center py-12"
        >
          <v-progress-circular
            color="primary"
            indeterminate
            size="64"
          />
        </div>

        <!-- Error State -->
        <v-alert
          v-else-if="store.error || !properNoun"
          class="mb-4"
          type="error"
          variant="tonal"
        >
          {{ store.error || 'Proper noun not found' }}
          <template #append>
            <v-btn
              variant="text"
              @click="loadData"
            >
              Retry
            </v-btn>
          </template>
        </v-alert>

        <!-- Content -->
        <div
          v-else
          class="proper-noun-content animate-fade-in"
        >
          <!-- Header Section -->
          <header class="vocab-header mb-6">
            <div class="d-flex flex-column flex-md-row align-md-end justify-space-between">
              <div class="main-term">
                <h1 class="display-term font-weight-bold text-h2 text-md-h1 mb-2">
                  <ruby v-if="primaryKanji">
                    {{ primaryKanji }}
                    <rt class="text-h5 font-weight-medium text-primary">{{ primaryKana }}</rt>
                  </ruby>
                  <span v-else>{{ primaryKana }}</span>
                </h1>
              </div>

              <div class="header-actions d-flex flex-column align-end gap-2">
                <div class="d-flex align-center mb-2">
                  <v-btn
                    color="primary"
                    prepend-icon="mdi-arrow-left"
                    variant="tonal"
                    @click="goBack"
                  >
                    Back to Search
                  </v-btn>
                </div>
              </div>
            </div>
            <v-divider class="mt-4 mb-6 border-opacity-25" />
          </header>

          <v-row>
            <!-- Main Content: Translations -->
            <v-col
              cols="12"
              md="8"
            >
              <section class="translations-section mb-8">
                <div class="d-flex align-center justify-space-between mb-4">
                  <h2 class="text-h5 font-weight-bold d-flex align-center">
                    <v-icon
                      class="mr-2"
                      color="primary"
                      icon="mdi-translate"
                      start
                    />
                    Translations
                  </h2>
                  <LanguageSelector
                    v-if="availableLanguages.length > 0"
                    :available-languages="availableLanguages"
                    :default-language="selectedLanguage"
                    @language-changed="onLanguageChanged"
                  />
                </div>

                <v-card
                  class="translations-card bg-surface"
                  flat
                >
                  <div
                    v-if="filteredTranslations.length === 0"
                    class="pa-4 text-medium-emphasis font-italic"
                  >
                    No translations available for the selected language.
                  </div>

                  <div
                    v-for="(translation, index) in filteredTranslations"
                    :key="index"
                    class="translation-block py-4 px-4"
                    :class="{ 'border-bottom': index < filteredTranslations.length - 1 }"
                  >
                    <div class="d-flex">
                      <div class="translation-number text-h6 text-medium-emphasis font-weight-light mr-4">
                        {{ index + 1 }}.
                      </div>
                      <div class="translation-body flex-grow-1">
                        <!-- Type Tags -->
                        <div class="translation-meta mb-1">
                          <span
                            v-for="(type, tIdx) in translation.types"
                            :key="`type-${tIdx}`"
                            class="text-caption text-medium-emphasis mr-2 font-italic"
                          >
                            {{ type.description }}
                          </span>
                        </div>

                        <!-- Translation Text -->
                        <div class="translation-text text-body-1 font-weight-medium mb-2">
                          {{ getTranslationText(translation) }}
                        </div>
                      </div>
                    </div>
                  </div>
                </v-card>
              </section>
            </v-col>

            <!-- Sidebar: Forms, Kanji, Metadata -->
            <v-col
              cols="12"
              md="4"
            >
              <!-- Other Forms -->
              <section class="forms-section mb-6">
                <v-card
                  class="pa-4 rounded-lg border-thin"
                  variant="outlined"
                >
                  <h3 class="text-overline font-weight-bold mb-2 text-medium-emphasis">
                    Forms
                  </h3>

                  <div class="forms-group mb-4">
                    <div class="text-caption font-weight-bold mb-1">
                      Kanji Forms
                    </div>
                    <div class="d-flex flex-wrap gap-2">
                      <v-chip
                        v-for="(form, idx) in properNoun.kanjiForms"
                        :key="`kf-${idx}`"
                        class="mr-1 mb-1 font-jp"
                        size="small"
                        :variant="form.text === primaryKanji ? 'flat' : 'outlined'"
                      >
                        {{ form.text }}
                      </v-chip>
                      <span
                        v-if="!properNoun.kanjiForms?.length"
                        class="text-caption text-disabled"
                      >None</span>
                    </div>
                  </div>

                  <div class="forms-group">
                    <div class="text-caption font-weight-bold mb-1">
                      Kana Forms
                    </div>
                    <div class="d-flex flex-wrap gap-2">
                      <v-chip
                        v-for="(form, idx) in properNoun.kanaForms"
                        :key="`kn-${idx}`"
                        class="mr-1 mb-1 font-jp"
                        size="small"
                        :variant="form.text === primaryKana ? 'flat' : 'outlined'"
                      >
                        {{ form.text }}
                      </v-chip>
                    </div>
                  </div>
                </v-card>
              </section>

              <!-- Contained Kanji -->
              <section
                v-if="properNoun.containedKanji && properNoun.containedKanji.length > 0"
                class="kanji-section mb-6"
              >
                <v-card
                  class="pa-4 rounded-lg border-thin"
                  variant="outlined"
                >
                  <h3 class="text-overline font-weight-bold mb-2 text-medium-emphasis">
                    Contained Kanji
                  </h3>
                  <div class="d-flex flex-wrap gap-2">
                    <v-btn
                      v-for="kanji in properNoun.containedKanji"
                      :key="kanji.id"
                      class="kanji-link-btn font-jp text-h6 px-3"
                      color="primary"
                      height="48"
                      :to="`/kanji/${kanji.literal}`"
                      variant="tonal"
                    >
                      {{ kanji.literal }}
                    </v-btn>
                  </div>
                </v-card>
              </section>

              <!-- Metadata/Ids -->
              <section class="meta-section">
                <div class="text-caption text-disabled font-mono">
                  ID: {{ properNoun.id }}<br>
                  JMnedict ID: {{ properNoun.jmnedictId }}
                </div>
              </section>
            </v-col>
          </v-row>
        </div>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
  import type { TranslationDetails } from '@/types/ProperNoun'
  import { computed, onMounted, ref, watch } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import LanguageSelector from '@/components/search/LanguageSelector.vue'
  import { useProperNounStore } from '@/stores/proper-noun'
  import { DEFAULT_LANGUAGE, languageMatches } from '@/utils/language'

  const route = useRoute()
  const router = useRouter()
  const store = useProperNounStore()
  const selectedLanguage = ref<string>(DEFAULT_LANGUAGE)

  const term = computed(() => (route.params as any).term as string)

  // Fetch data
  async function loadData () {
    if (term.value) {
      await store.getProperNounDetails(term.value)
    }
  }

  onMounted(() => {
    loadData()
  })

  watch(() => term.value, () => {
    loadData()
  })

  const properNoun = computed(() => store.properNounDetails)

  // Primary Display Logic
  const primaryKanji = computed(() => {
    const p = properNoun.value
    if (!p) return ''
    if (p.kanjiForms && p.kanjiForms.length > 0) {
      return p.kanjiForms[0]?.text ?? ''
    }
    return ''
  })

  const primaryKana = computed(() => {
    const p = properNoun.value
    if (!p) return ''
    // Find kana that applies to primary kanji
    if (primaryKanji.value) {
      const match = p.kanaForms.find(k =>
        !k.appliesToKanji
        || k.appliesToKanji.length === 0
        || k.appliesToKanji.includes('*')
        || k.appliesToKanji.includes(primaryKanji.value as string),
      )
      if (match) return match.text
    }
    // Fallback to first kana
    if (p.kanaForms && p.kanaForms.length > 0) {
      return p.kanaForms[0]?.text ?? ''
    }
    return ''
  })

  // Language Handling
  function getLanguagesFromTranslation (translation: TranslationDetails, languages: Set<string>) {
    if (translation.text) {
      for (const t of translation.text) {
        if (t.language) languages.add(t.language)
      }
    }
  }

  const availableLanguages = computed(() => {
    const p = properNoun.value
    if (!p || !p.translations) return []

    const languages = new Set<string>()
    for (const translation of p.translations) {
      getLanguagesFromTranslation(translation, languages)
    }
    return Array.from(languages)
  })

  function onLanguageChanged (lang: string) {
    selectedLanguage.value = lang
  }

  const filteredTranslations = computed(() => {
    const p = properNoun.value
    if (!p || !p.translations) return []

    return p.translations.map(translation => {
      const hasText = translation.text?.some(t => languageMatches(t.language, selectedLanguage.value))
      if (!hasText) return null

      return {
        ...translation,
        text: translation.text.filter(t => languageMatches(t.language, selectedLanguage.value)),
      }
    }).filter((t): t is TranslationDetails => t !== null)
  })

  function getTranslationText (translation: TranslationDetails) {
    return translation.text.map(t => t.text).join('; ')
  }

  function goBack () {
    router.back()
  }
</script>

<style lang="scss" scoped>
.font-jp {
  font-family: 'Noto Sans JP', sans-serif;
}

.proper-noun-details-container {
  background-color: rgb(var(--v-theme-background));
}

.vocab-header {
  .display-term {
    line-height: 1.2;
    ruby {
      rt {
        line-height: 1.5;
      }
    }
  }
}

.translation-block {
  &.border-bottom {
    border-bottom: 1px solid rgba(var(--v-border-color), 0.1);
  }
}

.gap-2 {
  gap: 0.5rem;
}

.animate-fade-in {
  animation: fadeIn 0.4s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
