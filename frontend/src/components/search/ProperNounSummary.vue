<template>
  <v-hover v-slot="{ isHovering, props: hoverProps }">
    <v-card
      v-ripple
      v-bind="hoverProps"
      class="pa-3 mb-3 interactive-card text-left"
      :elevation="isHovering ? 8 : 2"
      outlined
      @click="handleCardClick"
    >
      <div class="d-flex justify-space-between align-start mb-2">
        <div class="proper-noun-primary flex-grow-1">
          <ruby v-if="properNoun.primaryKanji">
            {{ properNoun.primaryKanji.text }}
            <rt v-if="properNoun.primaryKana">{{ properNoun.primaryKana.text }}</rt>
          </ruby>
          <span v-else class="proper-noun-text">
            {{ properNoun.primaryKana?.text }}
          </span>
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
        <div v-if="hasOtherForms || allTags.length > 0" class="metadata-col">
          <!-- Tags -->
          <div v-if="allTags.length > 0" class="tags-section mb-2">
            <v-chip
              v-for="(tag, idx) in allTags.slice(0, 5)"
              :key="idx"
              class="mr-1 mb-1"
              size="x-small"
              variant="outlined"
            >
              {{ tag.description }}
            </v-chip>
          </div>

          <!-- Other Forms -->
          <div v-if="hasOtherForms" class="other-forms">
            <div class="section-label">Also:</div>
            <div class="forms-list">
              <span v-for="(form, idx) in properNoun.otherKanjiForms" :key="`kanji-${idx}`" class="form-item mr-2">
                <ruby v-if="getKanaForKanji(form.text)">
                  {{ form.text }}
                  <rt>{{ getKanaForKanji(form.text) }}</rt>
                </ruby>
                <span v-else>{{ form.text }}</span>
              </span>
              <span v-for="(form, idx) in properNoun.otherKanaForms" :key="`kana-${idx}`" class="form-item mr-2">
                {{ form.text }}
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
  import type { ProperNounSummary } from '@/types/ProperNoun'
  import { computed, ref } from 'vue'
  import { DEFAULT_LANGUAGE, languageMatches } from '@/utils/language'
  import LanguageSelector from './LanguageSelector.vue'

  const props = defineProps<{
    properNoun: ProperNounSummary
  }>()

  const router = useRouter()
  const selectedLanguage = ref<string>(DEFAULT_LANGUAGE)

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

  const hasOtherForms = computed(() => {
    return (props.properNoun.otherKanjiForms && props.properNoun.otherKanjiForms.length > 0)
      || (props.properNoun.otherKanaForms && props.properNoun.otherKanaForms.length > 0)
  })

  function getKanaForKanji (kanjiText: string): string | null {
    // Find matching kana form
    const kanaForms = [
      props.properNoun.primaryKana,
      ...(props.properNoun.otherKanaForms || []),
    ]

    for (const kana of kanaForms) {
      if (kana && kana.appliesToKanji && kana.appliesToKanji.includes(kanjiText)) {
        return kana.text
      }
    }
    return null
  }

  const allTags = computed(() => {
    const tags = [
      ...props.properNoun.primaryKanji?.tags || [],
      ...props.properNoun.primaryKana?.tags || [],
    ]
    // Remove duplicates based on code
    const uniqueTags = tags.filter((tag, index, self) =>
      index === self.findIndex(t => t.code === tag.code),
    )
    return uniqueTags
  })

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
    color: rgba(var(--v-theme-on-surface), 0.87);

    ruby {
        rt {
            font-size: 0.7rem;
            color: rgba(var(--v-theme-on-surface), 0.6);
        }
    }

    .proper-noun-text {
        display: block;
    }
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

            ruby {
                rt {
                    font-size: 0.6rem;
                    color: rgba(var(--v-theme-on-surface), 0.6);
                }
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
