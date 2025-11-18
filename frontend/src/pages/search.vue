<template>
  <v-container
    :max-width="$vuetify.display.mdAndUp ? 1200 : '100%'"
    class="text-center main-container-layout"
    fluid
  >
    <v-row
      class="content d-flex flex-row"
      justify="center"
      align="start"
    >
      <!-- Search Bar -->
      <v-col
        ref="searchColumn"
        cols="12"
      >
        <v-text-field
          v-model="searchQuery"
          bg-color="white"
          icon-color="#00000066"
          class="home__search mx-auto"
          density="comfortable"
          placeholder="Search"
          prepend-inner-icon="mdi-magnify"
          variant="outlined"
          clearable
          hide-details="auto"
        >
        </v-text-field>
        <v-col
          v-if="searchStore.error"
          cols="12"
          class="pa-0 mt-4"
        >
          <v-alert
            type="error"
            class="mb-4"
            closable
          >
            {{ searchStore.error }}
          </v-alert>
        </v-col>
      </v-col>

      <!-- Unified View -->
      <v-col
        v-if="searchStore.viewMode === 'unified'"
        cols="12"
        class="results-container"
      >
        <v-row>
          <!-- Vocabulary Column -->
          <v-col
            cols="12"
            md="8"
            class="d-flex flex-column"
          >
            <div class="d-flex justify-space-between align-center mb-2">
              <div class="text-left section-title">
                <span>Vocabulary</span>
              </div>
              <v-btn
                v-if="!searchStore.loading && vocabularyCount > 5"
                size="small"
                variant="text"
                color="primary"
                @click="showAllResults('vocabulary')"
              >
                See All {{ vocabularyCount }} Results
              </v-btn>
            </div>
            <v-divider class="mb-4 mt-2"></v-divider>
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
                  <v-icon size="64" color="grey-lighten-1">mdi-text-search</v-icon>
                  <div class="text-h6 mt-4 text-grey">No vocabulary found</div>
                </v-card>
              </template>
            </div>
          </v-col>

          <!-- Kanji & Proper Nouns Column -->
          <v-col
            cols="12"
            md="4"
            class="d-flex flex-column"
          >
            <!-- Kanji Section -->
            <div class="d-flex justify-space-between align-center mb-2">
              <div class="text-left section-title">
                <span>Kanji</span>
              </div>
              <v-btn
                v-if="!searchStore.loading && kanjiCount > 5"
                size="small"
                variant="text"
                color="primary"
                @click="showAllResults('kanji')"
              >
                See All {{ kanjiCount }}
              </v-btn>
            </div>
            <v-divider class="mb-4 mt-2"></v-divider>
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
                  <v-icon size="48" color="grey-lighten-1">mdi-ideogram-cjk</v-icon>
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
                size="small"
                variant="text"
                color="primary"
                @click="showAllResults('properNouns')"
              >
                See All {{ properNounCount }}
              </v-btn>
            </div>
            <v-divider class="mb-4 mt-2"></v-divider>
            <div class="proper-noun-iterator overflow-y-auto" :style="{ maxHeight: sideColumnHeight }">
              <template v-if="searchStore.loading">
                <ProperNounSummarySkeleton v-for="i in 2" :key="i" />
              </template>
              <template v-else-if="limitedProperNouns.length > 0">
                <ProperNounSummary
                  v-for="properNoun in limitedProperNouns"
                  :key="properNoun.id"
                  :properNoun="properNoun"
                />
              </template>
              <template v-else>
                <v-card class="pa-6 text-center" variant="outlined">
                  <v-icon size="48" color="grey-lighten-1">mdi-account</v-icon>
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
        cols="12"
        class="results-container"
      >
        <div class="tab-header mb-4">
          <v-tabs
            v-model="currentTab"
            show-arrows
            color="primary"
            class="flex-grow-1 tab-nav"
          >
            <v-tab value="vocabulary">
              Vocabulary
              <v-chip
                v-if="vocabularyCount > 0"
                size="small"
                class="ml-2"
                color="primary"
                variant="flat"
              >
                {{ vocabularyCount }}
              </v-chip>
            </v-tab>
            <v-tab value="kanji">
              Kanji
              <v-chip
                v-if="kanjiCount > 0"
                size="small"
                class="ml-2"
                color="primary"
                variant="flat"
              >
                {{ kanjiCount }}
              </v-chip>
            </v-tab>
            <v-tab value="properNouns">
              Proper Nouns
              <v-chip
                v-if="properNounCount > 0"
                size="small"
                class="ml-2"
                color="primary"
                variant="flat"
              >
                {{ properNounCount }}
              </v-chip>
            </v-tab>
          </v-tabs>
          <v-btn
            class="tabbed-back-btn mt-2 mt-md-0"
            variant="text"
            color="primary"
            prepend-icon="mdi-arrow-left"
            @click="returnToUnifiedView"
          >
            Back to unified view
          </v-btn>
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
                    variant="outlined"
                    :loading="searchStore.loadingMore"
                    @click="loadMore('vocabulary')"
                  >
                    Load More
                  </v-btn>
                </div>
              </template>
              <template v-else>
                <v-card class="pa-12 text-center" variant="outlined">
                  <v-icon size="96" color="grey-lighten-1">mdi-text-search</v-icon>
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
                    variant="outlined"
                    :loading="searchStore.loadingMore"
                    @click="loadMore('kanji')"
                  >
                    Load More
                  </v-btn>
                </div>
              </template>
              <template v-else>
                <v-card class="pa-12 text-center" variant="outlined">
                  <v-icon size="96" color="grey-lighten-1">mdi-ideogram-cjk</v-icon>
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
                  :properNoun="properNoun"
                />
                <div v-if="searchStore.properNounList.pagination.hasNext" class="text-center mt-4">
                  <v-btn
                    color="primary"
                    variant="outlined"
                    :loading="searchStore.loadingMore"
                    @click="loadMore('properNouns')"
                  >
                    Load More
                  </v-btn>
                </div>
              </template>
              <template v-else>
                <v-card class="pa-12 text-center" variant="outlined">
                  <v-icon size="96" color="grey-lighten-1">mdi-account</v-icon>
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
import KanjiSummary from '@/components/search/KanjiSummary.vue'
import VocabularySummary from '@/components/search/VocabularySummary.vue'
import ProperNounSummary from '@/components/search/ProperNounSummary.vue'
import KanjiSummarySkeleton from '@/components/search/KanjiSummarySkeleton.vue'
import VocabularySummarySkeleton from '@/components/search/VocabularySummarySkeleton.vue'
import ProperNounSummarySkeleton from '@/components/search/ProperNounSummarySkeleton.vue'
import { useSearchStore } from '@/stores/search'
import type { ActiveTab } from '@/stores/search'
import { ref, computed, watch, onMounted } from 'vue'

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
const debounce = (func: Function, delay: number) => {
  let timeoutId: number
  return (...args: any[]) => {
    clearTimeout(timeoutId)
    timeoutId = setTimeout(() => func.apply(null, args), delay)
  }
}

const loadInitialData = async () => {
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

const updateUrl = () => {
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

const showAllResults = (tab: ActiveTab) => {
  searchStore.setViewMode('tabbed', tab)
  currentTab.value = tab
  updateUrl()
}

const loadMore = (category: ActiveTab) => {
  searchStore.loadMoreResults(category)
}

const returnToUnifiedView = () => {
  searchStore.setViewMode('unified')
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

watch(currentTab, (newTab) => {
  searchStore.setActiveTab(newTab)
  updateUrl()
})

watch(searchQuery, (newQuery) => {
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
  color: #333;
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
  scrollbar-color: rgba(0, 0, 0, 0.3) transparent;
  
  &::-webkit-scrollbar {
    width: 6px;
    height: 6px;
  }
  
  &::-webkit-scrollbar-thumb {
    background: rgba(0, 0, 0, 0.3);
    border-radius: 3px;
  }
  
  &::-webkit-scrollbar-thumb:hover {
    background: rgba(0, 0, 0, 0.5);
  }
}
</style>
