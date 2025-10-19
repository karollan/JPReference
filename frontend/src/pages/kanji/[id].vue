<template>
  <v-container class="kanji-detail-page">
    <!-- Loading State -->
    <v-row v-if="loading" justify="center" class="pa-8">
      <v-col cols="auto" class="text-center">
        <v-progress-circular indeterminate color="primary" size="48" />
        <p class="mt-4 text-grey">Loading kanji details...</p>
      </v-col>
    </v-row>

    <!-- Error State -->
    <v-row v-else-if="error" justify="center" class="pa-8">
      <v-col cols="12" class="text-center">
        <v-icon size="64" color="error">mdi-alert-circle</v-icon>
        <h3 class="text-error mt-4">{{ error }}</h3>
        <v-btn color="primary" @click="goBack" class="mt-4">
          <v-icon start>mdi-arrow-left</v-icon>
          Go Back
        </v-btn>
      </v-col>
    </v-row>

    <!-- Kanji Detail Content -->
    <div v-else-if="kanji" class="kanji-detail-content">
      <!-- Header with Back Button -->
      <v-row class="mb-6">
        <v-col cols="12">
          <v-btn
            color="primary"
            variant="text"
            prepend-icon="mdi-arrow-left"
            @click="goBack"
            class="mb-4"
          >
            Back to Search
          </v-btn>
          
          <v-card class="kanji-detail-header-card">
            <v-card-title class="kanji-detail-header">
              <div class="d-flex align-center justify-space-between">
                <div class="d-flex align-center">
                  <span class="text-h4 font-weight-bold primary--text mr-4">
                    {{ kanji.character }}
                  </span>
                  <v-chip
                    v-if="kanji.jlptNew"
                    :color="getJllptColor(kanji.jlptNew)"
                    size="large"
                    class="mr-2"
                  >
                    N{{ kanji.jlptNew }}
                  </v-chip>
                  <v-chip
                    v-if="kanji.grade"
                    color="blue-grey"
                    size="small"
                    variant="outlined"
                  >
                    Grade {{ kanji.grade }}
                  </v-chip>
                </div>
              </div>
            </v-card-title>
          </v-card>
        </v-col>
      </v-row>

      <!-- Main Content -->
      <v-row>
        <!-- Left Column - Character Info -->
        <v-col cols="12" md="6">
          <!-- Character Display -->
          <div class="character-display mb-6">
            <div class="text-center">
              <div class="kanji-large kanji-animate">{{ kanji.character }}</div>
              <div class="text-caption text-grey mt-2">
                {{ kanji.strokeCount }} strokes
              </div>
            </div>
          </div>

          <!-- Meanings -->
          <v-card variant="outlined" class="mb-4">
            <v-card-title class="text-h6 pa-4 pb-2">
              <v-icon start>mdi-translate</v-icon>
              Meanings
            </v-card-title>
            <v-card-text class="pt-0">
              <div v-if="kanji.meanings?.length" class="meanings-list">
                <v-chip
                  v-for="(meaning, index) in kanji.meanings"
                  :key="index"
                  class="ma-1"
                  color="primary"
                  variant="outlined"
                  size="small"
                >
                  {{ meaning }}
                </v-chip>
              </div>
              <div v-else class="text-grey">No meanings available</div>
            </v-card-text>
          </v-card>

          <!-- Metadata -->
          <v-card variant="outlined">
            <v-card-title class="text-h6 pa-4 pb-2">
              <v-icon start>mdi-information</v-icon>
              Information
            </v-card-title>
            <v-card-text class="pt-0">
              <v-list bg-color="transparent" density="compact">
                <v-list-item v-if="kanji.strokeCount">
                  <template v-slot:prepend>
                    <v-icon>mdi-pencil</v-icon>
                  </template>
                  <v-list-item-title>Stroke Count</v-list-item-title>
                  <v-list-item-subtitle>{{ kanji.strokeCount }}</v-list-item-subtitle>
                </v-list-item>
                
                <v-list-item v-if="kanji.frequency">
                  <template v-slot:prepend>
                    <v-icon>mdi-chart-line</v-icon>
                  </template>
                  <v-list-item-title>Frequency Rank</v-list-item-title>
                  <v-list-item-subtitle>#{{ kanji.frequency }}</v-list-item-subtitle>
                </v-list-item>
                
                <v-list-item v-if="kanji.jlptOld">
                  <template v-slot:prepend>
                    <v-icon>mdi-school</v-icon>
                  </template>
                  <v-list-item-title>Old JLPT Level</v-list-item-title>
                  <v-list-item-subtitle>N{{ kanji.jlptOld }}</v-list-item-subtitle>
                </v-list-item>
              </v-list>
            </v-card-text>
          </v-card>
        </v-col>

        <!-- Right Column - Readings -->
        <v-col cols="12" md="6">
          <!-- On Readings -->
          <v-card variant="outlined" class="mb-4">
            <v-card-title class="text-h6 pa-4 pb-2">
              <v-icon start>mdi-format-text</v-icon>
              On Readings (音読み)
            </v-card-title>
            <v-card-text class="pt-0">
              <div v-if="kanji.readingsOn?.length" class="readings-list">
                <v-chip
                  v-for="(reading, index) in kanji.readingsOn"
                  :key="index"
                  class="ma-1"
                  color="orange"
                  variant="outlined"
                  size="small"
                >
                  {{ reading }}
                </v-chip>
              </div>
              <div v-else class="text-grey">No on readings available</div>
            </v-card-text>
          </v-card>

          <!-- Kun Readings -->
          <v-card variant="outlined" class="mb-4">
            <v-card-title class="text-h6 pa-4 pb-2">
              <v-icon start>mdi-format-text</v-icon>
              Kun Readings (訓読み)
            </v-card-title>
            <v-card-text class="pt-0">
              <div v-if="kanji.readingsKun?.length" class="readings-list">
                <v-chip
                  v-for="(reading, index) in kanji.readingsKun"
                  :key="index"
                  class="ma-1"
                  color="green"
                  variant="outlined"
                  size="small"
                >
                  {{ reading }}
                </v-chip>
              </div>
              <div v-else class="text-grey">No kun readings available</div>
            </v-card-text>
          </v-card>

          <!-- Study Actions -->
          <v-card variant="outlined">
            <v-card-title class="text-h6 pa-4 pb-2">
              <v-icon start>mdi-book-open</v-icon>
              Study Tools
            </v-card-title>
            <v-card-text class="pt-0">
              <div class="d-flex flex-wrap gap-2">
                <v-btn
                  color="primary"
                  variant="outlined"
                  size="small"
                  prepend-icon="mdi-pencil"
                  @click="showStrokeOrder"
                >
                  Stroke Order
                </v-btn>
                <v-btn
                  color="secondary"
                  variant="outlined"
                  size="small"
                  prepend-icon="mdi-volume-high"
                  @click="playPronunciation"
                >
                  Pronunciation
                </v-btn>
                <v-btn
                  color="success"
                  variant="outlined"
                  size="small"
                  prepend-icon="mdi-heart"
                  @click="addToFavorites"
                >
                  Add to Favorites
                </v-btn>
              </div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </div>
  </v-container>
</template>

<script lang="ts" setup>
import { useKanjiStore } from '@/stores/kanji'
import type { Kanji } from '@/types/Kanji'

// Get route params
const route = useRoute()
const router = useRouter()
const kanjiStore = useKanjiStore()

// Reactive state
const loading = ref(true)
const error = ref<string | null>(null)
const kanji = ref<Kanji | null>(null)

// Get kanji ID from route params
const kanjiId = computed(() => (route.params as any).id as string)

const getJllptColor = (level: number) => {
  const colors = {
    1: 'red',
    2: 'orange', 
    3: 'yellow',
    4: 'green',
    5: 'blue'
  }
  return colors[level as keyof typeof colors] || 'grey'
}

const goBack = () => {
  router.back()
}

const showStrokeOrder = () => {
  // TODO: Implement stroke order animation
  console.log('Show stroke order for:', kanji.value?.character)
  // For now, show a simple alert
  alert(`Stroke order for ${kanji.value?.character} would be displayed here. This feature can be enhanced with a stroke order animation library.`)
}

const playPronunciation = () => {
  // Basic text-to-speech implementation
  if (kanji.value?.readingsOn?.length) {
    const utterance = new SpeechSynthesisUtterance(kanji.value.readingsOn[0])
    utterance.lang = 'ja-JP'
    speechSynthesis.speak(utterance)
  } else if (kanji.value?.readingsKun?.length) {
    const utterance = new SpeechSynthesisUtterance(kanji.value.readingsKun[0])
    utterance.lang = 'ja-JP'
    speechSynthesis.speak(utterance)
  } else {
    alert('No readings available for pronunciation')
  }
}

const addToFavorites = () => {
  // TODO: Implement favorites functionality with store
  console.log('Add to favorites:', kanji.value?.character)
  alert(`${kanji.value?.character} added to favorites! (This feature can be connected to a favorites store)`)
}

// Load kanji data
const loadKanji = async () => {
  try {
    loading.value = true
    error.value = null
    
    // Try to get kanji by ID from the store first
    const foundKanji = kanjiStore.kanjiList.find(k => k.id === kanjiId.value)
    
    if (foundKanji) {
      kanji.value = foundKanji
    } else {
      // If not found in store, try to fetch from API using store method
      kanji.value = await kanjiStore.getKanjiById(kanjiId.value)
      // This would require implementing a getKanjiById method in the store
      if (!kanji.value) {
        error.value = 'Kanji not found'
      }
    }
  } catch (err) {
    console.error('Error loading kanji:', err)
    error.value = 'Failed to load kanji details'
  } finally {
    loading.value = false
  }
}

// Load data on mount
onMounted(() => {
  loadKanji()
})

// Watch for route changes
watch(() => (route.params as any).id, () => {
  loadKanji()
})
</script>

<style lang="scss" scoped>
.kanji-detail-page {
  max-width: 1200px;
}

.kanji-detail-header-card {
  .kanji-detail-header {
    background: rgba(var(--v-theme-primary), 0.15);
    border-bottom: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  }
}

.character-display {
  .kanji-large {
    font-size: 8rem;
    font-weight: 300;
    line-height: 1;
    color: var(--v-theme-primary);
    text-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  }
  
  .kanji-animate {
    animation: kanjiAppear 0.6s ease-out;
  }
}

@keyframes kanjiAppear {
  0% {
    opacity: 0;
    transform: scale(0.8) rotate(-5deg);
  }
  50% {
    opacity: 0.8;
    transform: scale(1.05) rotate(2deg);
  }
  100% {
    opacity: 1;
    transform: scale(1) rotate(0deg);
  }
}

.meanings-list,
.readings-list {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}

.v-card {
  transition: all 0.2s ease;
  
  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  }
}

.v-chip {
  transition: all 0.2s ease;
  
  &:hover {
    transform: scale(1.05);
  }
}

// Responsive adjustments
@media (max-width: 768px) {
  .character-display .kanji-large {
    font-size: 6rem;
  }
}
</style>
