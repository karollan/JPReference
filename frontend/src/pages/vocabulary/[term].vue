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
                <div class="badges d-flex gap-2 mb-2">
                  <v-chip
                    v-if="isCommon"
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
              </div>
            </div>
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
                      <v-chip
                        v-for="(form, idx) in vocabulary.kanjiForms"
                        :key="`kf-${idx}`"
                        class="mr-1 mb-1 font-jp"
                        :color="form.isCommon ? 'secondary' : undefined"
                        size="small"
                        :variant="form.text === primaryKanji ? 'flat' : 'outlined'"
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
                            start
                          />
                        </template>
                      </v-chip>
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
                      <v-chip
                        v-for="(form, idx) in vocabulary.kanaForms"
                        :key="`kn-${idx}`"
                        class="mr-1 mb-1 font-jp"
                        :color="form.isCommon ? 'secondary' : undefined"
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
                v-if="vocabulary.containedKanji && vocabulary.containedKanji.length > 0"
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

              <!-- Metadata/Ids -->
              <section class="meta-section">
                <div class="text-caption text-disabled font-mono">
                  ID: {{ vocabulary.id }}<br>
                  JMdict ID: {{ vocabulary.jmdictId }}
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
  import type { SenseDetails, SenseExample, SenseGloss } from '@/types/Vocabulary'
  import { computed, onMounted, ref, watch } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import LanguageSelector from '@/components/search/LanguageSelector.vue'
  import { useVocabularyStore } from '@/stores/vocabulary'
  import { DEFAULT_LANGUAGE, languageMatches } from '@/utils/language'

  const route = useRoute()
  const router = useRouter()
  const store = useVocabularyStore()
  const selectedLanguage = ref<string>(DEFAULT_LANGUAGE)

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
    loadData()
  })

  const vocabulary = computed(() => store.vocabularyDetails)

  // Primary Display Logic
  const primaryKanji = computed(() => {
    const v = vocabulary.value
    if (!v) return ''
    if (v.kanjiForms?.length > 0) {
      return v.kanjiForms[0]?.text
    }
    return ''
  })

  const primaryKana = computed(() => {
    const v = vocabulary.value
    if (!v) return ''
    // Find kana that applies to primary kanji
    if (primaryKanji.value) {
      const match = v.kanaForms.find(k =>
        !k.appliesToKanji
        || k.appliesToKanji.length === 0
        || k.appliesToKanji.includes('*')
        || k.appliesToKanji.includes(primaryKanji.value as string),
      )
      if (match) return match.text
    }
    // Fallback to first kana
    if (v.kanaForms && v.kanaForms.length > 0) {
      return v.kanaForms[0]?.text
    }
    return ''
  })

  const isCommon = computed(() => {
    const v = vocabulary.value
    if (!v) return false
    const pKanji = v.kanjiForms?.find(k => k.text === primaryKanji.value)
    const pKana = v.kanaForms.find(k => k.text === primaryKana.value)
    return pKanji?.isCommon || pKana?.isCommon || false
  })

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

  const filteredSenses = computed(() => {
    const v = vocabulary.value
    if (!v || !v.senses) return []

    return v.senses.map(sense => {
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
</style>
