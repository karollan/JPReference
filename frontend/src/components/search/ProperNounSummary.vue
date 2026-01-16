<template>
  <v-hover v-slot="{ isHovering, props: hoverProps }">
    <v-card
      v-ripple
      v-bind="hoverProps"
      class="pa-3 mb-3 interactive-card text-left"
      :elevation="isHovering ? 4 : 2"
      outlined
      @click="handleCardClick"
    >
      <div class="d-flex justify-space-between align-start mb-2">
        <div class="proper-noun-primary flex-grow-1">
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
                        :furigana="properNoun.furigana"
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
                  :furigana="properNoun.furigana"
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

      <div class="content-grid">
        <!-- Metadata Column -->
        <div v-if="hasOtherForms" class="metadata-col">
          <!-- Other Kanji Forms with Readings -->
          <div v-if="otherKanjiWithReadings.length > 0" class="other-forms">
            <div class="section-label">Also:</div>
            <div class="forms-list">
              <span
                v-for="(entry, idx) in otherKanjiWithReadings"
                :key="`kanji-form-${idx}`"
                class="form-item mr-2"
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
                      <div v-for="(tag, tidx) in entry.kanji.tags" :key="tidx">
                        • {{ tag.description }}
                      </div>
                    </div>
                  </v-tooltip>
                </template>
                <template v-else>{{ entry.kanji.text }}</template>
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
              </span>
            </div>
          </div>

          <!-- Standalone Kana Forms (no kanji match) -->
          <div v-if="standaloneKanaForms.length > 0" class="other-forms mt-1">
            <div class="section-label">Readings:</div>
            <div class="forms-list">
              <span
                v-for="(kana, idx) in standaloneKanaForms"
                :key="`kana-form-${idx}`"
                class="form-item mr-2"
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
                <template v-else>{{ kana.text }}</template>
              </span>
            </div>
          </div>
        </div>

        <!-- Translations Column -->
        <div class="translations-col">
          <div class="translations-section">
            <div
              v-for="(translation, index) in filteredTranslations"
              :key="index"
              class="translation-item"
            >
              <div class="translation-header">
                <span class="translation-number">{{ index + 1 }}.</span>
                <div class="translation-content">
                  <div class="translation-text">
                    {{ translation.text }}
                  </div>
                  <div v-if="translation.types && translation.types.length > 0" class="translation-types mt-1">
                    <v-chip
                      v-for="(type, idx) in translation.types"
                      :key="idx"
                      class="mr-1"
                      color="secondary"
                      size="x-small"
                      variant="tonal"
                    >
                      {{ type.description }}
                    </v-chip>
                  </div>
                </div>
              </div>
            </div>
            <div v-if="filteredTranslations.length === 0" class="no-translations">
              <v-icon class="mr-1" size="small">mdi-translate-off</v-icon>
              No translations available in selected language
            </div>
          </div>
        </div>
      </div>
    </v-card>
  </v-hover>
</template>

<script lang="ts" setup>
  import type { KanaForm, KanjiForm, ProperNounSummary } from '@/types/ProperNoun'
  import type { KanjiWithReadings } from '@/utils/kanjiKanaForms'
  import { computed, ref } from 'vue'
  import { DEFAULT_LANGUAGE, languageMatches } from '@/utils/language'
  import { collectAllKanaForms, getStandaloneKanaForms, getUsedKanaTexts, groupKanjiWithReadings } from '@/utils/kanjiKanaForms'
  import LanguageSelector from './LanguageSelector.vue'
  import { useResponsiveTooltip } from '@/composables/useResponsiveTooltip'

  const props = defineProps<{
    properNoun: ProperNounSummary
  }>()

  const router = useRouter()
  const selectedLanguage = ref<string>(DEFAULT_LANGUAGE)

  const { isMobile } = useResponsiveTooltip()

  // Extract available languages from all translations
  const availableLanguages = computed(() => {
    const languages = new Set<string>()
    if (props.properNoun.translations) for (const translation of props.properNoun.translations) {
      if (translation.translations) for (const textItem of translation.translations) {
        if (textItem.language) {
          languages.add(textItem.language)
        }
      }
    }
    return Array.from(languages)
  })

  function onLanguageChanged (language: string) {
    selectedLanguage.value = language
  }

  // Filter translations to show only those in selected language
  const filteredTranslations = computed(() => {
    if (!props.properNoun.translations) return []

    return props.properNoun.translations
      .map(translation => {
        const textInLanguage = translation.translations?.find(t => languageMatches(t.language, selectedLanguage.value))
        return textInLanguage
          ? {
            text: textInLanguage.text,
            types: translation.types,
          }
          : null
      })
      .filter((t): t is { text: string, types: any[] } => t !== null)
  })

  // Tags helpers
  const primaryKanaTags = computed(() => {
    return props.properNoun.primaryKana?.tags || []
  })

  const primaryKanjiTags = computed(() => {
    return props.properNoun.primaryKanji?.tags || []
  })

  // Has any tags (kanji or kana)
  const hasTags = computed(() => {
    return primaryKanjiTags.value.length > 0 || primaryKanaTags.value.length > 0
  })

  const primaryText = computed(() => {
    return props.properNoun.primaryKanji?.text || props.properNoun.primaryKana?.text || ''
  })

  const primaryReading = computed(() => {
    return props.properNoun.primaryKanji ? props.properNoun.primaryKana?.text : null
  })

  const hasOtherForms = computed(() => {
    return (props.properNoun.otherKanjiForms && props.properNoun.otherKanjiForms.length > 0)
      || (props.properNoun.otherKanaForms && props.properNoun.otherKanaForms.length > 0)
  })

  // Get all kana forms for matching (using shared utility)
  const allKanaForms = computed(() => collectAllKanaForms(
    props.properNoun.primaryKana,
    props.properNoun.otherKanaForms,
  ))

  // Track which kana forms have been used (matched to a kanji)
  const usedKanaTexts = computed(() => getUsedKanaTexts(
    props.properNoun.primaryKanji,
    props.properNoun.primaryKana,
    props.properNoun.otherKanjiForms,
    allKanaForms.value,
  ))

  // Group other kanji forms with their applicable kana readings
  const otherKanjiWithReadings = computed<KanjiWithReadings<KanjiForm, KanaForm>[]>(() =>
    groupKanjiWithReadings(props.properNoun.otherKanjiForms, allKanaForms.value),
  )

  // Standalone kana forms - those not matched to any kanji
  const standaloneKanaForms = computed<KanaForm[]>(() => getStandaloneKanaForms(
    props.properNoun.primaryKanji,
    props.properNoun.otherKanjiForms,
    props.properNoun.otherKanaForms,
    usedKanaTexts.value,
  ))

  function handleCardClick () {
    const routePath = props.properNoun.slug
    router.push(`/proper-noun/${routePath}`)
  }
</script>

<style lang="scss" scoped>
.proper-noun-primary {
    font-size: 1.5rem;
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
            font-size: 0.7rem;
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

    .proper-noun-text {
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

.content-grid {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.metadata-col {
    .section-label {
        font-size: 0.7rem;
        font-weight: 600;
        color: rgba(var(--v-theme-on-surface), 0.6);
        text-transform: uppercase;
        display: inline-block;
        margin-right: 0.5rem;
    }

    .other-forms {
        display: flex;
        align-items: baseline;
        flex-wrap: wrap;

        .forms-list {
            display: inline-flex;
            flex-wrap: wrap;
        }

        .form-item {
            font-size: 0.95rem;
            color: rgba(var(--v-theme-on-surface), 0.87);

            .reading-text {
                color: rgba(var(--v-theme-on-surface), 0.6);
                font-size: 0.85rem;
            }
        }
    }
}

.translations-section {
    .translation-item {
        margin-bottom: 0.5rem;

        &:last-child {
            margin-bottom: 0;
        }
    }

    .translation-header {
        display: flex;
        gap: 0.5rem;
        align-items: flex-start;
    }

    .translation-number {
        font-weight: 600;
        color: rgba(var(--v-theme-on-surface), 0.38);
        font-size: 0.85rem;
        min-width: 1.2rem;
        margin-top: 0.1rem;
    }

    .translation-content {
        flex: 1;
    }

    .translation-text {
        font-size: 1rem;
        line-height: 1.4;
        color: rgba(var(--v-theme-on-surface), 0.87);
    }

    .translation-types {
        display: flex;
        flex-wrap: wrap;
        gap: 4px;
    }

    .no-translations {
        font-size: 0.9rem;
        color: rgba(var(--v-theme-on-surface), 0.6);
        font-style: italic;
        display: flex;
        align-items: center;
    }
}

.interactive-card {
    cursor: pointer;
    transition: box-shadow 0.2s ease, transform 0.2s ease, border-color 0.2s ease;
    border-color: rgba(var(--v-border-color), var(--v-border-opacity));

    &:hover {
        transform: translateY(-2px);
        border-color: rgba(var(--v-theme-primary), 0.5);
    }
}
</style>
