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
    default: ''
  },
  clientId: {
    type: String,
    default: ''
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

const config = useRuntimeConfig()

// Use props if provided, otherwise fall back to runtime config
const clientId = computed(() => props.clientId || config.public.adsenseClientId || '')
const slotId = computed(() => props.slotId || config.public.adsenseSlotId || '')

const isDev = import.meta.dev
const error = ref(false)

const showAd = computed(() => {
  return (clientId.value && slotId.value) || isDev
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
