<template>
    <div class="language-selector" v-if="displayedLanguages.length > 0">
        <v-tooltip
            v-for="lang in displayedLanguages"
            :key="lang"
            location="bottom"
        >
            <template #activator="{ props: tooltipProps }">
                <v-btn
                    v-bind="tooltipProps"
                    :color="selectedLanguage === lang ? 'primary' : 'default'"
                    :variant="selectedLanguage === lang ? 'tonal' : 'text'"
                    size="x-small"
                    density="compact"
                    class="language-btn"
                    @click="selectLanguage(lang)"
                    v-ripple
                    :aria-label="getLanguageName(lang)"
                >
                    {{ getLanguageFlag(lang) }}
                </v-btn>
            </template>
            <span>{{ getLanguageName(lang) }}</span>
        </v-tooltip>
    </div>
</template>

<script lang="ts" setup>
import { computed, ref, watch } from 'vue'
import { DEFAULT_LANGUAGE, getLanguageFlag, getLanguageName, languageMatches } from '@/utils/language'

const props = defineProps<{
    availableLanguages: string[]
    defaultLanguage?: string
}>()

const emit = defineEmits<{
    'language-changed': [language: string]
}>()

const selectedLanguage = ref<string>(props.defaultLanguage || DEFAULT_LANGUAGE)

const displayedLanguages = computed(() => {
    const seen = new Set<string>()
    return (props.availableLanguages || []).filter((lang) => {
        if (!lang) {
            return false
        }
        const normalized = lang.toLowerCase()
        if (seen.has(normalized)) {
            return false
        }
        seen.add(normalized)
        return true
    })
})

const emitSelection = (language: string) => {
    selectedLanguage.value = language
    emit('language-changed', language)
}

const synchronizeLanguage = (languages: string[]) => {
    if (languages.length === 0) {
        return
    }

    const exactMatch = languages.find(lang => languageMatches(lang, selectedLanguage.value))
    if (exactMatch) {
        if (selectedLanguage.value !== exactMatch) {
            emitSelection(exactMatch)
        }
        return
    }

    const fallback = languages.find(lang => languageMatches(lang, DEFAULT_LANGUAGE)) || languages[0]
    if (fallback) {
        emitSelection(fallback)
    }
}

watch(() => props.availableLanguages, (newLangs) => {
    synchronizeLanguage(newLangs || [])
}, { immediate: true })

watch(() => props.defaultLanguage, (newDefault) => {
    if (!newDefault) {
        return
    }
    selectedLanguage.value = newDefault
    synchronizeLanguage(props.availableLanguages || [])
})

const selectLanguage = (lang: string) => {
    emitSelection(lang)
}
</script>

<style lang="scss" scoped>
.language-selector {
    display: flex;
    gap: 2px;
    align-items: center;
}

.language-btn {
    min-width: 32px !important;
    padding: 0 4px !important;
    font-size: 1.2rem;
}
</style>
