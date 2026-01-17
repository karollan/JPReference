import { defineStore } from 'pinia'
import type { RadicalSummary, KanjiSimple, RadicalSearchResult } from '@/types/Radical'

export const useRadicalSearchStore = defineStore('radical-search', () => {
    // UI State
    const selectedRadicalIds = ref<string[]>([])

    // Actions
    const toggleRadical = (radicalId: string) => {
        const index = selectedRadicalIds.value.indexOf(radicalId)
        if (index === -1) {
            selectedRadicalIds.value.push(radicalId)
        } else {
            selectedRadicalIds.value.splice(index, 1)
        }
    }

    const clearSelection = () => {
        selectedRadicalIds.value = []
    }

    // Helper to check compatibility
    // If no radicals selected, ALL are compatible (or none are disabled). 
    // If radicals selected, only those in compatibleRadicalIds are enabled.
    // BUT we should always allow clicking currently selected ones to deselect them.
    const isRadicalCompatible = (radicalId: string, compatibleRadicalIds: Set<string>) => {
        if (selectedRadicalIds.value.length === 0) return true
        return compatibleRadicalIds.has(radicalId) || selectedRadicalIds.value.includes(radicalId)
    }

    // Computed key for useAsyncData caching
    const selectedIdsKey = computed(() =>
        [...selectedRadicalIds.value].sort().join(',')
    )

    // Helpers to organize data by stroke count (used in template)
    const getRadicalsOrdered = (radicalsList: RadicalSummary[]) => {
        return Object.entries(
            radicalsList.reduce<Record<number, RadicalSummary[]>>((acc, radical) => {
                const strokeCount = radical.strokeCount;
                if (!strokeCount) return acc;

                (acc[strokeCount] ??= []).push(radical);
                return acc;
            }, {})
        ).map(([strokeCount, radicals]) => ({
            strokeCount: Number(strokeCount),
            radicals,
        })).sort((a, b) => a.strokeCount - b.strokeCount)
    }

    const getKanjiResultsOrdered = (kanjiResults: KanjiSimple[]) => {
        return Object.entries(
            kanjiResults.reduce<Record<number, KanjiSimple[]>>((acc, kanji) => {
                const strokeCount = kanji.strokeCount;
                if (!strokeCount) return acc;

                (acc[strokeCount] ??= []).push(kanji);
                return acc;
            }, {})
        ).map(([strokeCount, kanjis]) => ({
            strokeCount: Number(strokeCount),
            kanjis,
        })).sort((a, b) => a.strokeCount - b.strokeCount)
    }

    return {
        selectedRadicalIds,
        selectedIdsKey,
        toggleRadical,
        clearSelection,
        isRadicalCompatible,
        getRadicalsOrdered,
        getKanjiResultsOrdered
    }
})