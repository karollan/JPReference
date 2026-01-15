<template>
  <v-container
    class="text-center main-container-layout"
    fluid
    :max-width="$vuetify.display.mdAndUp ? 1200 : '100%'"
  >
    <v-row
      class="content d-flex flex-row"
      justify="center"
      style="align-content: flex-start; min-height: 0;"
    >
      <v-col cols="12" class="d-flex flex-column h-100" style="min-height: 0;">
        <!-- Search Bar -->
        <div class="w-100 flex-shrink-0 mb-4">
        <div class="d-flex align-center">
          <SearchAutocomplete
            v-model:search-query="searchQuery"
            :show-radical-search="true"
            @clear="searchQuery = ''"
            @search="handleEnterSearch"
          />

          <v-tooltip location="bottom" :persistent="false">
            <template #activator="{ props }">
              <v-btn
                v-bind="props"
                :aria-label="searchStore.viewMode === 'unified' ? 'Switch to tab view' : 'Switch to quick view'"
                class="ml-2"
                color="primary"
                icon
                variant="text"
                @click="toggleViewMode"
              >
                <v-icon size="28">{{ searchStore.viewMode === 'unified' ? 'mdi-tab' : 'mdi-view-dashboard' }}</v-icon>
              </v-btn>
            </template>
            <span>{{ searchStore.viewMode === 'unified' ? 'Switch to tab view' : 'Switch to quick view' }}</span>
          </v-tooltip>
        </div>
        <div
          v-if="searchedTerms.length > 0"
          class="d-flex justify-left mt-1"
        >
          <span class="text-body-2 text-grey">
            Searched for: {{ searchedTerms.join(', ') }}
            <span v-if="searchedTerms.length > 1">. You can also search for</span>
            <span v-if="searchedTerms.length > 1">
              <v-hover
                v-for="(term, index) in searchedTerms"
                :key="term"
              >
                <template #default="{ isHovering, props }">
                  <span
                    v-bind="props"
                    class="text-primary cursor-pointer"
                    :class="{ 'text-secondary': isHovering }"
                    @click="replaceTermBySuggestion(term)"
                  >
                    "{{ term }}"
                  </span>
                  <span v-if="isLastTerm(index)">,</span>
                </template>
              </v-hover>
            </span>
          </span>
        </div>

        <v-col
          v-if="error"
          class="pa-0 mt-4"
          cols="12"
        >
          <v-alert
            class="mb-4"
            closable
            type="error"
          >
            {{ error?.message || 'Search error occurred' }}
          </v-alert>
        </v-col>
        </div>

        <div
          v-if="searchStore.viewMode === 'unified'"
          class="results-container w-100 flex-grow-1 d-flex flex-column"
          style="min-height: 0"
        >
          <v-row class="flex-grow-1" style="min-height: 0;">
          <!-- Vocabulary Column -->
          <v-col
            class="d-flex flex-column h-100"
            style="min-height: 0;"
            cols="12"
            md="8"
          >
            <div class="d-flex justify-space-between align-center mb-2 flex-shrink-0">
              <div class="text-left section-title">
                <span>Vocabulary</span>
              </div>
              <v-btn
                v-if="!pending && vocabularyCount > 5"
                color="primary"
                size="small"
                variant="text"
                @click="showAllResults('vocabulary')"
              >
                See All {{ vocabularyCount }} Results
              </v-btn>
            </div>
            <v-divider class="mb-4 mt-2 flex-shrink-0" />
            <div class="vocabulary-iterator overflow-y-auto flex-grow-1 pa-2" style="min-height: auto; max-height: 100%;">
              <template v-if="pending">
                <VocabularySummarySkeleton v-for="i in 3" :key="i" />
              </template>
              <template v-else-if="limitedVocabulary.length > 0">
                <VocabularySummary
                  v-for="vocabulary in limitedVocabulary"
                  :key="vocabulary.id"
                  :vocabulary="vocabulary"
                />
              </template>
              <template v-else>
                <v-card class="pa-8 text-center justify-center align-center d-flex flex-column" variant="outlined">
                  <v-icon color="grey-lighten-1" size="64">mdi-text-search</v-icon>
                  <div class="text-h6 mt-4 text-grey">No vocabulary found</div>
                </v-card>
              </template>
            </div>
          </v-col>

          <!-- Kanji & Proper Nouns Column -->
          <v-col
            class="d-flex flex-column h-100"
            style="min-height: 0;"
            cols="12"
            md="4"
          >
            <!-- Kanji Section (60%) -->
            <div class="d-flex flex-column flex-grow-1 flex-md-grow-0 section-kanji" :class="{'h-auto': !$vuetify.display.mdAndUp}" style="min-height: auto; max-height: 55%;">
              <div class="d-flex justify-space-between align-center mb-2 flex-shrink-0">
                <div class="text-left section-title">
                  <span>Kanji</span>
                </div>
                <v-btn
                  v-if="!pending && kanjiCount > 5"
                  color="primary"
                  size="small"
                  variant="text"
                  @click="showAllResults('kanji')"
                >
                  See All {{ kanjiCount }}
                </v-btn>
              </div>
              <v-divider class="mb-4 mt-2 flex-shrink-0" />
              <div class="kanji-iterator overflow-y-auto mb-md-6 mb-4 flex-grow-1 pa-2">
                <template v-if="pending">
                  <KanjiSummarySkeleton v-for="i in 2" :key="i" />
                </template>
                <template v-else-if="limitedKanji.length > 0">
                  <KanjiSummary
                    v-for="kanji in limitedKanji"
                    :key="kanji.id"
                    :kanji="kanji"
                  />
                </template>
                <template v-else>
                  <v-card class="pa-6 text-center h-100 justify-center align-center d-flex flex-column" variant="outlined">
                    <v-icon color="grey-lighten-1" size="48">mdi-ideogram-cjk</v-icon>
                    <div class="text-body-2 mt-2 text-grey">No kanji found</div>
                  </v-card>
                </template>
              </div>
            </div>

            <!-- Proper Nouns Section (40%) -->
            <div class="d-flex flex-column flex-grow-1 flex-md-grow-0 section-proper" :class="{'h-auto': !$vuetify.display.mdAndUp}" style="min-height: auto; max-height: 45%;">
              <div class="d-flex justify-space-between align-center mb-2 mt-md-0 mt-4 flex-shrink-0">
                <div class="text-left section-title">
                  <span>Proper Nouns</span>
                </div>
                <v-btn
                  v-if="!pending && properNounCount > 5"
                  color="primary"
                  size="small"
                  variant="text"
                  @click="showAllResults('properNouns')"
                >
                  See All {{ properNounCount }}
                </v-btn>
              </div>
              <v-divider class="mb-4 mt-2 flex-shrink-0" />
              <div class="proper-noun-iterator overflow-y-auto flex-grow-1 pa-2">
                <template v-if="pending">
                  <ProperNounSummarySkeleton v-for="i in 2" :key="i" />
                </template>
                <template v-else-if="limitedProperNouns.length > 0">
                  <ProperNounSummary
                    v-for="properNoun in limitedProperNouns"
                    :key="properNoun.id"
                    :proper-noun="properNoun"
                  />
                </template>
                <template v-else>
                  <v-card class="pa-6 text-center h-100 justify-center align-center d-flex flex-column" variant="outlined">
                    <v-icon color="grey-lighten-1" size="48">mdi-account</v-icon>
                    <div class="text-body-2 mt-2 text-grey">No proper nouns found</div>
                  </v-card>
                </template>
              </div>
            </div>
          </v-col>
        </v-row>
        </div>

      <!-- Tabbed View -->
        <div
          v-else
          class="results-container w-100 flex-grow-1 d-flex flex-column"
          style="min-height: 0"
        >
        <div class="tab-header mb-4 flex-shrink-0">
          <v-tabs
            v-model="currentTab"
            class="flex-grow-1 tab-nav"
            color="primary"
            show-arrows
          >
            <v-tab value="vocabulary">
              Vocabulary
              <v-chip
                v-if="vocabularyCount > 0"
                class="ml-2"
                color="primary"
                size="small"
                variant="flat"
              >
                {{ vocabularyCount }}
              </v-chip>
            </v-tab>
            <v-tab value="kanji">
              Kanji
              <v-chip
                v-if="kanjiCount > 0"
                class="ml-2"
                color="primary"
                size="small"
                variant="flat"
              >
                {{ kanjiCount }}
              </v-chip>
            </v-tab>
            <v-tab value="properNouns">
              Proper Nouns
              <v-chip
                v-if="properNounCount > 0"
                class="ml-2"
                color="primary"
                size="small"
                variant="flat"
              >
                {{ properNounCount }}
              </v-chip>
            </v-tab>
          </v-tabs>
        </div>

        <v-tabs-window ref="tabsWindowRef" v-model="currentTab" class="flex-grow-1 overflow-y-auto d-flex flex-column">
          <!-- Vocabulary Tab -->
          <v-tabs-window-item value="vocabulary">
            <div class="tab-content h-100">
              <template v-if="pending">
                <VocabularySummarySkeleton v-for="i in 5" :key="i" />
              </template>
              <template v-else-if="vocabularyList?.data && vocabularyList.data.length > 0">
                <VocabularySummary
                  v-for="vocabulary in vocabularyList.data"
                  :key="vocabulary.id"
                  :vocabulary="vocabulary"
                />
                <div v-if="vocabularyList.pagination.hasNext" class="text-center mt-4">
                  <v-btn
                    color="primary"
                    :loading="loadingMore"
                    variant="outlined"
                    @click="loadMore('vocabulary')"
                  >
                    Load More
                  </v-btn>
                </div>
              </template>
              <template v-else>
                <v-card class="pa-12 text-center h-100 justify-center align-center d-flex flex-column" variant="outlined">
                  <v-icon color="grey-lighten-1" size="96">mdi-text-search</v-icon>
                  <div class="text-h5 mt-4 text-grey">No vocabulary found</div>
                </v-card>
              </template>
            </div>
          </v-tabs-window-item>

          <!-- Kanji Tab -->
          <v-tabs-window-item value="kanji">
            <div class="tab-content h-100">
              <template v-if="pending">
                <KanjiSummarySkeleton v-for="i in 5" :key="i" />
              </template>
              <template v-else-if="kanjiList?.data && kanjiList.data.length > 0">
                <KanjiSummary
                  v-for="kanji in kanjiList.data"
                  :key="kanji.id"
                  :kanji="kanji"
                />
                <div v-if="kanjiList.pagination.hasNext" class="text-center mt-4">
                  <v-btn
                    color="primary"
                    :loading="loadingMore"
                    variant="outlined"
                    @click="loadMore('kanji')"
                  >
                    Load More
                  </v-btn>
                </div>
              </template>
              <template v-else>
                <v-card class="pa-12 text-center h-100 justify-center align-center d-flex flex-column" variant="outlined">
                  <v-icon color="grey-lighten-1" size="96">mdi-ideogram-cjk</v-icon>
                  <div class="text-h5 mt-4 text-grey">No kanji found</div>
                </v-card>
              </template>
            </div>
          </v-tabs-window-item>

          <!-- Proper Nouns Tab -->
          <v-tabs-window-item value="properNouns">
            <div class="tab-content h-100">
              <template v-if="pending">
                <ProperNounSummarySkeleton v-for="i in 5" :key="i" />
              </template>
              <template v-else-if="properNounList?.data && properNounList.data.length > 0">
                <ProperNounSummary
                  v-for="properNoun in properNounList.data"
                  :key="properNoun.id"
                  :proper-noun="properNoun"
                />
                <div v-if="properNounList.pagination.hasNext" class="text-center mt-4">
                  <v-btn
                    color="primary"
                    :loading="loadingMore"
                    variant="outlined"
                    @click="loadMore('properNouns')"
                  >
                    Load More
                  </v-btn>
                </div>
              </template>
              <template v-else>
                <v-card class="pa-12 text-center h-100 justify-center align-center d-flex flex-column" variant="outlined">
                  <v-icon color="grey-lighten-1" size="96">mdi-account</v-icon>
                  <div class="text-h5 mt-4 text-grey">No proper nouns found</div>
                </v-card>
              </template>
            </div>
          </v-tabs-window-item>
        </v-tabs-window>
        </div>
      </v-col>
    </v-row>
  </v-container>
</template>

<script lang="ts" setup>
  import type { ActiveTab } from '@/stores/search'
  import type { GlobalSearchResponse } from '@/types/GlobalSearch'
  import { computed, ref, watch, onMounted, onBeforeUnmount, nextTick } from 'vue'
  import KanjiSummary from '@/components/search/KanjiSummary.vue'
  import KanjiSummarySkeleton from '@/components/search/KanjiSummarySkeleton.vue'
  import ProperNounSummary from '@/components/search/ProperNounSummary.vue'
  import ProperNounSummarySkeleton from '@/components/search/ProperNounSummarySkeleton.vue'
  import VocabularySummary from '@/components/search/VocabularySummary.vue'
  import VocabularySummarySkeleton from '@/components/search/VocabularySummarySkeleton.vue'
  import { useSearchStore } from '@/stores/search'
  import { useSearchService, fetchWithError } from '~/services'

  const route = useRoute()
  const router = useRouter()
  const searchStore = useSearchStore()
  const service = useSearchService()

  const pageSize = 50
  const searchQuery = ref<string>(route.query.query as string || '')
  const currentTab = ref<ActiveTab>('vocabulary')
  const loadingMore = ref(false)

  // Template ref for tabs window scroll container
  const tabsWindowRef = ref<HTMLElement | null>(null)

  // Accumulated results for load more functionality
  // Initialize from cached results if same query (for back navigation)
  const accumulatedResults = ref<GlobalSearchResponse | null>(
    searchStore.cachedQuery === searchQuery.value.trim().toLowerCase() 
      ? searchStore.cachedResults 
      : null
  )

  // Unique key for useAsyncData based on search query
  const searchKey = computed(() => `search-${searchQuery.value.trim().toLowerCase()}`)

  function shouldSkipSearch(originalQuery: string): boolean {
    // Check if query ends with a potential filter being typed
    // Pattern: ends with # alone, or ends with #word (no trailing space)
    // This means user is in the middle of typing a filter
    const endsWithUncommittedFilter = /(^|\s)#[^\s]*$/.test(originalQuery)
    return endsWithUncommittedFilter
  }

  const { data: searchResults, pending, error, refresh } = await useAsyncData<GlobalSearchResponse | null>(
    searchKey.value,
    async () => {
      const originalQuery = searchQuery.value
      const query = originalQuery.trim()
      if (!query) return null
      
      // Check for uncommitted filters using original query (before trim)
      // This allows trailing space to signal filter commitment
      if (shouldSkipSearch(originalQuery)) return null
      
      return await fetchWithError(() => service.fetchGlobalSearch(query, 1, pageSize))
    },
    {
      server: true
    }
  )

  // Track if we initialized from cache (to skip overwriting with fresh data)
  const initializedFromCache = Boolean(
    searchStore.cachedQuery === searchQuery.value.trim().toLowerCase() && 
    searchStore.cachedResults
  )
  let skipNextSearchResultsSync = initializedFromCache

  // Sync accumulated results with fresh search results
  watch(searchResults, (newResults) => {
    // Skip syncing if we have cached data from navigation back
    if (skipNextSearchResultsSync) {
      skipNextSearchResultsSync = false
      return
    }
    
    if (newResults) {
      accumulatedResults.value = newResults
      searchStore.resetPage()
      // Update cache
      searchStore.setCachedResults(newResults, searchQuery.value.trim().toLowerCase())
    }
  }, { immediate: true })

  // Computed values from accumulated results
  const vocabularyList = computed(() => accumulatedResults.value?.vocabularyResults)
  const kanjiList = computed(() => accumulatedResults.value?.kanjiResults)
  const properNounList = computed(() => accumulatedResults.value?.properNounResults)
  const searchedTerms = computed(() => accumulatedResults.value?.searchedTerms || [])

  // Result counts
  const vocabularyCount = computed(() => vocabularyList.value?.pagination.totalCount || 0)
  const kanjiCount = computed(() => kanjiList.value?.pagination.totalCount || 0)
  const properNounCount = computed(() => properNounList.value?.pagination.totalCount || 0)

  // Limited results for unified view (5 items each)
  const limitedVocabulary = computed(() => vocabularyList.value?.data?.slice(0, 5) || [])
  const limitedKanji = computed(() => kanjiList.value?.data?.slice(0, 5) || [])
  const limitedProperNouns = computed(() => properNounList.value?.data?.slice(0, 5) || [])

  function isLastTerm (index: number) : boolean {
    return index < searchedTerms.value.length - 1
  }

  // Simple debounce function
  function debounce (func: Function, delay: number) {
    let timeoutId: ReturnType<typeof setTimeout>
    return (...args: any[]) => {
      clearTimeout(timeoutId)
      timeoutId = setTimeout(() => func(...args), delay)
    }
  }

  // Track last searched query to avoid redundant searches for whitespace-only changes
  let lastSearchedQuery = ''

  // Debounced refresh for typing
  const debouncedRefresh = debounce(async () => {
    const originalQuery = searchQuery.value
    const query = originalQuery.trim()
    
    if (!query) {
      accumulatedResults.value = null
      lastSearchedQuery = ''
      return
    }
    
    // Skip if only whitespace changed (trimmed query is same as last search)
    if (query === lastSearchedQuery) return
    
    // Check for uncommitted filters using original query
    if (shouldSkipSearch(originalQuery)) return
    
    lastSearchedQuery = query
    await refresh()
  }, 500)

  async function handleEnterSearch () {
    // Immediate search on Enter, no debounce
    const query = searchQuery.value.trim()
    if (!query) {
      accumulatedResults.value = null
      return
    }
    await refresh()
  }

  async function loadMore (category: ActiveTab) {
    if (loadingMore.value || !searchQuery.value.trim()) return
    
    loadingMore.value = true
    
    try {
      const nextPage = searchStore.currentPage + 1
      const response = await service.fetchGlobalSearch(searchQuery.value, nextPage, pageSize)
      
      if (!response || !accumulatedResults.value) {
        return
      }
      
      // Append results based on category
      if (category === 'vocabulary' && accumulatedResults.value.vocabularyResults) {
        accumulatedResults.value = {
          ...accumulatedResults.value,
          vocabularyResults: {
            data: [...accumulatedResults.value.vocabularyResults.data, ...response.vocabularyResults.data],
            pagination: response.vocabularyResults.pagination,
          }
        }
      } else if (category === 'kanji' && accumulatedResults.value.kanjiResults) {
        accumulatedResults.value = {
          ...accumulatedResults.value,
          kanjiResults: {
            data: [...accumulatedResults.value.kanjiResults.data, ...response.kanjiResults.data],
            pagination: response.kanjiResults.pagination,
          }
        }
      } else if (category === 'properNouns' && accumulatedResults.value.properNounResults) {
        accumulatedResults.value = {
          ...accumulatedResults.value,
          properNounResults: {
            data: [...accumulatedResults.value.properNounResults.data, ...response.properNounResults.data],
            pagination: response.properNounResults.pagination,
          }
        }
      }
      
      searchStore.incrementPage()
      // Update cache with new accumulated results
      if (accumulatedResults.value) {
        searchStore.setCachedResults(accumulatedResults.value, searchQuery.value.trim().toLowerCase())
      }
    } catch (err) {
      console.error('Load more error:', err)
    } finally {
      loadingMore.value = false
    }
  }

  // Scroll position tracking for tabs window
  function onTabScroll(event: Event) {
    const target = event.target as HTMLElement
    if (target) {
      searchStore.setTabScrollPosition(currentTab.value, target.scrollTop)
    }
  }

  // Setup scroll listener on the actual DOM element
  function setupScrollListener() {
    const tabsWindow = tabsWindowRef.value
    const scrollContainer = (tabsWindow as any)?.$el || tabsWindow
    if (scrollContainer && scrollContainer.addEventListener) {
      scrollContainer.addEventListener('scroll', onTabScroll)
    }
  }

  // Restore scroll position when tab content is ready
  function restoreScrollPosition() {
    const position = searchStore.tabScrollPositions[currentTab.value]
    if (position <= 0) return

    nextTick(() => {
      const tabsWindow = tabsWindowRef.value
      // v-tabs-window exposes the element via $el
      const scrollContainer = (tabsWindow as any)?.$el || tabsWindow
      if (scrollContainer) {
        scrollContainer.scrollTop = position
      }
    })
  }

  // Save current scroll position (call before navigation)
  function saveScrollPosition() {
    const tabsWindow = tabsWindowRef.value
    const scrollContainer = (tabsWindow as any)?.$el || tabsWindow
    if (scrollContainer) {
      searchStore.setTabScrollPosition(currentTab.value, scrollContainer.scrollTop)
    }
  }

  function updateUrl () {
    const query: any = {}

    if (searchQuery.value) {
      query.query = searchQuery.value
    }

    if (searchStore.viewMode === 'tabbed') {
      query.view = 'tabbed'
      query.tab = searchStore.activeTab
    }

    router.replace({ query })
  }

  function showAllResults (tab: ActiveTab) {
    searchStore.setViewMode('tabbed', tab)
    currentTab.value = tab
    // Setup scroll listener after switching to tabbed view
    nextTick(() => {
      setupScrollListener()
    })
    updateUrl()
  }

  function toggleViewMode () {
    if (searchStore.viewMode === 'unified') {
      searchStore.setViewMode('tabbed', currentTab.value)
      // Setup scroll listener after switching to tabbed view
      nextTick(() => {
        setupScrollListener()
      })
    } else {
      searchStore.setViewMode('unified')
    }
    updateUrl()
  }

  function replaceTermBySuggestion (term: string) {
    searchQuery.value = `"${term}"`
  }

  // Initialize view mode from URL (runs on both server and client)
  if (route.query.view === 'tabbed') {
    searchStore.setViewMode('tabbed')
    if (route.query.tab) {
      const tab = route.query.tab as ActiveTab
      searchStore.setActiveTab(tab)
      currentTab.value = tab
    }
  }

  // Watch for URL query changes (navigation)
  watch(() => route.query.query, async (newQuery) => {
    const queryStr = (newQuery as string) || ''
    if (queryStr !== searchQuery.value) {
      searchQuery.value = queryStr
      if (queryStr) {
        await refresh()
      } else {
        accumulatedResults.value = null
      }
    }
  })

  // Watch for changes and update URL + trigger search
  watch(() => searchStore.viewMode, () => {
    updateUrl()
  })

  watch(currentTab, newTab => {
    searchStore.setActiveTab(newTab)
    updateUrl()
  })

  watch(searchQuery, newQuery => {
    searchStore.reset()
    searchStore.setViewMode('unified')
    // Clear cache when query changes
    searchStore.clearCache()
    updateUrl()

    if (!newQuery.trim()) {
      accumulatedResults.value = null
      return
    }

    debouncedRefresh()
  })

  // Restore scroll position on mount for tabbed view
  onMounted(() => {
    if (searchStore.viewMode === 'tabbed') {
      // Setup scroll listener
      setupScrollListener()
      
      // Restore scroll position if we have cached results
      if (accumulatedResults.value) {
        setTimeout(() => {
          restoreScrollPosition()
        }, 100)
      }
    }
  })

  // Save scroll position before unmounting
  onBeforeUnmount(() => {
    if (searchStore.viewMode === 'tabbed') {
      saveScrollPosition()
    }
  })

  // Restore scroll position when switching tabs
  watch(currentTab, () => {
    if (searchStore.viewMode === 'tabbed') {
      restoreScrollPosition()
    }
  })

  useHead({
    title: computed(() => searchQuery.value ? `Search: ${searchQuery.value} - JP Reference` : 'Search - JP Reference'),
    meta: [
      {
        name: 'description',
        content: computed(() => searchQuery.value ? `Search results for "${searchQuery.value}" in Japanese dictionary.` : 'Search for Japanese vocabulary, kanji, and proper nouns.')
      }
    ]
  })
</script>
<style lang="scss" scoped>
.content {
  height: auto;
  padding: 1rem;
}

@media (min-width: 960px) {
  .content, .main-container-layout {
    height: calc(100vh - var(--v-layout-top) - var(--v-layout-bottom));
  }
  
  .section-kanji {
    flex: 6 0 0;
  }

  .section-proper {
    flex: 4 0 0;
  }
}

.section-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: rgba(var(--v-theme-on-surface), 0.87);
}

.results-container {
  width: 100%;
  min-height: 0;
}

.tab-content {
  padding: 0.5rem 0;
}

.tab-header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
}

.tabbed-back-btn {
  white-space: nowrap;
}

// Ensure smooth scrolling
.vocabulary-iterator,
.kanji-iterator,
.proper-noun-iterator,
.tab-content {
  scrollbar-width: thin;
  scrollbar-color: rgba(var(--v-theme-on-surface), 0.3) transparent;
  padding: 12px;

  &::-webkit-scrollbar {
    width: 6px;
    height: 6px;
  }

  &::-webkit-scrollbar-thumb {
    background: rgba(var(--v-theme-on-surface), 0.3);
    border-radius: 3px;
  }

  &::-webkit-scrollbar-thumb:hover {
    background: rgba(var(--v-theme-on-surface), 0.5);
  }
}
</style>
