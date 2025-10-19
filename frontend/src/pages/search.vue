<template>
  <v-container class="text-center kanji main-container-layout">
    <v-row justify="center" dense>
      <v-col cols="12">
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
          @update:model-value="onSearchChanged"
        >
        </v-text-field>
      </v-col>
      <v-col cols="12">
        <v-alert v-if="kanjiStore.error" type="error" class="mb-4">
          {{ kanjiStore.error }}
        </v-alert>
        
        <p style="text-align: left;">
          Filter by JLPT Level
        </p>
        <v-chip-group
          v-model="selectedLevel"
          multiple
          class="mb-4"
        >
          <v-chip
            v-for="jltpLevel in jlptLevels" 
            :key="jltpLevel"
            :value="jltpLevel"
            filter
          >
            {{ jltpLevel === 0 ? 'N/A' : `N${jltpLevel}` }}
          </v-chip>
        </v-chip-group>
        
        <v-row>
          <v-col cols="12">
            <v-tabs
              v-model="tab"
              color="primary"
              align-tabs="center"
            >
              <v-tab
                value="kanji"
                class="text-none"  
              >
                Kanji
              </v-tab>
              <v-tab
                value="vocabulary"
                class="text-none"
              >
                Vocabulary
              </v-tab>
            </v-tabs>
          </v-col>
        </v-row>
        <v-tabs-window v-model="tab">
          <v-tabs-window-item value="kanji">
            <p style="text-align: right;">
              {{ kanjiStore.totalCount }} kanji found
            </p>
            <!-- Initial loading state -->
            <v-row v-if="kanjiStore.loading && kanjiList.length === 0" justify="center" class="pa-8">
              <v-col cols="auto" class="text-center">
                <v-progress-circular indeterminate color="primary" size="48" />
                <p class="mt-4 text-grey">Loading kanji...</p>
              </v-col>
            </v-row>

            <!-- Kanji Grid -->
            <v-data-iterator
              v-else
              :items="kanjiList"
              :items-per-page="-1"
              item-key="id"
              class="kanji-iterator justify-between"
            >
              <template v-slot:default="{ items }">
                <v-row>
                  <v-col
                    v-for="item in items"
                    :key="item.raw.id"
                    cols="12"
                    sm="6"
                    md="4"
                    lg="4"
                    xl="4"
                  >
                    <kanji-dictionary-card 
                      :kanji="item.raw" 
                    />
                  </v-col>
                </v-row>
              </template>
              
              <template v-slot:no-data>
                <v-row justify="center" class="pa-8">
                  <v-col cols="12" class="text-center">
                    <v-icon size="64" color="grey-lighten-1">mdi-book-open-variant</v-icon>
                    <h3 class="text-grey-lighten-1 mt-4">No kanji found</h3>
                    <p class="text-grey">Try adjusting your search criteria</p>
                  </v-col>
                </v-row>
              </template>
            </v-data-iterator>

            <!-- Loading more indicator -->
            <v-row v-if="kanjiStore.loadingMore" justify="center" class="pa-4">
              <v-col cols="auto">
                <v-progress-circular indeterminate color="primary" size="32" />
                <span class="ml-3 text-grey">Loading more...</span>
              </v-col>
            </v-row>

            <!-- End of results -->
            <v-row v-else-if="!kanjiStore.hasMorePages && kanjiList.length > 0" justify="center" class="pa-4">
              <v-col cols="auto">
                <v-chip color="grey-lighten-2" variant="outlined">
                  <v-icon start>mdi-check-circle</v-icon>
                  All kanji loaded
                </v-chip>
              </v-col>
            </v-row>
          </v-tabs-window-item>
        </v-tabs-window>
      </v-col>
    </v-row>

  </v-container>
</template>

<script lang="ts" setup>
import { useKanjiStore } from '@/stores/kanji'
import KanjiDictionaryCard from '@/components/KanjiDictionaryCard.vue'
import { onMounted, onUnmounted, ref } from 'vue'

// Simple debounce function
const debounce = (func: Function, delay: number) => {
  let timeoutId: number
  return (...args: any[]) => {
    clearTimeout(timeoutId)
    timeoutId = setTimeout(() => func.apply(null, args), delay)
  }
}

const kanjiStore = useKanjiStore()

const tab = shallowRef<string>('kanji')
const selectedLevel = ref<number[]>([])
const searchQuery = ref<string>('')
const pageSize = 50
const jlptLevels = [5, 4, 3, 2, 1, 0]


// Get the kanji list directly from store
const kanjiList = computed(() => kanjiStore.kanjiList)

const loadInitialData = async () => {
  await kanjiStore.fetchKanji(selectedLevel.value, searchQuery.value || null, pageSize)
}

const loadMoreData = async () => {
  await kanjiStore.fetchNextPage(pageSize)
}

// Debounced search function
const debouncedSearch = debounce(async () => {
  await loadInitialData()
}, 500)

const onSearchChanged = () => {
  debouncedSearch()
}

// Infinite scroll setup
const scrollHandler = async () => {
  const scrollPosition = window.innerHeight + window.scrollY
  const documentHeight = document.documentElement.offsetHeight
  const threshold = 400 // Load when user is 200px from bottom
  if (scrollPosition >= documentHeight - threshold && kanjiStore.hasMorePages && !kanjiStore.loadingMore) {
    await loadMoreData()
  }
}


onMounted(async () => {
  await loadInitialData()
  window.addEventListener('scroll', scrollHandler)
})

onUnmounted(() => {
  window.removeEventListener('scroll', scrollHandler)
})

watch(selectedLevel, async () => {
  await loadInitialData()
})

</script>
<style lang="scss" scoped>
.kanji-iterator {
  min-height: 300px;
}

.main-container-layout {
  max-width: 1400px;
}

.home__search {
  max-width: 400px;
}
</style>
