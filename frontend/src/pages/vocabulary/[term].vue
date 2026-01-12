<template>
  <v-container
    class="vocabulary-details-container fill-height align-start"
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
          v-else-if="store.error || !vocabulary"
          class="mb-4"
          type="error"
          variant="tonal"
        >
          {{ store.error || 'Vocabulary not found' }}
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
          class="vocabulary-content animate-fade-in"
        >
          <!-- Header Section -->
          <header class="vocab-header mb-6">
            <v-row class="justify-space-between">
              <v-col class="main-term v-col-auto">
                <h1 class="display-term font-weight-bold text-h2 text-md-h1 mb-2">
                  <FuriganaText
                    :text="selectedKanjiText || selectedKanaText"
                    :reading="selectedKanjiText ? selectedKanaText : null"
                    :furigana="vocabulary.furigana"
                  />
                </h1>
              </v-col>

              <v-col class="header-actions v-col-auto d-flex justify-space-between flex-column">
                <div class="d-flex justify-end">
                  <v-btn
                    color="primary"
                    prepend-icon="mdi-arrow-left"
                    variant="tonal"
                    @click="goBack"
                  >
                    Go back
                  </v-btn>
                </div>
                <div class="badges d-flex gap-2 justify-end">
                  <v-chip
                    v-if="selectedIsCommon"
                    color="success"
                    label
                    variant="flat"
                  >
                    Common
                  </v-chip>
                  <v-chip
                    v-if="vocabulary.jlptLevel"
                    color="primary"
                    label
                    variant="flat"
                  >
                    N{{ vocabulary.jlptLevel }}
                  </v-chip>
                </div>
              </v-col>
            </v-row>
            <v-divider class="mt-4 mb-6 border-opacity-25" />
          </header>

          <v-row>
            <!-- Main Content: Senses & Examples -->
            <v-col
              cols="12"
              md="8"
            >
              <section class="meanings-section mb-8">
                <div class="d-flex align-center justify-space-between mb-4">
                  <h2 class="text-h5 font-weight-bold d-flex align-center">
                    <v-icon
                      class="mr-2"
                      color="primary"
                      icon="mdi-book-open-variant-outline"
                      start
                    />
                    Meanings
                  </h2>
                  <LanguageSelector
                    v-if="availableLanguages.length > 0"
                    :available-languages="availableLanguages"
                    :default-language="selectedLanguage"
                    @language-changed="onLanguageChanged"
                  />
                </div>

                <v-card
                  class="meanings-card bg-surface"
                  flat
                >
                  <div
                    v-if="filteredSenses.length === 0"
                    class="pa-4 text-medium-emphasis font-italic"
                  >
                    No definitions available for the selected language.
                  </div>

                  <div
                    v-for="(sense, index) in filteredSenses"
                    :key="index"
                    class="sense-block py-4 px-4"
                    :class="{ 'border-bottom': index < filteredSenses.length - 1 }"
                  >
                    <div class="d-flex">
                      <div class="sense-number text-h6 text-medium-emphasis font-weight-light mr-4">
                        {{ index + 1 }}.
                      </div>
                      <div class="sense-body flex-grow-1">
                        <!-- POS and Tags -->
                        <div class="sense-meta mb-1">
                          <span
                            v-for="(tag, tIdx) in sense.tags"
                            :key="`tag-${tIdx}`"
                            class="text-caption text-medium-emphasis mr-2 font-italic"
                          >
                            {{ tag.description }}
                          </span>
                        </div>

                        <!-- Glosses -->
                        <div class="glosses text-body-1 font-weight-medium mb-2">
                          {{ getGlossText(sense) }}
                        </div>

                        <!-- Info -->
                        <div
                          v-if="sense.info && sense.info.length > 0"
                          class="info-box mt-1 mb-2"
                        >
                          <div
                            v-for="(info, iIdx) in sense.info"
                            :key="`info-${iIdx}`"
                            class="text-caption text-grey-darken-1"
                          >
                            <v-icon
                              icon="mdi-information-outline"
                              size="small"
                              start
                            />
                            {{ info }}
                          </div>
                        </div>

                        <!-- Examples -->
                        <div
                          v-if="sense.examples && sense.examples.length > 0"
                          class="examples-section mt-3 pl-3 border-left-primary"
                        >
                          <div
                            v-for="(example, eIdx) in sense.examples"
                            :key="`ex-${eIdx}`"
                            class="example-item mb-2"
                          >
                            <div class="jp-sentence text-body-2 mb-1">
                              {{ getExampleSentence(example, 'jpn') }}
                            </div>
                            <div class="target-sentence text-caption text-medium-emphasis">
                              {{ getExampleSentence(example, selectedLanguage) || getExampleSentence(example, 'eng') }}
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </v-card>
              </section>
            </v-col>

            <!-- Sidebar: Forms, Kanji, Relations -->
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
                        v-for="(form, idx) in vocabulary.kanjiForms"
                        :key="`kf-${idx}`"
                        :disabled="!form.tags?.length && !form.isCommon"
                        location="top"
                        :open-on-click="isMobile"
                        :open-on-hover="!isMobile"
                        :persistent="false"
                      >
                        <template #activator="{ props: tooltipProps }">
                          <v-chip
                            v-bind="tooltipProps"
                            class="mr-1 mb-1 font-jp kanji-chip"
                            :class="{ 'chip-with-tags': form.tags?.length > 0 }"
                            :color="form.text === selectedKanjiText ? 'primary' : (form.isCommon ? 'secondary' : undefined)"
                            size="small"
                            :variant="form.text === selectedKanjiText ? 'flat' : 'outlined'"
                            @click.stop="selectKanji(form.text)"
                          >
                            {{ form.text }}
                            <template
                              v-if="form.isCommon"
                              #append
                            >
                              <v-icon
                                class="ml-1"
                                icon="mdi-check-circle"
                                size="x-small"
                              />
                            </template>
                          </v-chip>
                        </template>
                        <div class="tag-tooltip-content">
                          <div v-if="form.isCommon" class="mb-1">
                            <v-icon icon="mdi-check-circle" size="x-small" class="mr-1" />
                            Common word
                          </div>
                          <div v-for="(tag, tIdx) in form.tags" :key="tIdx">
                            • {{ tag.description }}
                          </div>
                        </div>
                      </v-tooltip>
                      <span
                        v-if="!vocabulary.kanjiForms?.length"
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
                        v-for="(form, idx) in vocabulary.kanaForms"
                        :key="`kn-${idx}`"
                        :disabled="!form.tags?.length && !form.isCommon"
                        location="top"
                        :open-on-click="isMobile"
                        :open-on-hover="!isMobile"
                        :persistent="false"
                      >
                        <template #activator="{ props: tooltipProps }">
                          <v-chip
                            v-bind="tooltipProps"
                            class="mr-1 mb-1 font-jp"
                            :class="{
                              'chip-with-tags': form.tags?.length > 0,
                              'kana-chip-clickable': !hasKanjiForms
                            }"
                            :color="isKanaMatching(form.text) ? 'primary' : (form.isCommon ? 'secondary' : undefined)"
                            size="small"
                            :variant="isKanaMatching(form.text) ? 'flat' : 'outlined'"
                            @click.stop="selectKana(form.text)"
                          >
                            {{ form.text }}
                            <template
                              v-if="form.isCommon"
                              #append
                            >
                              <v-icon
                                class="ml-1"
                                icon="mdi-check-circle"
                                size="x-small"
                              />
                            </template>
                          </v-chip>
                        </template>
                        <div class="tag-tooltip-content">
                          <div v-if="form.isCommon" class="mb-1">
                            <v-icon icon="mdi-check-circle" size="x-small" class="mr-1" />
                            Common word
                          </div>
                          <div v-for="(tag, tIdx) in form.tags" :key="tIdx">
                            • {{ tag.description }}
                          </div>
                        </div>
                      </v-tooltip>
                    </div>
                  </div>
                </v-card>
              </section>

              <AdSenseWidget />

              <!-- Contained Kanji -->
              <section
                v-if="vocabulary.containedKanji && vocabulary.containedKanji.length > 0"
                class="kanji-section mb-6"
              >
                <v-card
                  class="pa-4 rounded-lg border-thin"
                  variant="outlined"
                >
                  <h3 class="text-overline font-weight-bold mb-2 text-medium-emphasis">
                    Related Kanji
                  </h3>
                  <div class="d-flex flex-wrap gap-2">
                    <v-btn
                      v-for="kanji in vocabulary.containedKanji"
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
                  <div class="d-flex flex-column align-center gap-2">
                    <StrokePlayer
                      :text="selectedFormText ?? ''"
                      uri="/kanjivg/"
                    />
                    <v-btn
                      color="primary"
                      class="text-none mt-2"
                      width="300px"
                      prepend-icon="mdi-volume-high"
                      variant="outlined"
                      @click="playPronunciation(selectedKanaText)"
                    >
                      Play pronunciation
                    </v-btn>
                  </div>
                </v-card>
                
              </section>

              <!-- Metadata/Ids -->
              <section class="meta-section">
                <div class="text-caption text-disabled font-mono">
                  ID: {{ vocabulary.id }}<br>
                  JMdict ID: {{ vocabulary.jmdictId }}<br>
                  Last update: {{ updatedAtFormatted }}
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
  import type { KanaForm, KanjiForm, SenseDetails, SenseExample, SenseGloss } from '@/types/Vocabulary'
  import { computed, onMounted, ref, watch } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { useHead } from '@unhead/vue'
  import LanguageSelector from '@/components/search/LanguageSelector.vue'
  import { useVocabularyStore } from '@/stores/vocabulary'
  import { DEFAULT_LANGUAGE, languageMatches } from '@/utils/language'
  import { playPronunciation } from '@/utils/audio'
  import { useResponsiveTooltip } from '@/composables/useResponsiveTooltip'
  import AdSenseWidget from '@/components/common/AdSenseWidget.vue'

  const route = useRoute()
  const router = useRouter()
  const store = useVocabularyStore()
  const selectedLanguage = ref<string>(DEFAULT_LANGUAGE)
  const { isMobile } = useResponsiveTooltip()

  // Selection state - for kanji words we select kanji, for kana-only we select kana
  const selectedFormText = ref<string | null>(null)

  const term = computed(() => (route.params as any).term as string)
  // Fetch data
  async function loadData () {
    if (term.value) {
      await store.getVocabularyDetails(term.value)
    }
  }

  onMounted(() => {
    loadData()
  })

  watch(() => term.value, () => {
    selectedFormText.value = null
    loadData()
  })

  const vocabulary = computed(() => store.vocabularyDetails)

  const updatedAtFormatted = computed(() => {
    return new Date(vocabulary.value?.updatedAt as Date).toLocaleString(undefined, {
      dateStyle: 'short',
      timeStyle: 'short'
    })
  })

  // Determine if this is a kanji-based word or kana-only word
  const hasKanjiForms = computed(() => {
    return vocabulary.value?.kanjiForms && vocabulary.value.kanjiForms.length > 0
  })

  // Initialize selection when vocabulary loads
  watch(() => vocabulary.value, v => {
    if (!v) return

    // Check if current selection is valid for this newly loaded vocabulary
    let isValidSelection = false
    if (selectedFormText.value) {
      const inKanji = v.kanjiForms?.some(k => k.text === selectedFormText.value)
      // Only check kana forms if we are in kana-only mode or if we support mixed selection (though currently we separate them)
      // But purely for validity, if it exists in either, it might be valid.
      // However, our logic separates them.
      // If hasKanjiForms is true, we expect selection to be a kanji form.
      // If hasKanjiForms is false, we expect selection to be a kana form.
      
      if (v.kanjiForms?.length > 0) {
          isValidSelection = v.kanjiForms.some(k => k.text === selectedFormText.value)
      } else {
          isValidSelection = v.kanaForms?.some(k => k.text === selectedFormText.value) ?? false
      }
    }

    // If selection is missing or invalid for this term, set default
    if (!selectedFormText.value || !isValidSelection) {
      if (v.kanjiForms?.length > 0) {
        // Kanji exists - select first kanji form
        selectedFormText.value = v.kanjiForms[0]?.text ?? ''
      } else if (v.kanaForms?.length > 0) {
        // Kana-only - select first kana form (kana is the "form")
        selectedFormText.value = v.kanaForms[0]?.text ?? ''
      } else {
        selectedFormText.value = null
      }
    }
  }, { immediate: true })

  // For kanji words: get the selected kanji form
  const selectedKanjiForm = computed<KanjiForm | null>(() => {
    const v = vocabulary.value
    if (!v || !hasKanjiForms.value || !selectedFormText.value) return null
    return v.kanjiForms?.find(k => k.text === selectedFormText.value) || null
  })

  // For kana-only words: get the selected kana form
  const selectedKanaFormOnly = computed<KanaForm | null>(() => {
    const v = vocabulary.value
    if (!v || hasKanjiForms.value || !selectedFormText.value) return null
    return v.kanaForms?.find(k => k.text === selectedFormText.value) || null
  })

  // Get kana forms that match the selected kanji (for kanji words)
  const matchingKanaForms = computed<KanaForm[]>(() => {
    const v = vocabulary.value
    if (!v) return []
    if (!hasKanjiForms.value) {
      // Kana-only: no "matching" concept, just return empty
      return []
    }
    if (!selectedFormText.value) {
      // No selection, match all
      return v.kanaForms || []
    }
    return v.kanaForms.filter(kana => {
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
    return selectedFormText.value || vocabulary.value?.kanaForms?.[0]?.text || ''
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

  // Display the isCommon status based on selection
  const selectedIsCommon = computed(() => {
    if (hasKanjiForms.value) {
      if (selectedKanjiForm.value?.isCommon) return true
      const matchingKana = matchingKanaForms.value[0]
      return matchingKana?.isCommon || false
    }
    // Kana-only
    return selectedKanaFormOnly.value?.isCommon || false
  })

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

  // Language Handling
  function getLanguagesFromSense (sense: SenseDetails, languages: Set<string>) {
    if (sense.glosses) {
      for (const g of sense.glosses) {
        if (g.language) languages.add(g.language)
      }
    }
    if (sense.examples) {
      for (const ex of sense.examples) {
        for (const s of ex.sentences) {
          if (s.language && s.language !== 'jpn') {
            languages.add(s.language)
          }
        }
      }
    }
  }

  const availableLanguages = computed(() => {
    const v = vocabulary.value
    if (!v || !v.senses) return []

    const languages = new Set<string>()
    for (const sense of v.senses) {
      getLanguagesFromSense(sense, languages)
    }
    return Array.from(languages)
  })

  function onLanguageChanged (lang: string) {
    selectedLanguage.value = lang
  }

  // Check if a sense applies to the selected form
  function senseAppliesToSelection (sense: SenseDetails): boolean {
    const kanjiMatches = !sense.appliesToKanji
      || sense.appliesToKanji.length === 0
      || sense.appliesToKanji.includes('*')
      || Boolean(selectedKanjiText.value && sense.appliesToKanji.includes(selectedKanjiText.value))

    const kanaMatches = !sense.appliesToKana
      || sense.appliesToKana.length === 0
      || sense.appliesToKana.includes('*')
      || Boolean(selectedKanaText.value && sense.appliesToKana.includes(selectedKanaText.value))

    return Boolean(kanjiMatches) && Boolean(kanaMatches)
  }

  const filteredSenses = computed(() => {
    const v = vocabulary.value
    if (!v || !v.senses) return []

    return v.senses
      .filter(sense => senseAppliesToSelection(sense))
      .map(sense => {
        const hasGlosses = sense.glosses?.some(g => languageMatches(g.language, selectedLanguage.value))
        if (!hasGlosses) return null

        return {
          ...sense,
          glosses: sense.glosses.filter(g => languageMatches(g.language, selectedLanguage.value)),
        }
      }).filter((s): s is SenseDetails => s !== null)
  })

  function getGlossText (sense: SenseDetails) {
    return sense.glosses.map((g: SenseGloss) => g.text).join('; ')
  }

  function getExampleSentence (example: SenseExample, lang: string) {
    const sentence = example.sentences.find(s => languageMatches(s.language, lang))
    return sentence ? sentence.text : ''
  }

  function goBack () {
    router.back()
  }

  // SEO
  useHead({
    title: computed(() => vocabulary.value ? `Vocabulary: ${selectedKanjiText.value || selectedKanaText.value} - JP Reference` : 'Loading Vocabulary...'),
    meta: [
      {
        name: 'description',
        content: computed(() => {
          if (!vocabulary.value) return 'Loading vocabulary details...'
          const meanings = filteredSenses.value.map(s => getGlossText(s)).join('; ')
          return `Details for vocabulary ${selectedKanjiText.value || selectedKanaText.value} (${selectedKanaText.value}). Meanings: ${meanings}`
        })
      }
    ]
  })
</script>

<style lang="scss" scoped>
.font-jp {
  font-family: 'Noto Sans JP', sans-serif;
}

.vocabulary-details-container {
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

.sense-block {
  &.border-bottom {
    border-bottom: 1px solid rgba(var(--v-border-color), 0.1);
  }
}

.border-left-primary {
  border-left: 2px solid rgb(var(--v-theme-primary));
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
