import { SearchService } from '@/services/search.service';
import { defineStore } from 'pinia'

import type { GlobalSearchCache } from '@/types/GlobalSearch';
import type { KanjiResponse } from '@/types/Kanji';
import type { VocabularyResponse } from '@/types/Vocabulary';
import type { ProperNounResponse } from '@/types/ProperNoun';

export const useSearchStore = defineStore('search', () => {
    //State
    const searchCache = reactive<GlobalSearchCache>({})
    const kanjiList = shallowRef<KanjiResponse>();
    const vocabularyList = shallowRef<VocabularyResponse>();
    const properNounList = shallowRef<ProperNounResponse>();

    const loading = ref<boolean>(false);
    const loadingMore = ref<boolean>(false);
    const error = ref<string | null>(null);

    //Actions
    const reset = () => {
        error.value = null;
    }

    const performSearch = async(query: string, pageSize: number = 50) => {
        loading.value = true;
        error.value = null;
        loadingMore.value = false;
        kanjiList.value = undefined;
        vocabularyList.value = undefined;
        properNounList.value = undefined;

        try {
            const response = await SearchService.fetchGlobalSearch(query, 1, pageSize);

            // Validate response structure
            console.log(response);
            if (!response || !response.vocabularyResults || !response.properNounResults || !response.kanjiResults) {
                error.value = 'Invalid response structure from SearchService';
                throw new Error('Invalid response structure from SearchService');
            }

            // Cache hygiene
            if (Object.keys(searchCache).length > 10) {
                delete searchCache[Object.keys(searchCache)[0]!];
            }

            // Cache key
            const key = query.trim().toLowerCase() + `_${pageSize}`;
            
            // Cache response 
            searchCache[key] = response;
            kanjiList.value = response.kanjiResults;
            vocabularyList.value = response.vocabularyResults;
            properNounList.value = response.properNounResults;
        } catch (err: any) {
            error.value = `Search error: ${err.message}` || 'An error occurred during search.';
        } finally {
            loading.value = false;
        }
    }

    //Getters


    return {
        searchCache,
        performSearch,
        loading,
        loadingMore,
        error,
        kanjiList,
        vocabularyList,
        properNounList,
        reset
    }
});