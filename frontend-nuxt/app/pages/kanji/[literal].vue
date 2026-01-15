<template>
  <v-container class="kanji-detail-page fill-height align-start" fluid>
    <v-row class="w-100" justify="center">
      <v-col cols="12" lg="10" xl="8">
        <!-- Loading State -->
        <div v-if="pending" class="d-flex justify-center align-center py-12">
          <v-progress-circular color="primary" indeterminate size="64" />
        </div>

        <!-- Error State -->
        <v-alert
          v-else-if="error || !kanji"
          class="mb-4"
          type="error"
          variant="tonal"
        >
          {{ error?.message || 'Kanji not found' }}
          <template #append>
            <v-btn variant="text" @click="goBack">Go Back</v-btn>
          </template>
        </v-alert>

        <!-- Content -->
        <div v-else class="kanji-detail-content animate-fade-in">
          <!-- Header Section -->
          <header class="kanji-header mb-6">
            <v-row class="justify-space-between">
              <v-col class="v-col-auto main-char">
                <div class="d-flex flex-column gap-2">
                  <h1 class="display-char text-h1 font-jp text-center">
                    {{ kanji.literal }}
                  </h1>
                  <div class="char-meta">
                    <div class="text-body-2 text-medium-emphasis font-weight-regular">
                      <v-icon class="mr-1" icon="mdi-pencil" size="small" />
                      {{ kanji.strokeCount }} strokes
                    </div>
                  </div>
                </div>
              </v-col>

              <v-col class="header-actions v-col-auto justify-space-between d-flex flex-column">
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
                <div class="badges d-flex justify-end gap-2">
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
              </v-col>
            </v-row>
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
                    <div v-else class="mb-3">
                      <div class="text-caption font-weight-bold text-disabled mb-1">ON'YOMI</div>
                      <div class="d-flex flex-wrap gap-2">
                        <v-label
                          color="grey-darken-2"
                          variant="outlined"
                        >
                          No readings available
                        </v-label>
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
                    <div v-else>
                      <div class="text-caption font-weight-bold text-disabled mb-1">KUN'YOMI</div>
                      <div class="d-flex flex-wrap gap-2">
                        <v-label
                          color="grey-darken-2"
                          variant="outlined"
                        >
                          No readings available
                        </v-label>
                      </div>
                    </div>
                    <div v-if="kanji.nanori && kanji.nanori.length > 0" class="mt-3">
                      <div class="text-caption font-weight-bold text-disabled mb-1">NANORI</div>
                      <div class="d-flex flex-wrap gap-2">
                        <v-chip
                          v-for="(reading, index) in kanji.nanori"
                          :key="`nanori_${index}`"
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
                <h2 class="text-h5 font-weight-bold mb-4 d-flex align-center justify-space-between">
                  <div class="text-xs-body-2">
                    <v-icon class="mr-2" color="success" icon="mdi-book-open-page-variant-outline" start />
                    Top words containing this kanji
                  </div>
                  <v-chip
                    v-if="kanji.vocabularyReferences?.totalCount! > 5"
                    class="ml-3" color="primary" size="small" variant="flat" @click="searchForAllReferences">
                    Or see all {{ kanji.vocabularyReferences?.totalCount! }} words
                  </v-chip>
                </h2>

                <div class="vocabulary-iterator overflow-y-auto pl-2 pr-2 pt-2">
                  <template v-if="hasVocabulary">
                    <VocabularySummary
                      v-for="vocabulary in vocabularyReferences"
                      :key="vocabulary.id"
                      :vocabulary="vocabulary"
                    />
                  </template>
                  <template v-else>
                    <v-card class="pa-8 text-center" variant="outlined">
                      <v-icon color="grey-lighten-1" size="64">mdi-text-search</v-icon>
                      <div class="text-h6 mt-4 text-grey">No vocabulary found</div>
                    </v-card>
                  </template>
                </div>
              </section>
            </v-col>

            <!-- Sidebar -->
            <v-col cols="12" md="4">
              <!-- Radicals -->
              <section v-if="kanji?.radicals && kanji.radicals.length > 0" class="radicals-section mb-6">
                <v-card class="pa-4 rounded-lg border-thin" variant="outlined">
                  <h3 class="text-overline font-weight-bold mb-2 text-medium-emphasis">Radicals</h3>
                  <div class="d-flex flex-wrap gap-2">
                    <v-btn
                      v-for="(radical, index) in kanji.radicals"
                      :key="radical.id || `rad-${index}`"
                      class="font-jp text-h6 px-3"
                      :style="radical.hasDetails ? 'cursor: pointer' : 'cursor: default'"
                      :color="radical.hasDetails ? 'purple' : 'gray'"
                      height="48"
                      :to="radical.hasDetails ? `/radical/${radical.literal}` : undefined"
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
                    <ClientOnly>
                      <StrokePlayer
                        :text="kanjiLiteral"
                        uri="/kanjivg/"
                      />
                    </ClientOnly>

                    <ClientOnly>
                      <VueDmak
                        :text="kanjiLiteral"
                        uri="/kanjivg/"
                        view="series"
                        :width="90"
                        :height="90"
                        :stroke="seriesStroke"
                        :seriesStyle="seriesStyle"
                        :seriesFrameStyle="frameStyle"
                      />
                    </ClientOnly>

                    <!-- Just put in the literal and let google figure it out :D -->
                    <v-btn
                      color="primary"
                      class="text-none mt-2 align-self-center"
                      width="300px"
                      prepend-icon="mdi-volume-high"
                      variant="outlined"
                      @click="playPronunciation(kanji.literal)"
                    >
                      Play pronunciation
                    </v-btn>
                  </div>
                </v-card>
              </section>

              <!-- Other details accordion -->
              <section class="other-details-section mb-6">
                <v-card class="pa-4 rounded-lg border-thin" variant="outlined">
                  <h3 class="text-overline font-weight-bold mb-2 text-medium-emphasis">Other Info</h3>
                  <v-expansion-panels variant="accordion">
                    <v-expansion-panel elevation="1">
                      <v-expansion-panel-title>Codepoints</v-expansion-panel-title>
                      <v-expansion-panel-text>
                        <div v-if="codepoints.length > 0" class="reference-grid">
                          <v-card
                            v-for="(cp, index) in codepoints"
                            :key="index"
                            class="ref-card transition-swing"
                            elevation="0"
                            rounded="lg"
                            variant="outlined"
                          >
                            <v-card-text class="py-3">
                              <div class="text-caption text-medium-emphasis font-weight-bold line-height-1 mb-1 text-wrap">
                                {{ cp.displayName }}
                              </div>
                              <div class="font-weight-regular entry-value">
                                {{ cp.value }}
                              </div>
                            </v-card-text>
                          </v-card>
                        </div>
                        <div v-else class="pa-4 text-medium-emphasis font-italic">
                          No codepoints available.
                        </div>
                      </v-expansion-panel-text>
                    </v-expansion-panel>
                    <v-expansion-panel elevation="1">
                      <v-expansion-panel-title>Query codes</v-expansion-panel-title>
                      <v-expansion-panel-text>
                        <div v-if="queryCodes.length > 0" class="reference-grid">
                          <v-card
                            v-for="(qc, index) in queryCodes"
                            :key="index"
                            class="ref-card transition-swing"
                            elevation="0"
                            rounded="lg"
                            variant="outlined"
                          >
                            <v-card-text class="py-3">
                              <div class="text-caption text-medium-emphasis font-weight-bold line-height-1 mb-1 text-wrap">
                                {{ qc.displayName }}
                              </div>
                              <div class="font-weight-regular entry-value">
                                {{ qc.value }}
                              </div>
                            </v-card-text>
                          </v-card>
                        </div>
                        <div v-else class="pa-4 text-medium-emphasis font-italic">
                          No query codes available.
                        </div>
                      </v-expansion-panel-text>
                    </v-expansion-panel>
                    <v-expansion-panel elevation="1">
                      <v-expansion-panel-title>Dictionary references</v-expansion-panel-title>
                      <v-expansion-panel-text>
                        <div v-if="dictionaryReferences.length > 0" class="reference-grid">
                          <v-card
                            v-for="ref in dictionaryReferences"
                            class="ref-card transition-swing"
                            elevation="0"
                            rounded="lg"
                            variant="outlined"
                          >
                            <v-card-text class="d-flex align-center justify-space-between py-3">
                              <div class="d-flex align-center overflow-hidden">
                                <div class="text-truncate">
                                  <div class="text-caption text-medium-emphasis font-weight-bold line-height-1 mb-1 text-wrap">
                                    {{ ref.displayName }}
                                  </div>
                                  <div class="font-weight-regular entry-value">
                                    {{ ref.entry }}
                                  </div>
                                </div>
                              </div>

                              <div v-if="ref.isMorohashi" class="d-flex gap-2">
                                <div class="extra-info text-right">
                                  <div class="text-caption text-disabled">VOL</div>
                                  <div class="font-weight-bold">{{ ref.morohashiVolume }}</div>
                                </div>
                                <v-divider vertical class="mx-1" />
                                <div class="extra-info text-right">
                                  <div class="text-caption text-disabled">PAGE</div>
                                  <div class="font-weight-bold">{{ ref.morohashiPage }}</div>
                                </div>
                              </div>
                            </v-card-text>
                          </v-card>
                        </div>
                        <div v-else class="pa-4 text-medium-emphasis font-italic">
                          No dictionary references available.
                        </div>
                      </v-expansion-panel-text>
                    </v-expansion-panel>
                  </v-expansion-panels>
                </v-card>
              </section>
              <!-- Metadata/Ids -->
              <section class="meta-section">
                <div class="text-caption text-disabled font-mono">
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
  import type { KanjiDetails } from '@/types/Kanji'
  import { computed, ref, watch, reactive, defineAsyncComponent } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import LanguageSelector from '@/components/search/LanguageSelector.vue'
  import { useKanjiService, fetchWithError } from '~/services'
  import { useTheme } from 'vuetify'
  import { playPronunciation } from '@/utils/audio'
  import { useSmartNavigation } from '@/composables/useSmartNavigation'

  // Import VueDmak only on client side
  const VueDmak = defineAsyncComponent(() => import('vue-dmak').then(m => m.VueDmak))

  const route = useRoute()
  const router = useRouter()
  const kanjiService = useKanjiService()
  const theme = useTheme()

  // State
  const readingsTab = ref<string>('')
  const selectedLanguage = ref<string>('en')
  const seriesStyle = reactive({
    display: "flex",
    wrap: "no-wrap",
    overflow: "auto",
  })
  const frameStyle = reactive({
    flexShrink: 0,
  })

  // Computed
  const kanjiLiteral = computed(() => (route.params as any).literal as string)

  const { data: kanji, pending, error } = await useAsyncData(
    `kanji-${kanjiLiteral.value}`,
    () => fetchWithError(() => kanjiService.fetchKanjiByLiteral(kanjiLiteral.value)),
    {
      server: true
    }
  )

  const seriesStroke = computed(() => {
    return theme.global.current.value.dark ? {
      attr: {
        stroke: "white"
      }
    } : {
      attr: {
        stroke: "black"
      }
    }
  })

  const codepointNames: Record<string, string> = {
    ucs: 'Unicode 4.0',
    jis208: 'JIS X 0208-1997',
    jis212: 'JIS X 0212-1990',
    jis213: 'JIS X 0213-2000',
    deroo: 'De Roo',
  }

  const updatedAtFormatted = computed(() => {
    return new Date(kanji.value?.updatedAt as Date).toLocaleString(undefined, {
      dateStyle: 'short',
      timeStyle: 'short'
    })
  })

  const codepoints = computed(() => {
    return kanji.value?.codepoints?.map(codepoint => {
      const type = codepoint.type?.toLowerCase()
      return {
        type: codepoint.type,
        displayName: codepointNames[type] || codepoint.type,
        value: codepoint.value,
      }
    }) || []
  })

  const queryCodeNames: Record<string, string> = {
    skip: 'SKIP',
    four_corner: 'Four Corner',
    deroo: '2001 Kanji (De Roo)',
    misclass: 'Misclassification',
    sh_desc: 'The Kanji Dictionary (Spahn & Hadamitzky)'
  }

  const queryCodes = computed(() => {
    return kanji.value?.queryCodes?.map(code => {
      const type = code.type?.toLowerCase()
      return {
        code: code.type,
        displayName: queryCodeNames[type] || code.type,
        value: code.value,
      }
    }) || []
  })

  const dictionaryNames: Record<string, string> = {
    nelson_c: 'Classic Nelson',
    nelson_n: 'New Nelson',
    halpern_njecd: 'Halpern NJECD',
    halpern_kkld: 'The Kodansha Kanji Learners Dictionary',
    halpern_kkld_2ed: 'The Kodansha Kanji Learners Dictionary 2nd Ed.',
    halpern_kkd: 'The Kodansha Kanji Dictionary',
    heisig: 'Remembering The Kanji',
    heisig6: 'Remembering The Kanji 6th Ed.',
    gakken: 'A New Dictionary of Kanji Usage',
    oneill_names: 'Japanese Names',
    oneill_kk: 'Essential Kanji',
    moro: 'Morohashi (Dai Kan-Wa Jiten)',
    henshall: 'A Guide To Remembering Japanese Characters',
    sh_kk: 'Kanji and Kana',
    sh_kk2: 'Kanji and Kana (2011 edition)',
    sakade: 'A Guide To Reading and Writing Japanese',
    jf_cards: 'Japanese Kanji Flashcards, by Max Hodges and Tomoko Okazaki. (Series 1)',
    henshall3: 'A Guide To Reading and Writing Japanese (3rd Edition)',
    tutt_cards: 'The Kanji Way to Japanese Language Power',
    crowley: 'Crowley',
    kanji_in_context: 'Kanji in Context',
    kodansha_compact: 'Kodansha Compact Kanji Guide',
    maniette: 'Les Kanjis dans la tete',
    busy_people: 'Japanese for Busy People vols I-III',
    kanji_and_kana: 'Kanji & Kana',
    denshi_jisho: 'Denshi Jisho',
  }

  const dictionaryReferences = computed(() => {
    return kanji.value?.dictionaryReferences?.map(reference => {
      const isMorohashi = reference.type === 'moro'
      return {
        source: reference.type,
        displayName: dictionaryNames[reference.type] || reference.type,
        entry: reference.value,
        isMorohashi,
        morohashiVolume: reference.morohashiVolume ?? '—',
        morohashiPage: reference.morohashiPage ?? '—',
      }
    }) || []
  })

  const availableLanguages = computed(() => {
    return kanji.value?.meanings?.map(meaning => meaning.language) || []
  })

  const groupedMeanings = computed(() => {
    return kanji.value?.meanings?.filter(meaning => meaning.language === selectedLanguage.value)?.map(meaning => meaning.meaning) || []
  })

  const hasVocabulary = computed(() => {
    return kanji.value?.vocabularyReferences?.vocabulary?.length && kanji.value?.vocabularyReferences?.vocabulary?.length > 0
  })

  const vocabularyReferences = computed(() => {
    return kanji.value?.vocabularyReferences?.vocabulary || []
  })

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

  // Smart navigation - falls back to /search if no in-app history
  const { goBack } = useSmartNavigation()

  // Actions
  function searchForAllReferences () {
    router.push(`/search?query=*${kanji.value?.literal}*&view=tabbed&tab=vocabulary`)
  }

  watch(() => route.params.literal, async (newVal) => {
    await refreshNuxtData(`kanji-${newVal}`)
  })

  // SEO
  useHead({
    title: computed(() => kanji.value ? `Kanji: ${kanji.value.literal} - JP Reference` : 'Loading Kanji...'),
    meta: [
      {
        name: 'description',
        content: computed(() => {
          if (!kanji.value) return 'Loading kanji details...'
          const meanings = groupedMeanings.value.join(', ')
          const on = readingsMap.value.ja_on?.map(r => r.value).join(', ')
          const kun = readingsMap.value.ja_kun?.map(r => r.value).join(', ')
          return `Details for kanji ${kanji.value.literal}. Meanings: ${meanings}. Readings: ${on ? 'On: ' + on : ''} ${kun ? ' Kun: ' + kun : ''}`
        })
      },
      {
        property: 'og:title',
        content: computed(() => kanji.value ? `Kanji: ${kanji.value.literal}` : 'Kanji Details')
      }
    ]
  })
</script>

<style lang="scss" scoped>
.font-jp {
  font-family: 'Noto Sans JP', sans-serif;
  font-weight: 500;
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

.reference-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 12px;
}

.ref-card {
  border-color: rgba(var(--v-border-color), 0.12) !important;
  background-color: rgba(var(--v-theme-surface-variant), 0.05);

  &:hover {
    border-color: rgba(var(--v-theme-primary), 0.3) !important;
    background-color: rgba(var(--v-theme-primary), 0.02);
  }

  .entry-value {
    color: rgb(var(--v-theme-on-surface));
    letter-spacing: 0.5px;
  }
}

.ref-icon-container {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  background-color: rgba(var(--v-theme-on-surface), 0.04);
}

.extra-info {
  min-width: 45px;
  line-height: 1.2;
}

.line-height-1 {
  line-height: 1;
}
</style>
