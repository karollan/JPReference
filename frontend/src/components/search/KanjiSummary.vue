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
            <div class="d-flex justify-space-between align-start mb-3">
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
                size="small"
                color="primary"
                variant="flat"
                class="mr-1 mb-1"
            >
                JLPT N{{ kanji.jlptLevel }}
            </v-chip>
            <v-chip
                v-if="kanji.grade"
                size="small"
                color="secondary"
                variant="flat"
                class="mr-1 mb-1"
            >
                Grade {{ kanji.grade }}
            </v-chip>
            <v-chip
                v-if="kanji.frequency"
                size="small"
                color="info"
                variant="tonal"
                class="mr-1 mb-1"
            >
                Frequency: {{ kanji.frequency }}
            </v-chip>
        </div>

        <!-- Readings -->
        <div v-if="kanji.kunyomiReadings && kanji.kunyomiReadings.length > 0" class="readings-section mb-2">
            <div class="readings-label">Kun:</div>
            <div class="readings-content">
                <span
                    v-for="(reading, idx) in kanji.kunyomiReadings"
                    :key="idx"
                    class="reading-item"
                >
                    {{ reading.value }}{{ idx < kanji.kunyomiReadings!.length - 1 ? '、' : '' }}
                </span>
            </div>
        </div>

        <div v-if="kanji.onyomiReadings && kanji.onyomiReadings.length > 0" class="readings-section mb-3">
            <div class="readings-label">On:</div>
            <div class="readings-content">
                <span
                    v-for="(reading, idx) in kanji.onyomiReadings"
                    :key="idx"
                    class="reading-item"
                >
                    {{ reading.value }}{{ idx < kanji.onyomiReadings!.length - 1 ? '、' : '' }}
                </span>
            </div>
        </div>

        <!-- Meanings -->
        <div v-if="filteredMeanings.length > 0" class="meanings-section mb-3">
            <div class="meanings-label">Meanings:</div>
            <div class="meanings-content">
                {{ filteredMeanings.join(', ') }}
            </div>
        </div>

        <!-- Radicals -->
        <div v-if="kanji.radicals && kanji.radicals.length > 0" class="radicals-section">
            <div class="radicals-label">Radicals:</div>
            <div class="radicals-content">
                <v-chip
                    v-for="radical in kanji.radicals"
                    :key="radical.id"
                    size="small"
                    variant="outlined"
                    class="mr-1 mb-1"
                >
                    {{ radical.literal }}
                </v-chip>
            </div>
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
    font-size: 4rem;
    font-weight: 500;
    line-height: 1;
    color: #333;
}

.stroke-count {
    font-size: 0.85rem;
    color: #666;
    margin-top: 0.25rem;
}

.metadata-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
}

.readings-section {
    display: flex;
    align-items: baseline;
    gap: 0.5rem;
}

.readings-label {
    font-size: 0.85rem;
    font-weight: 600;
    color: #666;
    min-width: 40px;
}

.readings-content {
    font-size: 1.1rem;
    line-height: 1.5;
}

.reading-item {
    color: #333;
}

.meanings-section {
    display: flex;
    align-items: flex-start;
    gap: 0.5rem;
}

.meanings-label {
    font-size: 0.85rem;
    font-weight: 600;
    color: #666;
    min-width: 80px;
    margin-top: 0.1rem;
}

.meanings-content {
    font-size: 1rem;
    line-height: 1.5;
    flex: 1;
}

.radicals-section {
    display: flex;
    align-items: flex-start;
    gap: 0.5rem;
}

.radicals-label {
    font-size: 0.85rem;
    font-weight: 600;
    color: #666;
    min-width: 80px;
    margin-top: 0.1rem;
}

.radicals-content {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
    flex: 1;
}

.interactive-card {
    cursor: pointer;
    transition: box-shadow 0.2s ease, transform 0.2s ease;

    &:hover {
        transform: translateY(-2px);
    }
}
</style>