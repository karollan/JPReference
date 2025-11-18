import { SearchService } from '@/services/search.service';
import { defineStore } from 'pinia'

import type { GlobalSearchCache } from '@/types/GlobalSearch';
import type { KanjiResponse } from '@/types/Kanji';
import type { VocabularyResponse } from '@/types/Vocabulary';
import type { ProperNounResponse } from '@/types/ProperNoun';

export type ViewMode = 'unified' | 'tabbed'
export type ActiveTab = 'vocabulary' | 'kanji' | 'properNouns'

export const useSearchStore = defineStore('search', () => {
    //State
    const searchCache = reactive<GlobalSearchCache>({})
    const kanjiList = shallowRef<KanjiResponse>();
    const vocabularyList = shallowRef<VocabularyResponse>();
    const properNounList = shallowRef<ProperNounResponse>();

    const loading = ref<boolean>(false);
    const loadingMore = ref<boolean>(false);
    const error = ref<string | null>(null);

    const viewMode = ref<ViewMode>('unified');
    const activeTab = ref<ActiveTab>('vocabulary');
    const currentQuery = ref<string>('');
    const currentPage = ref<number>(1);
    const pageSize = ref<number>(50);

    //Actions
    const reset = () => {
        error.value = null;
    }

    const clearResults = () => {
        kanjiList.value = undefined;
        vocabularyList.value = undefined;
        properNounList.value = undefined;
        currentQuery.value = '';
        currentPage.value = 1;
        loading.value = false;
        loadingMore.value = false;
    }

    const performSearch = async (query: string, pageSizeOverride?: number) => {
        loading.value = true;
        error.value = null;
        loadingMore.value = false;
        kanjiList.value = undefined;
        vocabularyList.value = undefined;
        properNounList.value = undefined;
        currentQuery.value = query;
        currentPage.value = 1;

        if (pageSizeOverride) {
            pageSize.value = pageSizeOverride;
        }

        try {
            const response = await SearchService.fetchGlobalSearch(query, 1, pageSize.value);

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
            const key = query.trim().toLowerCase() + `_${pageSize.value}`;

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

    const loadMoreResults = async (category: ActiveTab) => {
        if (loadingMore.value || !currentQuery.value) return;

        loadingMore.value = true;
        error.value = null;

        try {
            const nextPage = currentPage.value + 1;
            const response = await SearchService.fetchGlobalSearch(currentQuery.value, nextPage, pageSize.value);

            if (!response || !response.vocabularyResults || !response.properNounResults || !response.kanjiResults) {
                error.value = 'Invalid response structure from SearchService';
                throw new Error('Invalid response structure from SearchService');
            }

            // Append results based on category
            if (category === 'vocabulary' && vocabularyList.value) {
                vocabularyList.value = {
                    data: [...vocabularyList.value.data, ...response.vocabularyResults.data],
                    pagination: response.vocabularyResults.pagination
                };
            } else if (category === 'kanji' && kanjiList.value) {
                kanjiList.value = {
                    data: [...kanjiList.value.data, ...response.kanjiResults.data],
                    pagination: response.kanjiResults.pagination
                };
            } else if (category === 'properNouns' && properNounList.value) {
                properNounList.value = {
                    data: [...properNounList.value.data, ...response.properNounResults.data],
                    pagination: response.properNounResults.pagination
                };
            }

            currentPage.value = nextPage;
        } catch (err: any) {
            error.value = `Load more error: ${err.message}` || 'An error occurred while loading more results.';
        } finally {
            loadingMore.value = false;
        }
    }

    const setViewMode = (mode: ViewMode, tab?: ActiveTab) => {
        viewMode.value = mode;
        if (tab) {
            activeTab.value = tab;
        }
    }

    const setActiveTab = (tab: ActiveTab) => {
        activeTab.value = tab;
    }

    //Getters


    return {
        searchCache,
        performSearch,
        loadMoreResults,
        setViewMode,
        setActiveTab,
        loading,
        loadingMore,
        error,
        kanjiList,
        vocabularyList,
        properNounList,
        viewMode,
        activeTab,
        currentQuery,
        currentPage,
        pageSize,
        reset,
        clearResults
    }
});