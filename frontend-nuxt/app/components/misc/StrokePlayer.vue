<template>
  <v-card class="stroke-player bg-transparent" flat>
    <!-- VueDmak Component -->
    <div v-show="hasData">
      <VueDmak
        ref="dmakRef"
        :autoplay="autoplay"
        :grid="gridOptions"
        :step="stepValue"
        :stroke="strokeOptions"
        :text="text"
        :uri="uri"
        :height="90"
        :canvas-style="canvasStyle"
        @loaded="onLoaded"
        @drew="onDrew"
      />
    </div>

    <!-- Control Center -->
    <v-card
      v-if="hasData"
      class="control-center mx-auto rounded-pill border-thin pa-1"
      elevation="0"
      max-width="300"
      variant="flat"
    >
      <div class="d-flex align-center justify-space-between px-1">
        <v-btn
          icon="mdi-refresh"
          size="x-small"
          variant="text"
          title="Reset"
          density="comfortable"
          @click="reset"
        />
        <!-- Main Controls -->
        <div class="d-flex align-center gap-1">
          <v-btn
            icon="mdi-rewind"
            size="x-small"
            variant="text"
            density="comfortable"
            @click="back"
          />
          <v-btn
            :icon="isPlaying ? 'mdi-pause' : 'mdi-play'"
            color="primary"
            size="small"
            variant="tonal"
            density="comfortable"
            @click="togglePlay"
          />
          <v-btn
            icon="mdi-fast-forward"
            size="x-small"
            variant="text"
            density="comfortable"
            @click="next"
          />
        </div>

        <!-- Utility Controls -->
        <v-menu
          v-model="showOptions"
          :close-on-content-click="false"
          location="top center"
          offset="8"
        >
          <template #activator="{ props }">
            <v-btn
              v-bind="props"
              icon="mdi-cog"
              size="x-small"
              variant="text"
              density="comfortable"
              :color="showOptions ? 'primary' : undefined"
            />
          </template>

          <v-card class="options-panel pa-3 rounded-lg" min-width="240">
            <div class="text-caption font-weight-bold mb-2 d-flex align-center">
              <v-icon class="mr-1" size="x-small">mdi-tune</v-icon>
              Playback Options
            </div>

            <!-- Speed Slider -->
            <div class="option-item mb-2">
              <div class="text-caption text-medium-emphasis mb-0" style="font-size: 0.7rem !important">Speed</div>
              <v-slider
                v-model="speed"
                color="primary"
                density="compact"
                hide-details
                :max="10"
                :min="1"
                :step="1"
              >
                <template #prepend>
                  <v-icon color="medium-emphasis" size="x-small">mdi-rabbit</v-icon>
                </template>
                <template #append>
                  <span class="text-caption font-weight-bold" style="width: 24px; font-size: 0.7rem !important">
                    {{ speed }}x
                  </span>
                </template>
              </v-slider>
            </div>

            <v-divider class="mb-2" />

            <!-- Stroke Numbers Checkbox -->
            <v-checkbox
              v-model="showStrokeNumbers"
              color="primary"
              density="compact"
              hide-details
            >
              <template #label>
                <span class="text-caption">Show Stroke Numbers</span>
              </template>
            </v-checkbox>
          </v-card>
        </v-menu>
      </div>
    </v-card>
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed, defineAsyncComponent } from 'vue'

// Import VueDmak only on client side - raphael requires window
const VueDmak = defineAsyncComponent(() => import('vue-dmak').then(m => m.VueDmak))

const props = defineProps<{
  text: string
  uri: string
}>()

const dmakRef = ref<any>(null)
const isPlaying = ref(true)
const autoplay = ref(true)
const speed = ref(5)
const showStrokeNumbers = ref(false)
const showOptions = ref(false)
const totalStrokes = ref(0)
const hasData = ref(true)

// Map speed (1-10) to step (0.03 to 0.003)
const stepValue = computed(() => 0.03 / speed.value)

const strokeOptions = computed(() => ({
  animated: {
    drawing: true,
    erasing: true
  },
  order: {
    visible: showStrokeNumbers.value,
    attr: {
      'font-size': '12',
      fill: '#999999'
    }
  },
  attr: {
    active: '#BF0000',
    stroke: 'currentColor',
    'stroke-width': 4,
    'stroke-linecap': 'round',
    'stroke-linejoin': 'round'
  }
}))

const gridOptions = reactive({
  show: true,
  attr: {
    stroke: 'rgba(var(--v-border-color), 0.2)',
    'stroke-width': 0.5,
    'stroke-dasharray': '--'
  }
})

const canvasStyle = reactive({
  display: 'flex',
  justifyContent: 'center',
  marginBottom: '8px'
})

function onLoaded(event: any) {
  const strokes = event?.strokes ?? event ?? []
  const dataAvailable = event?.hasData ?? (Array.isArray(event) && event.length > 0)
  
  totalStrokes.value = strokes.length
  hasData.value = dataAvailable
  isPlaying.value = dataAvailable
}

function onDrew(pointer: number) {
  if (pointer === totalStrokes.value - 1) {
    isPlaying.value = false
  }
}

function togglePlay() {
  if (isPlaying.value) {
    dmakRef.value?.pause()
  } else {
    dmakRef.value?.render()
  }
  isPlaying.value = !isPlaying.value
}

function reset() {
  // Use the new exposed reset method which handles restart gracefully
  dmakRef.value?.reset()
  isPlaying.value = true
}

function next() {
  isPlaying.value = false
  dmakRef.value?.pause()
  dmakRef.value?.renderNextStrokes(1)
}

function back() {
  isPlaying.value = false
  dmakRef.value?.pause()
  dmakRef.value?.eraseLastStrokes(1)
}
</script>

<style scoped>
.stroke-player {
  width: 100%;
}

.control-center {
  background-color: rgba(var(--v-theme-surface), 0.1) !important;
  backdrop-filter: blur(8px);
  border-color: rgba(var(--v-border-color), 0.12) !important;
  transition: all 0.2s ease-in-out;
}

.control-center:hover {
  background-color: rgba(var(--v-theme-surface), 0.6) !important;
  border-color: rgba(var(--v-theme-primary), 0.6) !important;
}

.gap-1 {
  gap: 2px;
}

.options-panel {
  border: 1px solid rgba(var(--v-border-color), 0.12);
}

:deep(.v-slider.v-input--horizontal) {
  margin-inline: 0;
}

:deep(.dmak-container svg) {
  height: max-content;
  min-height: 40px;
}
</style>


