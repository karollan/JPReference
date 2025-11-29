<template>
  <v-hover v-slot="{ isHovering, props: hoverProps }">
    <v-card
      v-ripple
      v-bind="hoverProps"
      class="pa-3 mb-3 interactive-card"
      :elevation="isHovering ? 8 : 2"
      outlined
      @click="handleCardClick"
    >
      <div class="d-flex justify-space-between align-start mb-2">
        <div class="vocabulary-primary flex-grow-1">
          <ruby v-if="vocabulary.primaryKanji">
            {{ vocabulary.primaryKanji.text }}
            <rt>{{ vocabulary.primaryKana?.text }}</rt>
          </ruby>
          <span v-else class="vocabulary-text">
            {{ vocabulary.primaryKana?.text }}
          </span>
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

            <!-- Other Forms -->
            <div v-if="hasOtherForms" class="other-forms mt-3">
              <div class="section-label">Other Forms</div>
              <div class="forms-list">
                <div v-for="(form, idx) in vocabulary.otherKanjiForms" :key="`kanji-${idx}`" class="form-item">
                  <ruby v-if="getKanaForKanji(form.text)">
                    {{ form.text }}
                    <rt>{{ getKanaForKanji(form.text) }}</rt>
                  </ruby>
                  <span v-else>{{ form.text }}</span>
                  <v-chip
                    v-if="form.isCommon"
                    class="ml-1"
                    color="success"
                    size="x-small"
                    variant="tonal"
                  >
                    Common
                  </v-chip>
                </div>
                <div v-for="(form, idx) in vocabulary.otherKanaForms" :key="`kana-${idx}`" class="form-item">
                  {{ form.text }}
                  <v-chip
                    v-if="form.isCommon"
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

            <!-- Tags -->
            <div v-if="tags.length > 0" class="tags-section mt-3">
              <div class="section-label">Tags</div>
              <div class="tags-list">
                <v-chip
                  v-for="(tag, idx) in tags.slice(0, 5)"
                  :key="idx"
                  class="mr-1 mb-1"
                  size="x-small"
                  variant="outlined"
                >
                  {{ tag.description }}
                </v-chip>
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
  import type { VocabularySummary } from '@/types/Vocabulary'
  import { computed, ref } from 'vue'
  import { DEFAULT_LANGUAGE, languageMatches } from '@/utils/language'
  import LanguageSelector from './LanguageSelector.vue'

  const props = defineProps<{
    vocabulary: VocabularySummary
  }>()

  const selectedLanguage = ref<string>(DEFAULT_LANGUAGE)

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

  const hasOtherForms = computed(() => {
    return (props.vocabulary.otherKanjiForms && props.vocabulary.otherKanjiForms.length > 0)
      || (props.vocabulary.otherKanaForms && props.vocabulary.otherKanaForms.length > 0)
  })

  function getKanaForKanji (kanjiText: string): string | null {
    // Find matching kana form
    const kanaForms = [
      props.vocabulary.primaryKana,
      ...(props.vocabulary.otherKanaForms || []),
    ]

    for (const kana of kanaForms) {
      if (kana && kana.appliesToKanji && kana.appliesToKanji.includes(kanjiText)) {
        return kana.text
      }
    }
    return null
  }

  const tags = computed(() => {
    const allTags = [
      ...props.vocabulary.primaryKanji?.tags || [],
      ...props.vocabulary.primaryKana?.tags || [],
    ]
    // Remove duplicates based on code
    const uniqueTags = allTags.filter((tag, index, self) =>
      index === self.findIndex(t => t.code === tag.code),
    )
    return uniqueTags
  })

  function handleCardClick () {
    console.log('[VocabularySummary] Card clicked', props.vocabulary.id)
  }
</script>
<style lang="scss" scoped>
.vocabulary-primary {
    font-size: 1.75rem;
    font-weight: 500;
    text-align: left;
    line-height: 1.2;
    color: rgba(var(--v-theme-on-surface), 0.87);

    ruby {
        rt {
            font-size: 0.8rem;
            color: rgba(var(--v-theme-on-surface), 0.6);
        }
    }

    .vocabulary-text {
        display: block;
    }
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

        .other-forms {
            display: flex;
            flex-direction: column;
            gap: 0.5rem;
        }

        .form-item {
            display: flex;
            align-items: center;
            font-size: 1rem;

            ruby {
                rt {
                    font-size: 0.6rem;
                    color: rgba(var(--v-theme-on-surface), 0.6);
                }
            }
        }
    }

    .tags-section {
        .tags-list {
            display: flex;
            flex-wrap: wrap;
            gap: 4px;
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
