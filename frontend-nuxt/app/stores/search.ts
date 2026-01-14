import { defineStore } from 'pinia'
import type { GlobalSearchResponse } from '@/types/GlobalSearch'

export type ViewMode = 'unified' | 'tabbed'
export type ActiveTab = 'vocabulary' | 'kanji' | 'properNouns'

export interface TabScrollPositions {
  vocabulary: number
  kanji: number
  properNouns: number
}

export const useSearchStore = defineStore('search', () => {
  // UI State
  const viewMode = ref<ViewMode>('unified')
  const activeTab = ref<ActiveTab>('vocabulary')
  const currentPage = ref<number>(1)
  const pageSize = ref<number>(50)

  // Cached results for tabbed view navigation persistence
  const cachedResults = ref<GlobalSearchResponse | null>(null)
  const cachedQuery = ref<string>('')
  const tabScrollPositions = ref<TabScrollPositions>({
    vocabulary: 0,
    kanji: 0,
    properNouns: 0
  })

  // Actions
  const reset = () => {
    currentPage.value = 1
  }

  const clearResults = () => {
    currentPage.value = 1
  }

  const setViewMode = (mode: ViewMode, tab?: ActiveTab) => {
    viewMode.value = mode
    if (tab) {
      activeTab.value = tab
    }
  }

  const setActiveTab = (tab: ActiveTab) => {
    activeTab.value = tab
  }

  const incrementPage = () => {
    currentPage.value++
  }

  const resetPage = () => {
    currentPage.value = 1
  }

  const setCachedResults = (results: GlobalSearchResponse | null, query: string) => {
    cachedResults.value = results
    cachedQuery.value = query
  }

  const setTabScrollPosition = (tab: ActiveTab, position: number) => {
    tabScrollPositions.value[tab] = position
  }

  const clearCache = () => {
    cachedResults.value = null
    cachedQuery.value = ''
    tabScrollPositions.value = {
      vocabulary: 0,
      kanji: 0,
      properNouns: 0
    }
  }

  return {
    viewMode,
    activeTab,
    currentPage,
    pageSize,
    cachedResults,
    cachedQuery,
    tabScrollPositions,
    setViewMode,
    setActiveTab,
    incrementPage,
    resetPage,
    reset,
    clearResults,
    setCachedResults,
    setTabScrollPosition,
    clearCache,
  }
})
