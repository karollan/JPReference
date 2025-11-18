<template>
    <v-hover v-slot="{ isHovering, props: hoverProps }">
        <v-card
            v-bind="hoverProps"
            class="pa-3 mb-3 interactive-card text-left"
            outlined
            :elevation="isHovering ? 8 : 2"
            v-ripple
            @click="handleCardClick"
        >
            <div class="d-flex justify-space-between align-start mb-2">
                <div class="kanji-literal-container">
                    <div class="kanji-literal">{{ kanji.literal }}</div>
                    <div class="stroke-count">{{ kanji.strokeCount }} strokes</div>
                </div>
                <LanguageSelector
                    v-if="availableLanguages.length > 0"
                    :available-languages="availableLanguages"
                    :default-language="selectedLanguage"
                    @language-changed="onLanguageChanged"
                />
            </div>

            <!-- Metadata Chips -->
            <div class="metadata-chips mb-3">
                <v-chip
                    v-if="kanji.jlptLevel"
                    size="x-small"
                    color="primary"
                    variant="flat"
                    class="mr-1 mb-1"
                >
                    N{{ kanji.jlptLevel }}
                </v-chip>
                <v-chip
                    v-if="kanji.grade"
                    size="x-small"
                    color="secondary"
                    variant="flat"
                    class="mr-1 mb-1"
                >
                    Grade {{ kanji.grade }}
                </v-chip>
                <v-chip
                    v-if="kanji.frequency"
                    size="x-small"
                    color="info"
                    variant="tonal"
                    class="mr-1 mb-1"
                >
                    Freq: {{ kanji.frequency }}
                </v-chip>
            </div>

            <!-- Info Grid -->
            <div class="info-grid">
                <template v-if="kanji.kunyomiReadings && kanji.kunyomiReadings.length > 0">
                    <div class="info-label">Kun</div>
                    <div class="info-content">
                        <span
                            v-for="(reading, idx) in kanji.kunyomiReadings"
                            :key="idx"
                            class="reading-item"
                        >
                            {{ reading.value }}{{ idx < kanji.kunyomiReadings!.length - 1 ? '、' : '' }}
                        </span>
                    </div>
                </template>

                <template v-if="kanji.onyomiReadings && kanji.onyomiReadings.length > 0">
                    <div class="info-label">On</div>
                    <div class="info-content">
                        <span
                            v-for="(reading, idx) in kanji.onyomiReadings"
                            :key="idx"
                            class="reading-item"
                        >
                            {{ reading.value }}{{ idx < kanji.onyomiReadings!.length - 1 ? '、' : '' }}
                        </span>
                    </div>
                </template>

                <template v-if="filteredMeanings.length > 0">
                    <div class="info-label">Meaning</div>
                    <div class="info-content meanings-text">
                        {{ filteredMeanings.join(', ') }}
                    </div>
                </template>

                <template v-if="kanji.radicals && kanji.radicals.length > 0">
                    <div class="info-label">Radical</div>
                    <div class="info-content radicals-content">
                        <v-chip
                            v-for="radical in kanji.radicals"
                            :key="radical.id"
                            size="x-small"
                            variant="outlined"
                            class="mr-1 mb-1"
                        >
                            {{ radical.literal }}
                        </v-chip>
                    </div>
                </template>
            </div>
        </v-card>
    </v-hover>
</template>

<script lang="ts" setup>
import type { KanjiSummary } from '@/types/Kanji'
import LanguageSelector from './LanguageSelector.vue'
import { ref, computed } from 'vue'
import { DEFAULT_LANGUAGE, languageMatches } from '@/utils/language'

const props = defineProps<{
    kanji: KanjiSummary
}>()

const selectedLanguage = ref<string>(DEFAULT_LANGUAGE)

// Extract available languages from meanings (handle both 2-letter and 3-letter codes)
const availableLanguages = computed(() => {
    const languages = new Set<string>()
    props.kanji.meanings?.forEach(meaning => {
        if (meaning.language) {
            languages.add(meaning.language)
        }
    })
    return Array.from(languages)
})

const onLanguageChanged = (language: string) => {
    selectedLanguage.value = language
}

// Filter meanings based on selected language
const filteredMeanings = computed(() => {
    if (!props.kanji.meanings) return []
    
    const preferredMeanings = props.kanji.meanings.filter(m => languageMatches(m.language, selectedLanguage.value))
    if (preferredMeanings.length > 0) {
        return preferredMeanings.map(m => m.meaning)
    }

    const fallbackMeanings = props.kanji.meanings.filter(m => languageMatches(m.language, DEFAULT_LANGUAGE))
    if (fallbackMeanings.length > 0) {
        return fallbackMeanings.map(m => m.meaning)
    }

    return props.kanji.meanings.map(m => m.meaning)
})

const handleCardClick = () => {
    console.log('[KanjiSummary] Card clicked', props.kanji.id || props.kanji.literal)
}
</script>

<style lang="scss" scoped>
.kanji-literal-container {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
}

.kanji-literal {
    font-size: 3rem;
    font-weight: 500;
    line-height: 1;
    color: rgba(var(--v-theme-on-surface), 0.87);
}

.stroke-count {
    font-size: 0.75rem;
    color: rgba(var(--v-theme-on-surface), 0.6);
    margin-top: 0.1rem;
}

.metadata-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
}

.info-grid {
    display: grid;
    grid-template-columns: min-content 1fr;
    column-gap: 12px;
    row-gap: 6px;
    align-items: baseline;
}

.info-label {
    font-size: 0.75rem;
    font-weight: 600;
    color: rgba(var(--v-theme-on-surface), 0.6);
    text-transform: uppercase;
    letter-spacing: 0.05em;
    text-align: right;
    white-space: nowrap;
    align-self: baseline;
    padding-top: 2px; 
}

.info-content {
    font-size: 0.95rem;
    line-height: 1.4;
    color: rgba(var(--v-theme-on-surface), 0.87);
}

.meanings-text {
    font-weight: 500;
}

.radicals-content {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
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