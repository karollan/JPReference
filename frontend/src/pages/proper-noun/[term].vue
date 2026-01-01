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
            <v-row class="justify-space-between">
              <v-col class="main-term v-col-auto">
                <h1 class="display-term font-weight-bold text-h2 text-md-h1 mb-2">
                  <FuriganaText
                    :text="selectedKanjiText || selectedKanaText"
                    :reading="selectedKanjiText ? selectedKanaText : null"
                    :furigana="properNoun.furigana"
                  />
                </h1>
              </v-col>

              <v-col class="header-actions v-col-auto justify-space-between d-flex flex-column align-top gap-2">
                <div class="d-flex align-top mb-2">
                  <v-btn
                    color="primary"
                    prepend-icon="mdi-arrow-left"
                    variant="tonal"
                    @click="goBack"
                  >
                    Back to Search
                  </v-btn>
                </div>
              </v-col>
            </v-row>
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
                      <v-tooltip
                        v-for="(form, idx) in properNoun.kanjiForms"
                        :key="`kf-${idx}`"
                        :disabled="!form.tags?.length"
                        location="top"
                      >
                        <template #activator="{ props: tooltipProps }">
                          <v-chip
                            v-bind="tooltipProps"
                            class="mr-1 mb-1 font-jp kanji-chip"
                            :class="{ 'chip-with-tags': form.tags?.length > 0 }"
                            :color="selectedKanjiText === form.text ? 'primary' : undefined"
                            size="small"
                            :variant="selectedKanjiText === form.text ? 'flat' : 'outlined'"
                            @click.stop="selectKanji(form.text)"
                          >
                            {{ form.text }}
                          </v-chip>
                        </template>
                        <div class="tag-tooltip-content">
                          <div v-for="(tag, tIdx) in form.tags" :key="tIdx">
                            • {{ tag.description }}
                          </div>
                        </div>
                      </v-tooltip>
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
                      <v-tooltip
                        v-for="(form, idx) in properNoun.kanaForms"
                        :key="`kn-${idx}`"
                        :disabled="!form.tags?.length"
                        location="top"
                      >
                        <template #activator="{ props: tooltipProps }">
                          <v-chip
                            v-bind="tooltipProps"
                            class="mr-1 mb-1 font-jp"
                            :class="{
                              'chip-with-tags': form.tags?.length > 0,
                              'kana-chip-clickable': !hasKanjiForms
                            }"
                            :color="isKanaMatching(form.text) ? 'primary' : undefined"
                            size="small"
                            :variant="isKanaMatching(form.text) ? 'flat' : 'outlined'"
                            @click.stop="selectKana(form.text)"
                          >
                            {{ form.text }}
                          </v-chip>
                        </template>
                        <div class="tag-tooltip-content">
                          <div v-for="(tag, tIdx) in form.tags" :key="tIdx">
                            • {{ tag.description }}
                          </div>
                        </div>
                      </v-tooltip>
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

              <!-- Study Tools -->
              <section class="tools-section mb-6">
                <v-card class="pa-4 rounded-lg border-thin" variant="outlined">
                  <h3 class="text-overline font-weight-bold mb-2 text-medium-emphasis">Study Tools</h3>
                  <div class="d-flex flex-column gap-2">
                    <StrokePlayer
                      :text="selectedFormText ?? ''"
                      uri="/kanjivg/"
                    />
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
  import type { KanaForm, KanjiForm, TranslationDetails } from '@/types/ProperNoun'
  import { computed, onMounted, ref, watch } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import LanguageSelector from '@/components/search/LanguageSelector.vue'
  import { useProperNounStore } from '@/stores/proper-noun'
  import { DEFAULT_LANGUAGE, languageMatches } from '@/utils/language'

  const route = useRoute()
  const router = useRouter()
  const store = useProperNounStore()
  const selectedLanguage = ref<string>(DEFAULT_LANGUAGE)

  // Selection state - for kanji words we select kanji, for kana-only we select kana
  const selectedFormText = ref<string | null>(null)

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
    selectedFormText.value = null
    loadData()
  })

  const properNoun = computed(() => store.properNounDetails)

  // Determine if this is a kanji-based word or kana-only word
  const hasKanjiForms = computed(() => {
    return properNoun.value?.kanjiForms && properNoun.value.kanjiForms.length > 0
  })

  // Initialize selection when proper noun loads
  watch(() => properNoun.value, p => {
    if (!p) return

    // Check if current selection is valid for this newly loaded proper noun
    let isValidSelection = false
    if (selectedFormText.value) {
      if (p.kanjiForms?.length > 0) {
        isValidSelection = p.kanjiForms.some(k => k.text === selectedFormText.value)
      } else {
        isValidSelection = p.kanaForms?.some(k => k.text === selectedFormText.value) ?? false
      }
    }

    // If selection is missing or invalid for this term, set default
    if (!selectedFormText.value || !isValidSelection) {
      if (p.kanjiForms?.length > 0) {
        // Kanji exists - select first kanji form
        selectedFormText.value = p.kanjiForms[0]?.text ?? ''
      } else if (p.kanaForms?.length > 0) {
        // Kana-only - select first kana form (kana is the "form")
        selectedFormText.value = p.kanaForms[0]?.text ?? ''
      } else {
        selectedFormText.value = null
      }
    }
  }, { immediate: true })

  // For kanji words: get the selected kanji form
  const selectedKanjiForm = computed<KanjiForm | null>(() => {
    const p = properNoun.value
    if (!p || !hasKanjiForms.value || !selectedFormText.value) return null
    return p.kanjiForms?.find(k => k.text === selectedFormText.value) || null
  })

  // For kana-only words: get the selected kana form
  const selectedKanaFormOnly = computed<KanaForm | null>(() => {
    const p = properNoun.value
    if (!p || hasKanjiForms.value || !selectedFormText.value) return null
    return p.kanaForms?.find(k => k.text === selectedFormText.value) || null
  })

  // Get kana forms that match the selected kanji (for kanji words)
  const matchingKanaForms = computed<KanaForm[]>(() => {
    const p = properNoun.value
    if (!p) return []
    if (!hasKanjiForms.value) {
      // Kana-only: no "matching" concept, just return empty
      return []
    }
    if (!selectedFormText.value) {
      // No selection, match all
      return p.kanaForms || []
    }
    return p.kanaForms.filter(kana => {
      if (!kana.appliesToKanji || kana.appliesToKanji.length === 0) return true
      return kana.appliesToKanji.includes('*') || kana.appliesToKanji.includes(selectedFormText.value!)
    })
  })

  // Get the display text for the header (kanji text or kana text)
  const selectedKanjiText = computed(() => {
    if (hasKanjiForms.value) {
      return selectedFormText.value
    }
    return null // No kanji to display
  })

  // Get the kana reading to display (for kanji words: first matching reading, for kana-only: selected kana)
  const selectedKanaText = computed(() => {
    if (hasKanjiForms.value) {
      // Show first matching kana reading
      if (matchingKanaForms.value.length > 0) {
        return matchingKanaForms.value[0]?.text ?? ''
      }
      return ''
    }
    // Kana-only: the selected form IS the kana
    return selectedFormText.value || properNoun.value?.kanaForms?.[0]?.text || ''
  })

  // Determine if a kana form is selected/matching
  function isKanaMatching (kanaText: string): boolean {
    if (hasKanjiForms.value) {
      // Kanji word: check if this kana matches the selected kanji
      return matchingKanaForms.value.some(k => k.text === kanaText)
    }
    // Kana-only: check if this IS the selected form
    return kanaText === selectedFormText.value
  }

  // Click handler for kanji chips
  function selectKanji (kanjiText: string) {
    selectedFormText.value = kanjiText
  }

  // Click handler for kana chips (only for kana-only words)
  function selectKana (kanaText: string) {
    if (!hasKanjiForms.value) {
      selectedFormText.value = kanaText
    }
  }

  // Build tooltip content for tags
  function getTagTooltipContent (tags: { description: string }[]): string {
    return tags.map(t => `• ${t.description}`).join('\n')
  }

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

// Interactive chip styles
.kanji-chip {
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.15);
  }
}

.chip-with-tags {
  border-style: dashed !important;
}

// Clickable kana chips for kana-only words
.kana-chip-clickable {
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.15);
  }
}

.tag-tooltip-content {
  max-width: 300px;
  text-align: left;
  font-size: 0.85rem;
  line-height: 1.4;
}
</style>
