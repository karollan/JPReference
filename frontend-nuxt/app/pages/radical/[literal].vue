<template>
  <v-container class="radical-detail-page fill-height align-start" fluid>
    <v-row class="w-100" justify="center">
      <v-col cols="12" lg="10" xl="8">
        <!-- Loading State -->
        <div v-if="pending" class="d-flex justify-center align-center py-12">
          <v-progress-circular color="primary" indeterminate size="64" />
        </div>

        <!-- Error State -->
        <v-alert
          v-else-if="error || !radical"
          class="mb-4"
          type="error"
          variant="tonal"
        >
          {{ error?.message || 'Radical not found' }}
          <template #append>
            <v-btn variant="text" @click="goBack">Go Back</v-btn>
          </template>
        </v-alert>

        <!-- Content -->
        <div v-else class="radical-detail-content animate-fade-in">
          <!-- Header Section -->
          <header class="radical-header mb-6">
            <v-row class="justify-space-between">
              <v-col class="v-col-auto main-char">
                <div class="d-flex flex-column gap-2">
                  <h1 class="display-char text-h1 font-jp text-center">
                    {{ selectedLiteral || radical.literal }}
                  </h1>
                  <div class="char-meta">
                    <div class="text-body-2 text-medium-emphasis font-weight-regular text-center">
                      <v-icon class="mr-1" icon="mdi-pencil" size="small" />
                      {{ radical.strokeCount }} strokes
                    </div>
                  </div>
                </div>
              </v-col>

              <v-col class="header-actions v-col-auto justify-space-between d-flex flex-column">
                <div class="d-flex justify-end">
                  <v-btn
                    color="primary"
                    prepend-icon="mdi-arrow-left"
                    variant="tonal"
                    @click="goBack"
                  >
                    Go back
                  </v-btn>
                </div>
                <div class="badges d-flex justify-end gap-2">
                  <v-chip
                    v-if="radical.kangXiNumber"
                    color="primary"
                    label
                    variant="flat"
                  >
                    Number #{{ radical.kangXiNumber }}
                  </v-chip>
                </div>
              </v-col>
            </v-row>
            <v-divider class="mt-4 mb-6 border-opacity-25" />
          </header>

          <v-row>
            <!-- Main Content: Meanings & Readings -->
            <v-col cols="12" md="8">
              <!-- Meanings -->
              <section v-if="radical.meanings && radical.meanings.length > 0" class="meanings-section mb-8">
                <h2 class="text-h5 font-weight-bold mb-4 d-flex align-center">
                  <v-icon class="mr-2" color="primary" icon="mdi-translate" start />
                  Meanings
                </h2>

                <v-card class="meanings-card bg-surface pa-4" flat>
                  <div class="d-flex flex-wrap gap-2">
                    <v-chip
                      v-for="(meaning, index) in radical.meanings"
                      :key="index"
                      class="font-weight-medium text-body-1 px-4"
                      color="primary"
                      size="large"
                      variant="tonal"
                    >
                      {{ meaning }}
                    </v-chip>
                  </div>
                </v-card>
              </section>

              <!-- Readings -->
              <section v-if="radical.readings && radical.readings.length > 0" class="readings-section mb-8">
                <h2 class="text-h5 font-weight-bold mb-4 d-flex align-center">
                  <v-icon class="mr-2" color="secondary" icon="mdi-format-text" start />
                  Readings
                </h2>

                <v-card class="readings-card bg-surface pa-4" flat>
                  <div class="d-flex flex-wrap gap-2">
                    <v-chip
                      v-for="(reading, index) in radical.readings"
                      :key="index"
                      class="font-jp"
                      color="deep-orange-darken-2"
                      variant="outlined"
                    >
                      {{ reading }}
                    </v-chip>
                  </div>
                </v-card>
              </section>

              <!-- Associated Kanji -->
              <section class="kanji-section mb-8">
                <h2 class="text-h5 font-weight-bold mb-4 d-flex align-center">
                  <v-icon class="mr-2" color="success" icon="mdi-book-open-page-variant-outline" start />
                  Most common kanji using this variant
                </h2>

                <div class="kanji-grid">
                  <template v-if="variantKanji && variantKanji.length > 0">
                    <KanjiSummary
                      v-for="kanji in variantKanji"
                      :key="kanji.id"
                      :kanji="kanji"
                    />
                  </template>
                  <template v-else>
                    <v-card class="pa-8 text-center w-100" variant="outlined">
                      <v-icon color="grey-lighten-1" size="64">mdi-text-search</v-icon>
                      <div class="text-h6 mt-4 text-grey">No kanji found for this variant</div>
                    </v-card>
                  </template>
                </div>
              </section>
            </v-col>

            <!-- Sidebar -->
            <v-col cols="12" md="4">
              <!-- Variants -->
              <section v-if="radical.variants && radical.variants.length > 0" class="variants-section mb-6">
                <v-card class="pa-4 rounded-lg border-thin" variant="outlined">
                  <h3 class="text-overline font-weight-bold mb-2 text-medium-emphasis">Variants</h3>
                  <div class="d-flex flex-wrap gap-2">
                    <v-chip
                      v-for="(variant, index) in radical.variants"
                      :key="variant.id || `var-${index}`"
                      class="font-jp text-h6 px-3 variant-chip"
                      :color="variant.literal === selectedLiteral ? 'purple' : undefined"
                      height="48"
                      :variant="variant.literal === selectedLiteral ? 'flat' : 'tonal'"
                      @click="selectVariant(variant.literal)"
                    >
                      {{ variant.literal }}
                    </v-chip>
                  </div>
                </v-card>
              </section>

              <!-- Study Tools -->
              <section class="tools-section mb-6">
                <v-card class="pa-4 rounded-lg border-thin" variant="outlined">
                  <h3 class="text-overline font-weight-bold mb-2 text-medium-emphasis">Study Tools</h3>
                  <div class="d-flex flex-column gap-2">
                    <ClientOnly>
                      <StrokePlayer
                        :key="selectedLiteral ?? 'none'"
                        :text="selectedLiteral || ''"
                        uri="/kanjivg/"
                      />
                    </ClientOnly>

                    <ClientOnly>
                      <VueDmak
                        :key="`dmak-${selectedLiteral ?? 'none'}`"
                        :text="selectedLiteral || ''"
                        uri="/kanjivg/"
                        view="series"
                        :width="90"
                        :height="90"
                        :seriesStyle="seriesStyle"
                        :seriesFrameStyle="frameStyle"
                      />
                    </ClientOnly>

                    <v-btn
                      color="primary"
                      class="text-none mt-2 align-self-center"
                      width="100%"
                      prepend-icon="mdi-volume-high"
                      variant="outlined"
                      @click="playPronunciation(radical?.readings?.[0] ?? '')"
                    >
                      Play pronunciation
                    </v-btn>
                  </div>
                </v-card>
              </section>

              <!-- Other details -->
              <section class="other-details-section mb-6">
                <v-card class="pa-4 rounded-lg border-thin" variant="outlined">
                  <h3 class="text-overline font-weight-bold mb-2 text-medium-emphasis">Other Info</h3>
                  <div class="reference-grid sidebar-grid">
                    <v-card
                      v-if="radical.kangXiNumber"
                      class="ref-card transition-swing"
                      elevation="0"
                      rounded="lg"
                      variant="outlined"
                    >
                      <v-card-text class="py-3">
                        <div class="text-caption text-medium-emphasis font-weight-bold line-height-1 mb-1">
                          KangXi Number
                        </div>
                        <div class="font-weight-regular entry-value">
                          {{ radical.kangXiNumber }}
                        </div>
                      </v-card-text>
                    </v-card>

                    <v-card
                      v-if="radical.code"
                      class="ref-card transition-swing"
                      elevation="0"
                      rounded="lg"
                      variant="outlined"
                    >
                      <v-card-text class="py-3">
                        <div class="text-caption text-medium-emphasis font-weight-bold line-height-1 mb-1">
                          Code
                        </div>
                        <div class="font-weight-regular entry-value">
                          {{ radical.code }}
                        </div>
                      </v-card-text>
                    </v-card>
                  </div>
                </v-card>
              </section>
              <!-- Metadata/Ids -->
              <section class="meta-section">
                <div class="text-caption text-disabled font-mono">
                  Last update: {{ updatedAtFormatted }}
                </div>
              </section>
            </v-col>
          </v-row>
        </div>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
  import { computed, watch, reactive, defineAsyncComponent } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { useRadicalService, fetchWithError } from '~/services'
  import { playPronunciation } from '@/utils/audio'
  import KanjiSummary from '@/components/search/KanjiSummary.vue'
  import StrokePlayer from '@/components/misc/StrokePlayer.vue'
  import { useSmartNavigation } from '@/composables/useSmartNavigation'

  // Import VueDmak only on client side
  const VueDmak = defineAsyncComponent(() => import('vue-dmak').then(m => m.VueDmak))

  const route = useRoute()
  const router = useRouter()
  const service = useRadicalService()

  // State
  const seriesStyle = reactive({
    display: "flex",
    wrap: "no-wrap",
    overflow: "auto",
  })
  const frameStyle = reactive({
    flexShrink: 0,
  })

  const literal = computed(() => (route.params as any).literal as string)

  const { data: radical, pending, error } = await useAsyncData(
    `radical-${literal.value}`,
    () => fetchWithError(() => service.fetchRadicalByLiteral(literal.value)),
    {
      server: true
    }
  )

  const selectedLiteral = ref<string>(literal.value)

  watch(literal, (newLiteral) => {
    if (!radical.value?.variants?.some(v => v.literal === newLiteral)) {
      selectedLiteral.value = newLiteral
    } else {
      selectedLiteral.value = newLiteral
    }
  })

  // Computed
  const updatedAtFormatted = computed(() => {
    return new Date(radical.value?.updatedAt as Date).toLocaleString(undefined, {
      dateStyle: 'short',
      timeStyle: 'short'
    })
  })

  const variantKanji = computed(() => {
    if (!radical.value || !selectedLiteral.value) return []
    const variant = radical.value.variants?.find(v => v.literal === selectedLiteral.value)
    return variant?.kanji || []
  })

  // Actions
  // Smart navigation - falls back to /search if no in-app history
  const { goBack } = useSmartNavigation()

  function selectVariant (newLiteral: string) {
    // If the new literal is a variant of the current radical, just switch locally
    if (radical.value?.variants?.some(v => v.literal === newLiteral)) {
      selectedLiteral.value = newLiteral
      router.replace({ params: { literal: newLiteral } })
    } else {
      router.push(`/radical/${newLiteral}`)
    }
  }

  watch(() => route.params.literal, async (newVal) => {
    // Check if the new literal is NOT a variant of the current radical
    const isVariantOfCurrent = radical.value?.variants?.some(v => v.literal === newVal)
    if (!isVariantOfCurrent) {
      await refreshNuxtData(`radical-${newVal}`)
    }
  })

    // SEO
  useHead({
    title: computed(() => radical.value ? `Radical: ${selectedLiteral.value || radical.value.literal} - JP Reference` : 'Loading Radical...'),
    meta: [
      {
        name: 'description',
        content: computed(() => {
          if (!radical.value) return 'Loading radical details...'
          const meanings = radical.value.meanings?.join(', ')
          return `Details for radical ${selectedLiteral.value || radical.value.literal}. Meanings: ${meanings}. Strokes: ${radical.value.strokeCount}`
        })
      }
    ]
  })
</script>

<style lang="scss" scoped>
.font-jp {
  font-family: 'Noto Sans JP', sans-serif;
  font-weight: 500;
}

.radical-detail-page {
  background-color: rgb(var(--v-theme-background));
}

.radical-header {
  .display-char {
    line-height: 1;
    color: rgba(var(--v-theme-on-surface), 0.87);
  }
}

.gap-2 {
  gap: 0.5rem;
}

.animate-fade-in {
  animation: fadeIn 0.4s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.kanji-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 16px;
}

.reference-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 12px;
}

.sidebar-grid {
  grid-template-columns: 1fr;
}

.ref-card {
  border-color: rgba(var(--v-border-color), 0.12) !important;
  background-color: rgba(var(--v-theme-surface-variant), 0.05);

  &:hover {
    border-color: rgba(var(--v-theme-primary), 0.3) !important;
    background-color: rgba(var(--v-theme-primary), 0.02);
  }

  .entry-value {
    color: rgb(var(--v-theme-on-surface));
    letter-spacing: 0.5px;
  }
}

.variant-chip {
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.15);
  }
}

.line-height-1 {
  line-height: 1;
}
</style>
