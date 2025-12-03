<template>
  <v-container class="kanji-detail-page fill-height align-start" fluid>
    <v-row class="w-100" justify="center">
      <v-col cols="12" lg="10" xl="8">
        <!-- Loading State -->
        <div v-if="loading" class="d-flex justify-center align-center py-12">
          <v-progress-circular color="primary" indeterminate size="64" />
        </div>

        <!-- Error State -->
        <v-alert
          v-else-if="error || !kanji"
          class="mb-4"
          type="error"
          variant="tonal"
        >
          {{ error || 'Kanji not found' }}
          <template #append>
            <v-btn variant="text" @click="goBack">Go Back</v-btn>
          </template>
        </v-alert>

        <!-- Content -->
        <div v-else class="kanji-detail-content animate-fade-in">
          <!-- Header Section -->
          <header class="kanji-header mb-6">
            <div class="d-flex flex-column flex-md-row align-md-end justify-space-between">
              <div class="main-char">
                <div class="d-flex align-end">
                  <h1 class="display-char font-weight-light text-h1 mb-0 mr-6 font-jp">
                    {{ kanji.literal }}
                  </h1>
                  <div class="char-meta mb-2">
                    <div class="text-h6 text-medium-emphasis font-weight-regular">
                      <v-icon class="mr-1" icon="mdi-pencil" size="small" />
                      {{ kanji.strokeCount }} strokes
                    </div>
                  </div>
                </div>
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
                    v-if="kanji.frequency"
                    color="primary"
                    label
                    variant="flat"
                  >
                    Top #{{ kanji.frequency }}
                  </v-chip>
                  <v-chip
                    v-if="kanji.jlptLevel"
                    color="secondary"
                    label
                    variant="flat"
                  >
                    N{{ kanji.jlptLevel }}
                  </v-chip>
                  <v-chip
                    v-if="kanji.grade"
                    color="success"
                    label
                    variant="flat"
                  >
                    Grade {{ kanji.grade }}
                  </v-chip>
                </div>
              </div>
            </div>
            <v-divider class="mt-4 mb-6 border-opacity-25" />
          </header>

          <v-row>
            <!-- Main Content: Meanings & Readings -->
            <v-col cols="12" md="8">

              <!-- Meanings -->
              <section class="meanings-section mb-8">
                <div class="d-flex align-center justify-space-between mb-4">
                  <h2 class="text-h5 font-weight-bold d-flex align-center">
                    <v-icon class="mr-2" color="primary" icon="mdi-translate" start />
                    Meanings
                  </h2>
                  <LanguageSelector
                    v-if="availableLanguages.length > 0"
                    :available-languages="availableLanguages"
                    :default-language="selectedLanguage"
                    @language-changed="onLanguageChanged"
                  />
                </div>

                <v-card class="meanings-card bg-surface pa-4" flat>
                  <div v-if="groupedMeanings.length > 0" class="d-flex flex-wrap gap-2">
                    <v-chip
                      v-for="(meaning, index) in groupedMeanings"
                      :key="index"
                      class="font-weight-medium text-body-1 px-4"
                      color="primary"
                      size="large"
                      variant="tonal"
                    >
                      {{ meaning }}
                    </v-chip>
                  </div>
                  <div v-else class="text-medium-emphasis font-italic">
                    No meanings available for the selected language.
                  </div>
                </v-card>
              </section>

              <!-- Readings -->
              <section class="readings-section mb-8">
                <h2 class="text-h5 font-weight-bold mb-4 d-flex align-center">
                  <v-icon class="mr-2" color="secondary" icon="mdi-format-text" start />
                  Readings
                </h2>

                <v-card class="readings-card bg-surface pa-4" flat>
                  <!-- Japanese Readings -->
                  <div class="reading-group mb-6">
                    <div class="text-overline text-medium-emphasis mb-2">Japanese</div>

                    <div v-if="readingsMap.ja_on && readingsMap.ja_on.length > 0" class="mb-3">
                      <div class="text-caption font-weight-bold text-disabled mb-1">ON'YOMI</div>
                      <div class="d-flex flex-wrap gap-2">
                        <v-chip
                          v-for="(reading, index) in readingsMap.ja_on"
                          :key="`ja_on_${index}`"
                          class="font-jp"
                          color="orange-darken-2"
                          variant="outlined"
                        >
                          {{ reading.value }}
                        </v-chip>
                      </div>
                    </div>
                    <div v-if="readingsMap.ja_kun && readingsMap.ja_kun.length > 0">
                      <div class="text-caption font-weight-bold text-disabled mb-1">KUN'YOMI</div>
                      <div class="d-flex flex-wrap gap-2">
                        <v-chip
                          v-for="(reading, index) in readingsMap.ja_kun"
                          :key="`ja_kun_${index}`"
                          class="font-jp"
                          color="deep-orange-darken-2"
                          variant="outlined"
                        >
                          {{ reading.value }}
                        </v-chip>
                      </div>
                    </div>
                  </div>

                  <!-- Other Readings -->
                  <div v-if="hasOtherReadings" class="other-readings">
                    <v-divider class="mb-4 border-opacity-10" />
                    <div class="text-overline text-medium-emphasis mb-2">Other Languages</div>
                    <v-tabs v-model="readingsTab" class="mb-4" color="primary" density="compact">
                      <v-tab
                        v-for="(readings, type) in otherReadingsMap"
                        :key="type"
                        class="text-capitalize"
                        :value="type"
                      >
                        {{ getReadingTypeLabel(type) }}
                      </v-tab>
                    </v-tabs>

                    <v-window v-model="readingsTab">
                      <v-window-item
                        v-for="(readings, type) in otherReadingsMap"
                        :key="type"
                        :value="type"
                      >
                        <div class="d-flex flex-wrap gap-2">
                          <v-chip
                            v-for="(reading, index) in readings"
                            :key="`${type}_${index}`"
                            size="small"
                            variant="tonal"
                          >
                            {{ reading.value }}
                          </v-chip>
                        </div>
                      </v-window-item>
                    </v-window>
                  </div>
                </v-card>
              </section>

              <!-- Associated Vocabulary (Infinite Scroll) -->
              <section class="vocabulary-section mb-8">
                <h2 class="text-h5 font-weight-bold mb-4 d-flex align-center">
                  <v-icon class="mr-2" color="success" icon="mdi-book-open-page-variant-outline" start />
                  Associated Vocabulary
                  <v-chip class="ml-3" color="success" size="small" variant="flat">
                    {{ kanji.vocabularyReferences?.length || 0 }}
                  </v-chip>
                </h2>

                <v-card class="vocabulary-card bg-surface" flat>
                  <div v-if="visibleVocabulary.length === 0" class="pa-4 text-medium-emphasis font-italic">
                    No associated vocabulary found.
                  </div>
                  <v-list v-else class="bg-transparent pa-0">
                    <v-list-item
                      v-for="(vocab, index) in visibleVocabulary"
                      :key="vocab.id || index"
                      class="vocab-item py-2"
                      :class="{ 'border-bottom': index < visibleVocabulary.length - 1 }"
                      hover
                      lines="one"
                      rounded="lg"
                      :to="getVocabularyLink(vocab)"
                    >
                      <template #prepend>
                        <v-avatar class="mr-2 text-success" color="success-lighten-5" size="32">
                          <v-icon size="small">mdi-book-outline</v-icon>
                        </v-avatar>
                      </template>
                      <v-list-item-title class="font-weight-medium font-jp text-body-1">
                        {{ vocab.term }}
                      </v-list-item-title>
                      <template #append>
                        <v-icon color="medium-emphasis" size="small">mdi-chevron-right</v-icon>
                      </template>
                    </v-list-item>

                    <!-- Load More Trigger -->
                    <div v-if="hasMoreVocabulary" class="d-flex justify-center py-4">
                      <v-btn
                        color="primary"
                        :loading="isLoadingMore"
                        variant="text"
                        @click="loadMoreVocabulary"
                      >
                        Load More
                      </v-btn>
                    </div>
                  </v-list>
                </v-card>
              </section>
            </v-col>

            <!-- Sidebar -->
            <v-col cols="12" md="4">
              <!-- Radicals -->
              <section v-if="kanji.radicals && kanji.radicals.length > 0" class="radicals-section mb-6">
                <v-card class="pa-4 rounded-lg border-thin" variant="outlined">
                  <h3 class="text-overline font-weight-bold mb-2 text-medium-emphasis">Radicals</h3>
                  <div class="d-flex flex-wrap gap-2">
                    <v-btn
                      v-for="(radical, index) in kanji.radicals"
                      :key="radical.id || `rad-${index}`"
                      class="font-jp text-h6 px-3"
                      color="purple"
                      height="48"
                      :to="`/radical/${radical.literal}`"
                      variant="tonal"
                    >
                      {{ radical.literal }}
                    </v-btn>
                  </div>
                </v-card>
              </section>

              <!-- Study Tools -->
              <section class="tools-section mb-6">
                <v-card class="pa-4 rounded-lg border-thin" variant="outlined">
                  <h3 class="text-overline font-weight-bold mb-2 text-medium-emphasis">Study Tools</h3>
                  <div class="d-flex flex-column gap-2">
                    <v-btn
                      block
                      prepend-icon="mdi-pencil"
                      variant="outlined"
                      @click="showStrokeOrder"
                    >
                      View Stroke Order
                    </v-btn>
                    <v-btn
                      block
                      prepend-icon="mdi-volume-high"
                      variant="outlined"
                      @click="playPronunciation"
                    >
                      Play Pronunciation
                    </v-btn>
                    <v-btn
                      block
                      color="pink"
                      prepend-icon="mdi-heart-outline"
                      variant="outlined"
                      @click="addToFavorites"
                    >
                      Add to Favorites
                    </v-btn>
                  </div>
                </v-card>
              </section>

              <!-- Info -->
              <section class="info-section">
                <v-card class="pa-4 rounded-lg border-thin" variant="outlined">
                  <h3 class="text-overline font-weight-bold mb-2 text-medium-emphasis">Information</h3>
                  <div class="info-grid">
                    <div class="info-item mb-2">
                      <div class="text-caption text-disabled">Frequency Rank</div>
                      <div class="font-weight-medium">#{{ kanji.frequency || 'N/A' }}</div>
                    </div>
                    <div class="info-item mb-2">
                      <div class="text-caption text-disabled">Grade</div>
                      <div class="font-weight-medium">{{ kanji.grade || 'N/A' }}</div>
                    </div>
                    <div class="info-item">
                      <div class="text-caption text-disabled">Unicode</div>
                      <div class="font-mono text-caption">{{ kanji.codepoints?.[0]?.value || 'N/A' }}</div>
                    </div>
                  </div>
                </v-card>
              </section>
            </v-col>
          </v-row>
        </div>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
  import type { KanjiDetails } from '@/types/Kanji'
  import { computed, onMounted, ref, watch } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import LanguageSelector from '@/components/search/LanguageSelector.vue'
  import { useKanjiStore } from '@/stores/kanji'

  const route = useRoute()
  const router = useRouter()
  const kanjiStore = useKanjiStore()

  // State
  const loading = ref(true)
  const error = ref<string | null>(null)
  const kanji = ref<KanjiDetails | null>(null)
  const readingsTab = ref<string>('')
  const selectedLanguage = ref<string>('en')
  const visibleVocabularyLimit = ref(20)
  const isLoadingMore = ref(false)

  // Computed
  const kanjiLiteral = computed(() => (route.params as any).literal as string)

  const availableLanguages = computed(() => {
    return kanji.value?.meanings?.map(meaning => meaning.language) || []
  })

  const groupedMeanings = computed(() => {
    return kanji.value?.meanings?.filter(meaning => meaning.language === selectedLanguage.value)?.map(meaning => meaning.meaning) || []
  })

  // Vocabulary Logic
  const visibleVocabulary = computed(() => {
    if (!kanji.value?.vocabularyReferences) return []
    return kanji.value.vocabularyReferences.slice(0, visibleVocabularyLimit.value)
  })

  const hasMoreVocabulary = computed(() => {
    if (!kanji.value?.vocabularyReferences) return false
    return visibleVocabularyLimit.value < kanji.value.vocabularyReferences.length
  })

  function loadMoreVocabulary () {
    isLoadingMore.value = true
    // Simulate network delay or just nice UX
    setTimeout(() => {
      visibleVocabularyLimit.value += 20
      isLoadingMore.value = false
    }, 300)
  }

  function getVocabularyLink (vocab: { term?: string }): string {
    return vocab.term ? `/vocabulary/${vocab.term}` : '#'
  }

  // Readings Logic
  const readingTypeMap: Record<string, string> = {
    pinyin: 'Pinyin (Chinese)',
    korean_r: 'Korean (Romanized)',
    korean_h: 'Korean (Hangul)',
    vietnam: 'Vietnamese',
    ja_on: 'On\'yomi',
    ja_kun: 'Kun\'yomi',
  }

  const readingsMap = computed(() => {
    const map: Record<string, NonNullable<KanjiDetails['readings']>> = {
      ja_on: [], ja_kun: [], pinyin: [], korean_r: [], korean_h: [], vietnam: [], other: [],
    }

    if (!kanji.value?.readings) return map

    for (const reading of kanji.value.readings) {
      const type = reading.type?.toLowerCase() || 'other'
      if (type in map) {
        map[type]!.push(reading)
      } else {
        map.other!.push(reading)
      }
    }
    return map
  })

  const otherReadingsMap = computed(() => {
    const map: Record<string, NonNullable<KanjiDetails['readings']>> = {}
    const excludedTypes = new Set(['ja_on', 'ja_kun'])

    for (const [type, readings] of Object.entries(readingsMap.value)) {
      if (!excludedTypes.has(type) && readings && readings.length > 0) {
        map[type] = readings
      }
    }

    // Auto-select first tab if not set
    if (!readingsTab.value && Object.keys(map).length > 0) {
      // eslint-disable-next-line vue/no-side-effects-in-computed-properties
      readingsTab.value = Object.keys(map)[0]!
    }

    return map
  })

  const hasOtherReadings = computed(() => Object.keys(otherReadingsMap.value).length > 0)

  function getReadingTypeLabel (type: string): string {
    return readingTypeMap[type] || type.charAt(0).toUpperCase() + type.slice(1)
  }

  function onLanguageChanged (language: string) {
    selectedLanguage.value = language
  }

  function goBack () {
    router.back()
  }

  // Actions
  function showStrokeOrder () {
    alert(`Stroke order for ${kanji.value?.literal} would be displayed here.`)
  }

  function playPronunciation () {
    const onReadings = readingsMap.value.ja_on
    const text = (onReadings && onReadings.length > 0) ? onReadings[0]!.value : readingsMap.value.ja_kun?.[0]?.value

    if (text) {
      const utterance = new SpeechSynthesisUtterance(text)
      utterance.lang = 'ja-JP'
      speechSynthesis.speak(utterance)
    }
  }

  function addToFavorites () {
    alert(`${kanji.value?.literal} added to favorites!`)
  }

  // Load Data
  async function loadKanji () {
    try {
      loading.value = true
      error.value = null
      visibleVocabularyLimit.value = 20 // Reset limit

      const foundKanji = kanjiStore.kanjiDetailsCache[kanjiLiteral.value]
      if (foundKanji) {
        kanji.value = foundKanji
      } else {
        kanji.value = await kanjiStore.getKanjiByLiteral(kanjiLiteral.value) ?? null
        if (!kanji.value) error.value = 'Kanji not found'
      }
    } catch (error_) {
      console.error('Error loading kanji:', error_)
      error.value = 'Failed to load kanji details'
    } finally {
      loading.value = false
    }
  }

  onMounted(loadKanji)

  watch(() => kanjiLiteral.value, () => {
    loadKanji()
  })

</script>

<style lang="scss" scoped>
.font-jp {
  font-family: 'Noto Sans JP', sans-serif;
}

.kanji-detail-page {
  background-color: rgb(var(--v-theme-background));
}

.kanji-header {
    .display-char {
        line-height: 1;
        color: rgba(var(--v-theme-on-surface), 0.87);
    }
}

.gap-2 {
  gap: 0.5rem;
}

.border-bottom {
   border-bottom: 1px solid rgba(var(--v-border-color), 0.08);
}

.animate-fade-in {
  animation: fadeIn 0.4s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
