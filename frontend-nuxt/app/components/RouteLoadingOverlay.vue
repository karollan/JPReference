<template>
  <Transition name="route-loading-fade">
    <div v-if="isLoading" class="route-loading-overlay">
      <div class="loader-container">
        <v-progress-linear
          color="primary"
          indeterminate
          chunk-count="12"
          chunk-gap="4"
          height="12"
          reverse
        ></v-progress-linear>
        <div class="loading-text">{{ loadingPhrase }}</div>
      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
const { isLoading } = useLoadingIndicator()

const phrases = [
  'Sharpening Katana...',
  'Consulting the Sensei...',
  'Brewing Green Tea...',
  'Polishing Shuriken...',
  'Summoning Kami...',
  'Practicing Kanji...',
  'Measuring Noodles...',
  'Folding Origami...',
  'Finding Zen...',
  'Warming Sake...',
  'Counting Rice Grains...',
  'Chasing Tanuki...',
  'Crossing the Torii...',
  'Waiting for the Shinkansen...',
  'Visiting the Onsen...',
  'Preparing Bentō...',
  'Tying Hachimaki...',
  'Looking for Mount Fuji...',
  'Studying Radicals...',
  'Memorizing Stroke Order...',
]

const loadingPhrase = ref(phrases[0])

watch(isLoading, (newValue) => {
  if (newValue) {
    loadingPhrase.value = phrases[Math.floor(Math.random() * phrases.length)]
  }
})
</script>

<style scoped>
.route-loading-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);
}

.loader-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 200px;
  gap: 16px;
}

.loading-text {
  color: rgba(255, 255, 255, 0.9);
  font-size: 1rem;
  font-weight: 500;
  letter-spacing: 0.05em;
}

/* Fade transition */
.route-loading-fade-enter-active,
.route-loading-fade-leave-active {
  transition: opacity 0.2s ease;
}

.route-loading-fade-enter-from,
.route-loading-fade-leave-to {
  opacity: 0;
}
</style>
