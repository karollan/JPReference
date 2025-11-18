<template>
    <v-hover v-slot="{ isHovering, props: hoverProps }">
        <v-card
            v-bind="hoverProps"
            class="pa-4 mb-4 interactive-card"
            outlined
            :elevation="isHovering ? 8 : 2"
            v-ripple
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

            <v-row>
                <v-col cols="12" md="4">
                    <div class="metadata-section">
                        <!-- Other Forms -->
                        <div v-if="hasOtherForms" class="other-forms">
                            <div class="section-label">Other Forms</div>
                            <div class="forms-list">
                                <div v-for="(form, idx) in properNoun.otherKanjiForms" :key="`kanji-${idx}`" class="form-item">
                                    <ruby v-if="getKanaForKanji(form.text)">
                                        {{ form.text }}
                                        <rt>{{ getKanaForKanji(form.text) }}</rt>
                                    </ruby>
                                    <span v-else>{{ form.text }}</span>
                                </div>
                                <div v-for="(form, idx) in properNoun.otherKanaForms" :key="`kana-${idx}`" class="form-item">
                                    {{ form.text }}
                                </div>
                            </div>
                        </div>

                        <!-- Tags -->
                        <div v-if="allTags.length > 0" class="tags-section" :class="{ 'mt-3': hasOtherForms }">
                            <div class="section-label">Tags</div>
                            <div class="tags-list">
                                <v-chip
                                    v-for="(tag, idx) in allTags.slice(0, 5)"
                                    :key="idx"
                                    size="x-small"
                                    variant="outlined"
                                    class="mr-1 mb-1"
                                >
                                    {{ tag.description }}
                                </v-chip>
                            </div>
                        </div>
                    </div>
                </v-col>

                <v-col cols="12" md="8" class="text-left">
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
                                            size="x-small"
                                            color="secondary"
                                            variant="tonal"
                                            class="mr-1"
                                        >
                                            {{ type.description }}
                                        </v-chip>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div v-if="filteredTranslations.length === 0" class="no-translations">
                            No translations available in selected language
                        </div>
                    </div>
                </v-col>
            </v-row>
        </v-card>
    </v-hover>
</template>

<script lang="ts" setup>
import type { ProperNounSummary } from '@/types/ProperNoun'
import LanguageSelector from './LanguageSelector.vue'
import { ref, computed } from 'vue'
import { DEFAULT_LANGUAGE, languageMatches } from '@/utils/language'

const props = defineProps<{
    properNoun: ProperNounSummary
}>()

const selectedLanguage = ref<string>(DEFAULT_LANGUAGE)

// Extract available languages from all translations
const availableLanguages = computed(() => {
    const languages = new Set<string>()
    props.properNoun.translations?.forEach(translation => {
        translation.translations?.forEach(textItem => {
            if (textItem.language) {
                languages.add(textItem.language)
            }
        })
    })
    return Array.from(languages)
})

const onLanguageChanged = (language: string) => {
    selectedLanguage.value = language
}

// Filter translations to show only those in selected language
const filteredTranslations = computed(() => {
    if (!props.properNoun.translations) return []
    
    return props.properNoun.translations
        .map(translation => {
            const textInLanguage = translation.translations?.find(t => languageMatches(t.language, selectedLanguage.value))
            return textInLanguage ? {
                text: textInLanguage.text,
                types: translation.types
            } : null
        })
        .filter((t): t is { text: string; types: any[] } => t !== null)
})

const hasOtherForms = computed(() => {
    return (props.properNoun.otherKanjiForms && props.properNoun.otherKanjiForms.length > 0) ||
           (props.properNoun.otherKanaForms && props.properNoun.otherKanaForms.length > 0)
})

const getKanaForKanji = (kanjiText: string): string | null => {
    // Find matching kana form
    const kanaForms = [
        props.properNoun.primaryKana,
        ...(props.properNoun.otherKanaForms || [])
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
        ...props.properNoun.primaryKana?.tags || []
    ]
    // Remove duplicates based on code
    const uniqueTags = tags.filter((tag, index, self) =>
        index === self.findIndex((t) => t.code === tag.code)
    )
    return uniqueTags
})

const handleCardClick = () => {
    console.log('[ProperNounSummary] Card clicked', props.properNoun.id)
}
</script>

<style lang="scss" scoped>
.proper-noun-primary {
    font-size: 2rem;
    font-weight: 500;
    text-align: left;
    line-height: 1.2;

    ruby {
        rt {
            font-size: 0.9rem;
            color: #666;
        }
    }

    .proper-noun-text {
        display: block;
    }
}

.metadata-section {
    .section-label {
        font-size: 0.75rem;
        font-weight: 600;
        color: #666;
        text-transform: uppercase;
        margin-bottom: 0.5rem;
    }

    .other-forms {
        .forms-list {
            display: flex;
            flex-direction: column;
            gap: 0.5rem;
        }

        .form-item {
            font-size: 1.1rem;

            ruby {
                rt {
                    font-size: 0.7rem;
                    color: #666;
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

.translations-section {
    .translation-item {
        margin-bottom: 1rem;

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
        color: #666;
        min-width: 1.5rem;
    }

    .translation-content {
        flex: 1;
    }

    .translation-text {
        font-size: 1rem;
        line-height: 1.5;
    }

    .translation-types {
        display: flex;
        flex-wrap: wrap;
        gap: 4px;
    }

    .no-translations {
        font-size: 0.9rem;
        color: #999;
        font-style: italic;
    }
}

.interactive-card {
    cursor: pointer;
    transition: box-shadow 0.2s ease, transform 0.2s ease;

    &:hover {
        transform: translateY(-2px);
    }
}
</style>