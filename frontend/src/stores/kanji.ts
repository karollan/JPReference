import type { KanjiListCache, KanjiSummary } from '@/types/Kanji'
// Utilities
import { defineStore } from 'pinia'
import { KanjiService } from '@/services/kanji.service'

export const useKanjiStore = defineStore('kanji', () => {
  // State
  const kanjiListCache = reactive<KanjiListCache>({})
  const kanjiList = shallowRef<KanjiSummary[]>([])
  const loading = ref<boolean>(false)
  const loadingMore = ref<boolean>(false)
  const error = ref<string | null>(null)
  const currentFilters = reactive<{
    jlptLevel: number[]
    search: string | null
  }>({ jlptLevel: [], search: null })

  // Utils
  const flattenPages = (cache: KanjiListCache, key: string): KanjiSummary[] => {
    if (!cache[key]?.pages) {
      return []
    }

    // Get all page numbers and sort them numerically
    const pageNumbers = Object.keys(cache[key].pages)
      .map(Number)
      .toSorted((a: number, b: number) => a - b)

    // Flatten pages into a single array
    return pageNumbers.reduce<KanjiSummary[]>((acc: KanjiSummary[], pageNum: number) => {
      return acc.concat(cache[key]?.pages[pageNum] ?? [])
    }, [])
  }

  const getLargestPageNumber = (cache: KanjiListCache, key: string): number | undefined => {
    if (!cache[key]?.pages) {
      return undefined
    }

    const pageNumbers = Object.keys(cache[key].pages).map(Number)
    if (pageNumbers.length === 0) {
      return undefined
    }

    return Math.max(...pageNumbers)
  }

  const currentCacheKey = (pageSize = 50) => {
    return JSON.stringify({ search: currentFilters.search, jlptLevel: currentFilters.jlptLevel, pageSize })
  }

  // Actions
  const fetchKanji = async (
    jlptLevel: number[],
    search: string | null,
    pageSize = 50,
    _page = 1,
  ) => {
    currentFilters.jlptLevel = jlptLevel.toSorted((a: number, b: number) => a - b)
    currentFilters.search = search

    const key = currentCacheKey(pageSize)
    const cacheItem = kanjiListCache[key]
    if (cacheItem) {
      kanjiList.value = flattenPages(kanjiListCache, key)
      return
    }
    loading.value = true
    error.value = null
    loadingMore.value = false

    try {
      const response = await KanjiService.fetchKanjis(search, 1, pageSize)

      // Validate response structure
      if (!response || !Array.isArray(response.data)) {
        throw new Error('Invalid response structure from KanjiService')
      }

      // Cache hygiene
      if (Object.keys(kanjiListCache).length > 10) {
        delete kanjiListCache[Object.keys(kanjiListCache)[0]!]
      }

      // Cache the response
      kanjiListCache[key] ??= { pages: {}, totalCount: response.pagination.totalCount, hasMorePages: true }
      kanjiListCache[key].pages[response.pagination.page] = response.data
      kanjiList.value = flattenPages(kanjiListCache, key)
      kanjiListCache[key].hasMorePages = kanjiList.value.length < kanjiListCache[key].totalCount
    } catch (error_: any) {
      console.error('KanjiStore error:', error_)
      error.value = error_.message ?? 'Failed to fetch kanjis'
      kanjiList.value = []
    } finally {
      loading.value = false
    }
  }

  const fetchNextPage = async (pageSize = 50) => {
    const key = currentCacheKey(pageSize)
    if (kanjiListCache[key] && (loadingMore.value || !kanjiListCache[key].hasMorePages || Object.keys(kanjiListCache[key].pages).length === 0)) {
      return
    }

    const largestPageNumber = getLargestPageNumber(kanjiListCache, key)
    const nextPage = largestPageNumber ? largestPageNumber + 1 : 1

    loadingMore.value = true
    error.value = null

    try {
      const response = await KanjiService.fetchKanjis(
        currentFilters.search,
        nextPage,
        pageSize,
      )

      // Validate response structure
      if (!response || !Array.isArray(response.data)) {
        throw new Error('Invalid response structure from KanjiService')
      }

      kanjiListCache[key]!.pages[nextPage] = response.data
      kanjiList.value = flattenPages(kanjiListCache, key)
      kanjiListCache[key]!.hasMorePages = kanjiList.value.length < kanjiListCache[key]!.totalCount
    } catch (error_: any) {
      console.error('KanjiStore fetchNextPage error:', error_)
      error.value = error_.message ?? 'Failed to fetch more kanjis'
    } finally {
      loadingMore.value = false
    }
  }

  const getKanjiById = async (guid: string) => {
    const existing = kanjiList.value.find(k => k.id === guid)
    if (existing) {
      return existing
    }
    try {
      const kanji = await KanjiService.fetchKanji(guid)
      kanjiList.value.push(kanji)
      return kanji ?? null
    } catch (error_: any) {
      error.value = error_.message ?? 'Failed to fetch kanji'
      return null
    }
  }

  // Getters
  const hasMorePages = computed(() => {
    // Use the current filters to get the right cache
    const key = currentCacheKey(50)
    const cache = kanjiListCache[key] ?? { hasMorePages: false, totalCount: 0 }
    return cache.hasMorePages
  })

  const totalCount = computed(() => {
    // Use the current filters to get the right cache
    const key = currentCacheKey(50)
    const cache = kanjiListCache[key] ?? { totalCount: 0 }
    return cache.totalCount
  })

  return {
    // Expose state, actions, getters here
    kanjiList,
    error,
    loading,
    loadingMore,
    hasMorePages,
    totalCount,
    getKanjiById,
    fetchKanji,
    fetchNextPage,
  }
})
