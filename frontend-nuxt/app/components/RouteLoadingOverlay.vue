<template>
  <Transition name="route-loading-fade">
    <div v-if="isLoading" class="route-loading-overlay">
      <div class="loader-container">
        <v-progress-circular
          indeterminate
          color="primary"
          size="64"
          width="5"
        />
        <div class="loading-text">Loading...</div>
      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
const { isLoading } = useLoadingIndicator()
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
