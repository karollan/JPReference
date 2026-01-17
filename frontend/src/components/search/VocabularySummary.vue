<template>
  <v-hover v-slot="{ isHovering, props: hoverProps }">
    <v-card
      v-ripple
      v-bind="hoverProps"
      class="pa-3 mb-3 interactive-card"
      :elevation="isHovering && !isMobile ? 4 : 2"
      outlined
      @click="handleCardClick"
    >
      <div class="d-flex justify-space-between align-start mb-2">
        <div class="vocabulary-primary flex-grow-1">
          <!-- Primary entry: Kanji with main reading, or Kana-only -->
            <div class="primary-entry">
              <template v-if="hasTags">
                <v-tooltip
                  location="top"
                  :open-on-click="isMobile"
                  :open-on-hover="!isMobile"
                  :persistent="false"
                >
                  <template #activator="{ props: tooltipProps }">
                    <span v-bind="tooltipProps" class="word-with-tags">
                      <FuriganaText
                        :text="primaryText"
                        :reading="primaryReading"
                        :furigana="vocabulary.furigana"
                      />
                    </span>
                  </template>
                  <div class="tag-tooltip-content">
                    <div v-if="primaryKanjiTags.length > 0">
                      <div class="text-caption font-weight-bold mb-1">Kanji Tags:</div>
                      <div v-for="(tag, idx) in primaryKanjiTags" :key="`kj-${idx}`">
                        • {{ tag.description }}
                      </div>
                    </div>
                    <div v-if="primaryKanaTags.length > 0" :class="{ 'mt-2': primaryKanjiTags.length > 0 }">
                      <div class="text-caption font-weight-bold mb-1">Kana Tags:</div>
                      <div v-for="(tag, idx) in primaryKanaTags" :key="`kn-${idx}`">
                        • {{ tag.description }}
                      </div>
                    </div>
                  </div>
                </v-tooltip>
              </template>
              <template v-else>
                <FuriganaText
                  :text="primaryText"
                  :reading="primaryReading"
                  :furigana="vocabulary.furigana"
                />
              </template>
            </div>
        </div>
        <LanguageSelector
          v-if="availableLanguages.length > 0"
          :available-languages="availableLanguages"
          :default-language="selectedLanguage"
          @language-changed="onLanguageChanged"
        />
      </div>

      <v-row>
        <v-col cols="12" md="3">
          <div class="metadata-section">
            <!-- Badges -->
            <div class="badges-container">
              <v-chip
                v-if="vocabulary.isCommon"
                class="mr-1 mb-1"
                color="success"
                size="small"
                variant="flat"
              >
                Common
              </v-chip>
              <v-chip
                v-if="vocabulary.jlptLevel"
                class="mr-1 mb-1"
                color="primary"
                size="small"
                variant="flat"
              >
                N{{ vocabulary.jlptLevel }}
              </v-chip>
            </div>

            <!-- Other Kanji Forms with Readings -->
            <div v-if="otherKanjiWithReadings.length > 0" class="other-forms text-left mt-3">
              <div class="section-label">Other Forms</div>
              <div class="forms-list">
                <div
                  v-for="(entry, idx) in otherKanjiWithReadings"
                  :key="`kanji-form-${idx}`"
                  class="form-item"
                >
                  <template v-if="entry.kanji.tags?.length > 0">
                    <v-tooltip
                      location="top"
                      :open-on-click="isMobile"
                      :open-on-hover="!isMobile"
                      :persistent="false"
                    >
                      <template #activator="{ props: tooltipProps }">
                        <span v-bind="tooltipProps" class="kanji-with-tags">{{ entry.kanji.text }}</span>
                      </template>
                      <div class="tag-tooltip-content">
                        <div v-for="(tag, idx) in entry.kanji.tags" :key="idx">
                          • {{ tag.description }}
                        </div>
                      </div>
                    </v-tooltip>
                  </template>
                  <template v-else>
                    <span class="kanji-text">{{ entry.kanji.text }}</span>
                  </template>
                  <span v-if="entry.readings.length > 0" class="reading-text">
                    (<template v-for="(reading, ridx) in entry.readings" :key="`reading-${ridx}`">
                      <template v-if="reading.tags && reading.tags.length > 0">
                        <v-tooltip
                          location="top"
                          :open-on-click="isMobile"
                          :open-on-hover="!isMobile"
                          :persistent="false"
                        >
                          <template #activator="{ props: tooltipProps }">
                            <span v-bind="tooltipProps" class="kana-with-tags">{{ reading.text }}</span>
                          </template>
                          <div class="tag-tooltip-content">
                            <div v-for="(tag, tidx) in reading.tags" :key="tidx">
                              • {{ tag.description }}
                            </div>
                          </div>
                        </v-tooltip>
                      </template>
                      <template v-else>{{ reading.text }}</template>
                      <template v-if="ridx < entry.readings.length - 1">、</template>
                    </template>)
                  </span>
                  <v-chip
                    v-if="entry.kanji.isCommon"
                    class="ml-1"
                    color="success"
                    size="x-small"
                    variant="tonal"
                  >
                    Common
                  </v-chip>
                </div>
              </div>
            </div>

            <!-- Standalone Kana Forms (no kanji match) -->
            <div v-if="standaloneKanaForms.length > 0" class="other-forms text-left mt-3">
              <div class="section-label">Kana Readings</div>
              <div class="forms-list">
                <div
                  v-for="(kana, idx) in standaloneKanaForms"
                  :key="`kana-form-${idx}`"
                  class="form-item"
                >
                  <template v-if="kana.tags && kana.tags.length > 0">
                    <v-tooltip
                      location="top"
                      :open-on-click="isMobile"
                      :open-on-hover="!isMobile"
                      :persistent="false"
                    >
                      <template #activator="{ props: tooltipProps }">
                        <span v-bind="tooltipProps" class="kana-with-tags">{{ kana.text }}</span>
                      </template>
                      <div class="tag-tooltip-content">
                        <div v-for="(tag, tidx) in kana.tags" :key="tidx">
                          • {{ tag.description }}
                        </div>
                      </div>
                    </v-tooltip>
                  </template>
                  <template v-else>
                    <span class="kana-text">{{ kana.text }}</span>
                  </template>
                  <v-chip
                    v-if="kana.isCommon"
                    class="ml-1"
                    color="success"
                    size="x-small"
                    variant="tonal"
                  >
                    Common
                  </v-chip>
                </div>
              </div>
            </div>
          </div>
        </v-col>

        <v-col class="text-left" cols="12" md="9">
          <div class="meanings-section">
            <div
              v-for="(sense, index) in filteredSenses"
              :key="index"
              class="sense-item"
            >
              <div class="sense-header">
                <span class="sense-number">{{ index + 1 }}.</span>
                <div class="sense-content">
                  <div v-if="sense.tags && sense.tags.length > 0" class="sense-tags mt-1">
                    <v-chip
                      v-for="(tag, idx) in sense.tags"
                      :key="idx"
                      class="mr-1"
                      color="secondary"
                      size="x-small"
                      variant="tonal"
                    >
                      {{ tag.description }}
                    </v-chip>
                  </div>
                  <div class="glosses">
                    {{ getFilteredGlosses(sense) }}
                  </div>
                  <div v-if="sense.info && sense.info.length > 0" class="sense-info mt-1">
                    <div v-for="(info, idx) in sense.info" :key="idx" class="info-text">
                      <v-icon class="mr-1" size="x-small">mdi-information-outline</v-icon>
                      {{ info }}
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </v-col>
      </v-row>
    </v-card>
  </v-hover>
</template>
<script lang="ts" setup>
  import type { KanaForm, KanjiForm, VocabularySummary } from '@/types/Vocabulary'
  import { computed, ref } from 'vue'
  import { DEFAULT_LANGUAGE, languageMatches } from '@/utils/language'
  import { collectAllKanaForms, getStandaloneKanaForms, getUsedKanaTexts, groupKanjiWithReadings } from '@/utils/kanjiKanaForms'
  import LanguageSelector from './LanguageSelector.vue'
  import { useResponsiveTooltip } from '@/composables/useResponsiveTooltip'

  // Import the shared interface
  import type { KanjiWithReadings } from '@/utils/kanjiKanaForms'

  const props = defineProps<{
    vocabulary: VocabularySummary
  }>()

  const router = useRouter()
  const selectedLanguage = ref<string>(DEFAULT_LANGUAGE)
  const { isMobile } = useResponsiveTooltip()

  // Extract available languages from all senses
  const availableLanguages = computed(() => {
    const languages = new Set<string>()
    if (props.vocabulary.senses) for (const sense of props.vocabulary.senses) {
      if (sense.glosses) for (const gloss of sense.glosses) {
        if (gloss.language) {
          languages.add(gloss.language)
        }
      }
    }
    return Array.from(languages)
  })

  function onLanguageChanged (language: string) {
    selectedLanguage.value = language
  }

  // Filter senses to show only those with glosses in selected language
  const filteredSenses = computed(() => {
    return props.vocabulary.senses?.map(sense => ({
      ...sense,
      glosses: sense.glosses?.filter(gloss => languageMatches(gloss.language, selectedLanguage.value)) || [],
    })).filter(sense => sense.glosses.length > 0) || []
  })

  function getFilteredGlosses (sense: any) {
    return sense.glosses.map((gloss: any) => gloss.text).join(', ')
  }

  // Get the primary kana text for the primary kanji
  const primaryKanaText = computed(() => {
    if (!props.vocabulary.primaryKana) return ''
    return props.vocabulary.primaryKana.text
  })

  // Tags from primary kana only
  const primaryKanaTags = computed(() => {
    return props.vocabulary.primaryKana?.tags || []
  })

  const primaryKanjiTags = computed(() => {
    return props.vocabulary.primaryKanji?.tags || []
  })

  // Has any tags (kanji or kana)
  const hasTags = computed(() => {
    return primaryKanjiTags.value.length > 0 || primaryKanaTags.value.length > 0
  })

  const primaryText = computed(() => {
    return props.vocabulary.primaryKanji?.text || props.vocabulary.primaryKana?.text || ''
  })

  const primaryReading = computed(() => {
    return props.vocabulary.primaryKanji ? primaryKanaText.value : null
  })

  // Get all kana forms for matching (using shared utility)
  const allKanaForms = computed(() => collectAllKanaForms(
    props.vocabulary.primaryKana,
    props.vocabulary.otherKanaForms,
  ))

  // Track which kana forms have been used (matched to a kanji)
  const usedKanaTexts = computed(() => getUsedKanaTexts(
    props.vocabulary.primaryKanji,
    props.vocabulary.primaryKana,
    props.vocabulary.otherKanjiForms,
    allKanaForms.value,
  ))

  // Group other kanji forms with their applicable kana readings
  const otherKanjiWithReadings = computed<KanjiWithReadings<KanjiForm, KanaForm>[]>(() =>
    groupKanjiWithReadings(props.vocabulary.otherKanjiForms, allKanaForms.value),
  )

  // Standalone kana forms - those not matched to any kanji
  const standaloneKanaForms = computed<KanaForm[]>(() => getStandaloneKanaForms(
    props.vocabulary.primaryKanji,
    props.vocabulary.otherKanjiForms,
    props.vocabulary.otherKanaForms,
    usedKanaTexts.value,
  ))

  function handleCardClick () {
    const routePath = props.vocabulary.slug
    router.push(`/vocabulary/${routePath}`)
  }
</script>
<style lang="scss" scoped>
.vocabulary-primary {
    font-size: 1.75rem;
    font-weight: 500;
    text-align: left;
    line-height: 1.2;

    .primary-entry {
        display: flex;
        align-items: baseline;
        flex-wrap: wrap;
        gap: 0.25rem;
    }

    ruby {
        rt {
            font-size: 0.8rem;
            color: rgba(var(--v-theme-on-surface), 0.6);

            .kana-with-tags {
                color: rgb(var(--v-theme-info));
                cursor: help;
                border-bottom: 1px dotted rgb(var(--v-theme-info));

                &:hover {
                    opacity: 0.8;
                }
            }
        }
    }

    .vocabulary-text {
        display: block;
    }

    .kana-with-tags-large {
        color: rgb(var(--v-theme-info));
        cursor: help;
        border-bottom: 2px dotted rgb(var(--v-theme-info));

        &:hover {
            opacity: 0.8;
        }
    }
}

.kana-with-tags, .kanji-with-tags, .word-with-tags {
    color: rgb(var(--v-theme-info));
    cursor: help;
    border-bottom: 1px dotted rgb(var(--v-theme-info));

    &:hover {
        opacity: 0.8;
    }
}

.tag-tooltip-content {
    max-width: 300px;
    text-align: left;
}

.metadata-section {
    .section-label {
        font-size: 0.75rem;
        font-weight: 600;
        color: rgba(var(--v-theme-on-surface), 0.6);
        text-transform: uppercase;
        margin-bottom: 0.5rem;
    }

    .badges-container {
        display: flex;
        flex-wrap: wrap;
        gap: 4px;
    }

    .other-forms {
        .forms-list {
            display: flex;
            flex-direction: column;
            gap: 0.5rem;
        }

        .form-item {
            display: flex;
            align-items: center;
            flex-wrap: wrap;
            gap: 2px;
            font-size: 1rem;
            line-height: 1.4;

            .kanji-text {
                font-weight: 500;
                color: rgba(var(--v-theme-on-surface), 0.87);
            }

            .reading-text {
                color: rgba(var(--v-theme-on-surface), 0.6);
                font-size: 0.9rem;
            }

            .kana-text {
                font-weight: 500;
                color: rgba(var(--v-theme-on-surface), 0.87);
            }
        }
    }
}

.meanings-section {
    .sense-item {
        margin-bottom: 0.75rem;

        &:last-child {
            margin-bottom: 0;
        }
    }

    .sense-header {
        display: flex;
        gap: 0.5rem;
        align-items: flex-start;
    }

    .sense-number {
        font-weight: 600;
        color: rgba(var(--v-theme-on-surface), 0.87);
        min-width: 1.2rem;
        font-size: 0.9rem;
    }

    .sense-content {
        flex: 1;
    }

    .glosses {
        font-size: 1rem;
        line-height: 1.4;
    }

    .sense-tags {
        display: flex;
        flex-wrap: wrap;
        gap: 4px;
    }

    .sense-info {
        .info-text {
            display: flex;
            align-items: center;
            font-size: 0.85rem;
            color: rgba(var(--v-theme-on-surface), 0.6);
            font-style: italic;
            margin-top: 0.25rem;
        }
    }
}

.interactive-card {
    cursor: pointer;
    transition: box-shadow 0.2s ease, transform 0.2s ease;
    border-color: rgba(var(--v-border-color), var(--v-border-opacity));

    &:hover {
        transform: translateY(-2px);
        border-color: rgba(var(--v-theme-primary), 0.5);
    }
}
</style>
