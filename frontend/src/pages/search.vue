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
          @update:model-value="onSearchChanged"
        >
        </v-text-field>
        <v-col
          v-if="searchStore.error"
          cols="12"
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
      <v-col
        cols="8"
        class="d-flex flex-column"
        :style="{ height: resultColumnHeight }"
      >
        <div class="text-left section-title mb-2">
          <span>Vocabulary</span>
        </div>
        <v-divider class="mb-4 mt-2"></v-divider>
        <div class="vocabulary-iterator overflow-y-auto h-100">
          <VocabularySummary
            v-for="vocabulary in searchStore.vocabularyList?.data || []"
            :key="vocabulary.id"
            :vocabulary="vocabulary"
            class="mb-4"
          />
        </div>
      </v-col>
      <v-col
        cols="4"
        class="d-flex flex-column"
        :style="{ height: resultColumnHeight }"
      >
        <div class="text-left section-title mb-2">
          <span>Kanji</span>
        </div>
        <v-divider class="mb-4 mt-2"></v-divider>
        <div class="flex-fill overflow-y-auto">
          <div
            class="kanji-iterator"
          >
            <KanjiSummary
              v-for="kanji in searchStore.kanjiList?.data || []"
              :key="kanji.literal"
              :kanji="kanji"
              class="mb-4"
            />
          </div>
        </div>
        <div class="text-left section-title mb-2 mt-4">
          <span>Proper Nouns</span>
        </div>
        <v-divider class="mb-4 mt-2"></v-divider>
        <div class="flex-fill overflow-y-auto">
          <div class="proper-noun-iterator">
            <ProperNounSummary
              v-for="properNoun in searchStore.properNounList?.data || []"
              :key="properNoun.id"
              :properNoun="properNoun"
              class="mb-4"
            />
          </div>
        </div>
      </v-col>
    </v-row>
  </v-container>
</template>

<script lang="ts" setup>
import KanjiSummary from '@/components/search/KanjiSummary.vue'
import VocabularySummary from '@/components/search/VocabularySummary.vue'
import { useSearchStore } from '@/stores/search'
import { ref } from 'vue'

const route = useRoute()
const router = useRouter()
const searchStore = useSearchStore()

const pageSize = 50
const searchColumn = ref<any | null>(null)
const searchQuery = ref<string>(route.query.query as string || '')

const resultColumnHeight = computed(() => {
  console.log(searchColumn.value)
  return searchColumn.value ? `${window.innerHeight - searchColumn.value.$el.getBoundingClientRect().bottom - 32}px` : '400px'
})

// Simple debounce function
const debounce = (func: Function, delay: number) => {
  let timeoutId: number
  return (...args: any[]) => {
    clearTimeout(timeoutId)
    timeoutId = setTimeout(() => func.apply(null, args), delay)
  }
}



const loadInitialData = async () => {
  await searchStore.performSearch(searchQuery.value, pageSize);
}

// Debounced search function
const debouncedSearch = debounce(async () => {
  await loadInitialData()
}, 500)

const updateUrl = (query: string) => {
  router.replace({ 
    query: { 
      ...route.query,
      query: query || undefined // Remove query param if empty
    } 
  })
}

const onSearchChanged = () => {
  debouncedSearch()
}

watch(searchQuery, (newQuery) => {
  searchStore.reset()
  updateUrl(newQuery)
  debouncedSearch()
}, { immediate: true})


</script>
<style lang="scss" scoped>
.content {
  height: calc(100vh - var(--v-layout-top) - var(--v-layout-bottom) - 2rem);
  padding: 1rem;
}

</style>
