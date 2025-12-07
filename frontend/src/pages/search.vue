<template>
  <v-container
    class="text-center main-container-layout"
    fluid
    :max-width="$vuetify.display.mdAndUp ? 1200 : '100%'"
  >
    <v-row
      class="content d-flex flex-row"
      justify="center"
      style="align-content: flex-start;"
    >
      <!-- Search Bar -->
      <v-col
        ref="searchColumn"
        cols="12"
      >
        <div class="d-flex align-center">
          <v-text-field
            v-model="searchQuery"
            bg-color="white"
            class="home__search flex-grow-1"
            clearable
            density="comfortable"
            hide-details="auto"
            icon-color="#00000066"
            placeholder="Search"
            prepend-inner-icon="mdi-magnify"
            variant="outlined"
          />

          <v-tooltip location="bottom">
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
          v-if="searchStore.searchedTerms.length > 0"
          class="d-flex justify-left mt-1"
        >
          <span class="text-body-2 text-grey">Searched for: {{ searchStore.searchedTerms.join(', ') }}</span>
        </div>

        <v-col
          v-if="searchStore.error"
          class="pa-0 mt-4"
          cols="12"
        >
          <v-alert
            class="mb-4"
            closable
            type="error"
          >
            {{ searchStore.error }}
          </v-alert>
        </v-col>
      </v-col>

      <!-- Unified View -->
      <v-col
        v-if="searchStore.viewMode === 'unified'"
        class="results-container"
        cols="12"
      >
        <v-row>
          <!-- Vocabulary Column -->
          <v-col
            class="d-flex flex-column"
            cols="12"
            md="8"
          >
            <div class="d-flex justify-space-between align-center mb-2">
              <div class="text-left section-title">
                <span>Vocabulary</span>
              </div>
              <v-btn
                v-if="!searchStore.loading && vocabularyCount > 5"
                color="primary"
                size="small"
                variant="text"
                @click="showAllResults('vocabulary')"
              >
                See All {{ vocabularyCount }} Results
              </v-btn>
            </div>
            <v-divider class="mb-4 mt-2" />
            <div class="vocabulary-iterator overflow-y-auto" :style="{ maxHeight: resultColumnHeight }">
              <template v-if="searchStore.loading">
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
                <v-card class="pa-8 text-center" variant="outlined">
                  <v-icon color="grey-lighten-1" size="64">mdi-text-search</v-icon>
                  <div class="text-h6 mt-4 text-grey">No vocabulary found</div>
                </v-card>
              </template>
            </div>
          </v-col>

          <!-- Kanji & Proper Nouns Column -->
          <v-col
            class="d-flex flex-column"
            cols="12"
            md="4"
          >
            <!-- Kanji Section -->
            <div class="d-flex justify-space-between align-center mb-2">
              <div class="text-left section-title">
                <span>Kanji</span>
              </div>
              <v-btn
                v-if="!searchStore.loading && kanjiCount > 5"
                color="primary"
                size="small"
                variant="text"
                @click="showAllResults('kanji')"
              >
                See All {{ kanjiCount }}
              </v-btn>
            </div>
            <v-divider class="mb-4 mt-2" />
            <div class="kanji-iterator overflow-y-auto mb-6" :style="{ maxHeight: sideColumnHeight }">
              <template v-if="searchStore.loading">
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
                <v-card class="pa-6 text-center" variant="outlined">
                  <v-icon color="grey-lighten-1" size="48">mdi-ideogram-cjk</v-icon>
                  <div class="text-body-2 mt-2 text-grey">No kanji found</div>
                </v-card>
              </template>
            </div>

            <!-- Proper Nouns Section -->
            <div class="d-flex justify-space-between align-center mb-2 mt-4">
              <div class="text-left section-title">
                <span>Proper Nouns</span>
              </div>
              <v-btn
                v-if="!searchStore.loading && properNounCount > 5"
                color="primary"
                size="small"
                variant="text"
                @click="showAllResults('properNouns')"
              >
                See All {{ properNounCount }}
              </v-btn>
            </div>
            <v-divider class="mb-4 mt-2" />
            <div class="proper-noun-iterator overflow-y-auto" :style="{ maxHeight: sideColumnHeight }">
              <template v-if="searchStore.loading">
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
                <v-card class="pa-6 text-center" variant="outlined">
                  <v-icon color="grey-lighten-1" size="48">mdi-account</v-icon>
                  <div class="text-body-2 mt-2 text-grey">No proper nouns found</div>
                </v-card>
              </template>
            </div>
          </v-col>
        </v-row>
      </v-col>

      <!-- Tabbed View -->
      <v-col
        v-else
        class="results-container"
        cols="12"
      >
        <div class="tab-header mb-4">
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

        <v-tabs-window v-model="currentTab">
          <!-- Vocabulary Tab -->
          <v-tabs-window-item value="vocabulary">
            <div class="tab-content overflow-y-auto" :style="{ maxHeight: tabContentHeight }">
              <template v-if="searchStore.loading">
                <VocabularySummarySkeleton v-for="i in 5" :key="i" />
              </template>
              <template v-else-if="searchStore.vocabularyList?.data && searchStore.vocabularyList.data.length > 0">
                <VocabularySummary
                  v-for="vocabulary in searchStore.vocabularyList.data"
                  :key="vocabulary.id"
                  :vocabulary="vocabulary"
                />
                <div v-if="searchStore.vocabularyList.pagination.hasNext" class="text-center mt-4">
                  <v-btn
                    color="primary"
                    :loading="searchStore.loadingMore"
                    variant="outlined"
                    @click="loadMore('vocabulary')"
                  >
                    Load More
                  </v-btn>
                </div>
              </template>
              <template v-else>
                <v-card class="pa-12 text-center" variant="outlined">
                  <v-icon color="grey-lighten-1" size="96">mdi-text-search</v-icon>
                  <div class="text-h5 mt-4 text-grey">No vocabulary found</div>
                </v-card>
              </template>
            </div>
          </v-tabs-window-item>

          <!-- Kanji Tab -->
          <v-tabs-window-item value="kanji">
            <div class="tab-content overflow-y-auto" :style="{ maxHeight: tabContentHeight }">
              <template v-if="searchStore.loading">
                <KanjiSummarySkeleton v-for="i in 5" :key="i" />
              </template>
              <template v-else-if="searchStore.kanjiList?.data && searchStore.kanjiList.data.length > 0">
                <KanjiSummary
                  v-for="kanji in searchStore.kanjiList.data"
                  :key="kanji.id"
                  :kanji="kanji"
                />
                <div v-if="searchStore.kanjiList.pagination.hasNext" class="text-center mt-4">
                  <v-btn
                    color="primary"
                    :loading="searchStore.loadingMore"
                    variant="outlined"
                    @click="loadMore('kanji')"
                  >
                    Load More
                  </v-btn>
                </div>
              </template>
              <template v-else>
                <v-card class="pa-12 text-center" variant="outlined">
                  <v-icon color="grey-lighten-1" size="96">mdi-ideogram-cjk</v-icon>
                  <div class="text-h5 mt-4 text-grey">No kanji found</div>
                </v-card>
              </template>
            </div>
          </v-tabs-window-item>

          <!-- Proper Nouns Tab -->
          <v-tabs-window-item value="properNouns">
            <div class="tab-content overflow-y-auto" :style="{ maxHeight: tabContentHeight }">
              <template v-if="searchStore.loading">
                <ProperNounSummarySkeleton v-for="i in 5" :key="i" />
              </template>
              <template v-else-if="searchStore.properNounList?.data && searchStore.properNounList.data.length > 0">
                <ProperNounSummary
                  v-for="properNoun in searchStore.properNounList.data"
                  :key="properNoun.id"
                  :proper-noun="properNoun"
                />
                <div v-if="searchStore.properNounList.pagination.hasNext" class="text-center mt-4">
                  <v-btn
                    color="primary"
                    :loading="searchStore.loadingMore"
                    variant="outlined"
                    @click="loadMore('properNouns')"
                  >
                    Load More
                  </v-btn>
                </div>
              </template>
              <template v-else>
                <v-card class="pa-12 text-center" variant="outlined">
                  <v-icon color="grey-lighten-1" size="96">mdi-account</v-icon>
                  <div class="text-h5 mt-4 text-grey">No proper nouns found</div>
                </v-card>
              </template>
            </div>
          </v-tabs-window-item>
        </v-tabs-window>
      </v-col>
    </v-row>
  </v-container>
</template>

<script lang="ts" setup>
  import type { ActiveTab } from '@/stores/search'
  import { computed, onMounted, ref, watch } from 'vue'
  import KanjiSummary from '@/components/search/KanjiSummary.vue'
  import KanjiSummarySkeleton from '@/components/search/KanjiSummarySkeleton.vue'
  import ProperNounSummary from '@/components/search/ProperNounSummary.vue'
  import ProperNounSummarySkeleton from '@/components/search/ProperNounSummarySkeleton.vue'
  import VocabularySummary from '@/components/search/VocabularySummary.vue'
  import VocabularySummarySkeleton from '@/components/search/VocabularySummarySkeleton.vue'
  import { useSearchStore } from '@/stores/search'

  const route = useRoute()
  const router = useRouter()
  const searchStore = useSearchStore()

  const pageSize = 50
  const searchColumn = ref<any | null>(null)
  const searchQuery = ref<string>(route.query.query as string || '')
  const currentTab = ref<ActiveTab>('vocabulary')

  // Heights for different sections
  const resultColumnHeight = computed(() => {
    return searchColumn.value ? `${window.innerHeight - searchColumn.value.$el.getBoundingClientRect().bottom - 100}px` : '500px'
  })

  const sideColumnHeight = computed(() => {
    return searchColumn.value ? `${(window.innerHeight - searchColumn.value.$el.getBoundingClientRect().bottom - 100) / 2 - 60}px` : '250px'
  })

  const tabContentHeight = computed(() => {
    return searchColumn.value ? `${window.innerHeight - searchColumn.value.$el.getBoundingClientRect().bottom - 150}px` : '600px'
  })

  // Result counts
  const vocabularyCount = computed(() => searchStore.vocabularyList?.pagination.totalCount || 0)
  const kanjiCount = computed(() => searchStore.kanjiList?.pagination.totalCount || 0)
  const properNounCount = computed(() => searchStore.properNounList?.pagination.totalCount || 0)

  // Limited results for unified view (5 items each)
  const limitedVocabulary = computed(() => searchStore.vocabularyList?.data?.slice(0, 5) || [])
  const limitedKanji = computed(() => searchStore.kanjiList?.data?.slice(0, 5) || [])
  const limitedProperNouns = computed(() => searchStore.properNounList?.data?.slice(0, 5) || [])

  // Simple debounce function
  function debounce (func: Function, delay: number) {
    let timeoutId: number
    return (...args: any[]) => {
      clearTimeout(timeoutId)
      timeoutId = setTimeout(() => func(...args), delay)
    }
  }

  async function loadInitialData () {
    if (!searchQuery.value.trim()) {
      searchStore.clearResults()
      return
    }

    await searchStore.performSearch(searchQuery.value, pageSize)
  }

  // Debounced search function
  const debouncedSearch = debounce(async () => {
    await loadInitialData()
  }, 500)

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
    updateUrl()
  }

  function loadMore (category: ActiveTab) {
    searchStore.loadMoreResults(category)
  }

  function toggleViewMode () {
    if (searchStore.viewMode === 'unified') {
      searchStore.setViewMode('tabbed', currentTab.value)
    } else {
      searchStore.setViewMode('unified')
    }
    updateUrl()
  }

  // Initialize view mode from URL
  onMounted(() => {
    if (route.query.view === 'tabbed') {
      searchStore.setViewMode('tabbed')
      if (route.query.tab) {
        const tab = route.query.tab as ActiveTab
        searchStore.setActiveTab(tab)
        currentTab.value = tab
      }
    }
  })

  // Watch for changes and update URL
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
    updateUrl()

    if (!newQuery.trim()) {
      searchStore.clearResults()
      return
    }

    debouncedSearch()
  }, { immediate: true })
</script>
<style lang="scss" scoped>
.content {
  height: calc(100vh - var(--v-layout-top) - var(--v-layout-bottom) - 2rem);
  padding: 1rem;
}

.section-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: rgba(var(--v-theme-on-surface), 0.87);
}

.results-container {
  width: 100%;
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
