<template>
  <div v-if="showAd" class="adsense-container my-6 text-center">
    <!-- Development/Placeholder Display -->
    <v-card
      v-if="isDev"
      class="d-flex align-center justify-center bg-surface-variant text-medium-emphasis"
      variant="outlined"
      style="min-height: 250px; width: 100%; border-style: dashed !important;"
    >
      <div class="text-center pa-4">
        <v-icon icon="mdi-google-ads" size="48" class="mb-2" />
        <div class="text-h6">Google AdSense</div>
        <div class="text-caption font-mono text-medium-emphasis mt-2">
          Slot: {{ slotId }}<br>
          Client: {{ clientId }}
        </div>
      </div>
    </v-card>

    <!-- Actual AdSense Unit -->
    <ins
      v-else
      class="adsbygoogle"
      style="display:block"
      :data-ad-client="clientId"
      :data-ad-slot="slotId"
      :data-ad-format="format"
      :data-full-width-responsive="fullWidthResponsive"
    />
  </div>
</template>

<script setup lang="ts">
import { onMounted, computed, ref } from 'vue'

const props = defineProps({
  slotId: {
    type: String,
    default: () => import.meta.env.VITE_ADSENSE_SLOT_ID || ''
  },
  clientId: {
    type: String,
    default: () => import.meta.env.VITE_ADSENSE_CLIENT_ID || ''
  },
  format: {
    type: String,
    default: 'auto'
  },
  fullWidthResponsive: {
    type: String,
    default: 'true'
  }
})

const isDev = import.meta.env.DEV
const error = ref(false)

const showAd = computed(() => {
  return (props.clientId && props.slotId) || isDev
})

onMounted(() => {
  if (showAd.value && !isDev) {
    try {
      // Initialize ad
      // @ts-ignore
      (window.adsbygoogle = window.adsbygoogle || []).push({});
    } catch (e) {
      console.error('AdSense initialization error:', e)
      error.value = true
    }
  }
})
</script>

<style scoped>
.adsense-container {
  min-height: 250px;
  overflow: hidden;
}
</style>
