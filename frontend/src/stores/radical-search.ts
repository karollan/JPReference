import { defineStore } from 'pinia'
import RadicalService from '@/services/radical.service'
import type { RadicalSummary, KanjiSimple, RadicalSearchResult } from '@/types/Radical'

export const useRadicalSearchStore = defineStore('radical-search', () => {
    // State
    const radicalsList = ref<RadicalSummary[]>([])
    const selectedRadicalIds = ref<string[]>([])
    const kanjiResults = ref<KanjiSimple[]>([])
    const compatibleRadicalIds = ref<Set<string>>(new Set())

    const kanjiCache = reactive<Map<string, RadicalSearchResult>>(new Map()) // Key: sorted comma-separated IDs

    const loading = ref<boolean>(false)
    const error = ref<string | null>(null)

    // Actions
    const getRadicalsList = async () => {
        if (radicalsList.value.length > 0) return

        loading.value = true
        error.value = null

        try {
            const response = await RadicalService.getRadicalsList()
            radicalsList.value = response
        } catch (error_: any) {
            error.value = error_.message
        } finally {
            loading.value = false
        }
    }

    const toggleRadical = (radicalId: string) => {
        const index = selectedRadicalIds.value.indexOf(radicalId)
        if (index === -1) {
            selectedRadicalIds.value.push(radicalId)
        } else {
            selectedRadicalIds.value.splice(index, 1)
        }
        searchKanji()
    }

    const searchKanji = async () => {
        if (selectedRadicalIds.value.length === 0) {
            kanjiResults.value = []
            compatibleRadicalIds.value.clear()
            return
        }

        const ids = [...selectedRadicalIds.value].sort()
        const cacheKey = ids.join(',')

        if (kanjiCache.has(cacheKey)) {
            const cached = kanjiCache.get(cacheKey)!
            kanjiResults.value = cached.results
            compatibleRadicalIds.value = new Set(cached.compatibleRadicalIds)
            return
        }

        loading.value = true
        error.value = null

        try {
            const response = await RadicalService.searchKanjiByRadicals(ids)

            // Cache hygiene
            if (Object.keys(kanjiCache).length > 10) {
                const firstKey = kanjiCache.keys().next().value;
                if (firstKey !== undefined) {
                    kanjiCache.delete(firstKey);
                }
            }

            kanjiCache.set(cacheKey, response)
            kanjiResults.value = response.results
            compatibleRadicalIds.value = new Set(response.compatibleRadicalIds)
        } catch (error_: any) {
            error.value = error_.message
        } finally {
            loading.value = false
        }
    }

    const clearSelection = () => {
        selectedRadicalIds.value = []
        kanjiResults.value = []
        compatibleRadicalIds.value.clear()
    }

    // Helper to check compatibility
    // If no radicals selected, ALL are compatible (or none are disabled). 
    // If radicals selected, only those in compatibleRadicalIds are enabled.
    // BUT we should always allow clicking currently selected ones to deselect them.
    const isRadicalCompatible = (radicalId: string) => {
        if (selectedRadicalIds.value.length === 0) return true
        return compatibleRadicalIds.value.has(radicalId) || selectedRadicalIds.value.includes(radicalId)
    }

    // Getters
    const radicalsOrdered = computed(() => {
        return Object.entries(
            radicalsList.value.reduce<Record<number, RadicalSummary[]>>((acc, radical) => {
                const strokeCount = radical.strokeCount;
                if (!strokeCount) return acc;

                (acc[strokeCount] ??= []).push(radical);
                return acc;
            }, {})
        ).map(([strokeCount, radicals]) => ({
            strokeCount: Number(strokeCount),
            radicals,
        })).sort((a, b) => a.strokeCount - b.strokeCount)
    })

    const kanjiResultsOrdered = computed(() => {
        return Object.entries(
            kanjiResults.value.reduce<Record<number, KanjiSimple[]>>((acc, kanji) => {
                const strokeCount = kanji.strokeCount;
                if (!strokeCount) return acc;

                (acc[strokeCount] ??= []).push(kanji);
                return acc;
            }, {})
        ).map(([strokeCount, kanjis]) => ({
            strokeCount: Number(strokeCount),
            kanjis,
        })).sort((a, b) => a.strokeCount - b.strokeCount)
    })

    return {
        radicalsList,
        selectedRadicalIds,
        kanjiResults,
        compatibleRadicalIds,
        loading,
        error,
        getRadicalsList,
        toggleRadical,
        clearSelection,
        isRadicalCompatible,
        radicalsOrdered,
        kanjiResultsOrdered
    }
}, { persist: true })