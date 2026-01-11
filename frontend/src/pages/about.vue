<template>
  <v-container class="about-page py-8">
    <!-- Hero Section -->
    <v-row justify="center" class="mb-4">
      <v-col cols="12" md="10" lg="8" class="text-center">
        <h1 class="text-h3 font-weight-bold mb-4 text-primary">
          About this project
        </h1>
        <p class="text-h6 text-medium-emphasis">
          A modern, comprehensive Japanese dictionary designed to help students master vocabulary and kanji.
        </p>
      </v-col>
    </v-row>

    <!-- Features Section -->
    <section class="mb-4">
      <h2 class="text-h4 font-weight-bold">
        Key Features
      </h2>
      <v-row>
        <v-col cols="12" md="6">
          <v-card variant="flat" class="bg-background pa-4 h-100">
            <v-card-text class="py-0">
              <v-list bg-color="transparent">
                <v-list-item v-for="feature in features" :key="feature.title" class="px-0">
                  <template v-slot:prepend>
                    <v-icon :icon="feature.icon" size="large" class="mr-4 text-primary" />
                  </template>
                  <v-list-item-title class="font-weight-bold">
                    {{ feature.title }}
                  </v-list-item-title>
                  <p class="mt-1 text-wrap text-medium-emphasis">
                    {{ feature.description }}
                  </p>
                </v-list-item>
              </v-list>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </section>

    <!-- Attributions Section -->
    <section class="mb-4">
      <h2 class="text-h4 font-weight-bold">
        Attributions & Resources
      </h2>
      
      <v-row>
        <!-- Data Sources -->
        <v-col cols="12" md="6">
          <v-card variant="flat" class="bg-background pa-4 h-100">
            <v-card-title class="d-flex align-center">
              <v-icon icon="mdi-database" start class="text-primary" />
              Dictionary Data
            </v-card-title>
            <v-card-text>
              <v-list bg-color="transparent">
                <v-list-item v-for="source in dataSources" :key="source.name" class="px-0">
                  <template v-slot:prepend>
                    <v-icon icon="mdi-book-open-page-variant" size="small" class="mr-3" />
                  </template>
                  <v-list-item-title class="font-weight-bold">
                    <a
                      v-if="source.link"
                      :href="source.link"
                      target="_blank"
                      rel="noopener noreferrer"
                      class="text-decoration-none text-high-emphasis"
                    >
                      {{ source.name }}
                    </a>
                    <span v-else>{{ source.name }}</span>
                  </v-list-item-title>
                  <p class="text-caption text-medium-emphasis mt-1 text-wrap">
                    {{ source.description }}
                  </p>
                  <div v-if="source.license_link" class="mt-1 text-caption text-medium-emphasis">
                    License: <a :href="source.license_link" target="_blank" rel="noopener noreferrer" class="text-decoration-none text-high-emphasis">{{ source.license }}</a>
                  </div>
                  <div v-else class="mt-2 text-caption text-medium-emphasis">
                    License: {{ source.license }}
                  </div>
                </v-list-item>
              </v-list>
            </v-card-text>
          </v-card>
        </v-col>

        <!-- Tech Stack -->
        <v-col cols="12" md="6">
          <v-card variant="flat" class="bg-background pa-4 h-100">
            <v-card-title class="d-flex align-center">
              <v-icon icon="mdi-code-tags" start class="text-primary" />
              Technology Stack
            </v-card-title>
            <v-card-text>
              <div class="d-flex flex-wrap gap-2 mt-2">
                <v-chip
                  v-for="tech in techStack"
                  :key="tech"
                  color="secondary"
                  variant="outlined"
                  class="mr-2 mb-2"
                >
                  {{ tech }}
                </v-chip>
              </div>
              
              <div class="mt-8">
                <h4 class="text-subtitle-1 font-weight-bold mb-2">Libraries and projects</h4>
                <v-list density="compact" bg-color="transparent">
                  <v-list-item v-for="lib in libraries" :key="lib.name" class="px-0 min-height-dense">
                    <v-list-item-title>
                      <a
                        v-if="lib.link"
                        :href="lib.link"
                        target="_blank"
                        rel="noopener noreferrer"
                        class="text-decoration-none text-high-emphasis font-weight-medium"
                      >
                        {{ lib.name }}
                      </a>
                      <span v-else class="font-weight-medium">{{ lib.name }}</span>
                      <span class="text-caption text-medium-emphasis ml-2 text-wrap">- {{ lib.purpose }}</span>
                      <a v-if="lib.licenseLink" :href="lib.licenseLink" target="_blank" rel="noopener noreferrer" class="text-caption text-decoration-none text-medium-emphasis ml-2">- {{ lib.license }}</a>
                      <span v-else class="text-caption text-medium-emphasis ml-2">{{ lib.license }}</span>
                    </v-list-item-title>
                  </v-list-item>
                </v-list>
              </div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </section>

    <div v-if="lastUpdate" class="db-update-info text-caption text-medium-emphasis">
      Last database update: {{ formattedLastUpdate }}
    </div>
  </v-container>
</template>

<script setup lang="ts">
import type { Status } from '@/types/Status'
import { useHead } from '@unhead/vue'
import { StatusService } from '@/services/status.service'

const features = [
  {
    title: 'Smart Search',
    description: 'Instantly find vocabulary and kanji with the intelligent search engine that supports romaji, kana, tags and multiple languages.',
    icon: 'mdi-magnify'
  },
  {
    title: 'Stroke Orders',
    description: 'Learn proper writing techniques with animated stroke order diagrams for thousands of kanji and vocabulary.',
    icon: 'mdi-brush'
  },
  {
    title: 'Radical Lookup',
    description: 'Find elusive kanji by identifying their component radicals, just like a traditional dictionary.',
    icon: 'mdi-puzzle'
  }
]

const dataSources = [
  {
    name: 'JMdict',
    description: 'A comprehensive Japanese-Multilingual Dictionary file by the Electronic Dictionary Research and Development Group (EDRDG).',
    license: 'Creative Commons Attribution-ShareAlike 4.0 (CC BY-SA 4.0)',
    license_link: 'https://creativecommons.org/licenses/by-sa/4.0/',
    link: 'https://www.edrdg.org/jmdict/j_jmdict.html'
  },
  {
    name: 'KANJIDIC2',
    description: 'Detailed information on over 13,000 kanji characters by the Electronic Dictionary Research and Development Group (EDRDG).',
    license: 'Creative Commons Attribution-ShareAlike 4.0 (CC BY-SA 4.0)',
    license_link: 'https://creativecommons.org/licenses/by-sa/4.0/',
    link: 'https://www.edrdg.org/wiki/index.php/KANJIDIC_Project'
  },
  {
    name: 'RADKFILE/KRADFILE',
    description: 'Radical data for kanji characters by the Electronic Dictionary Research and Development Group (EDRDG).',
    license: 'Creative Commons Attribution-ShareAlike 4.0 (CC BY-SA 4.0)',
    license_link: 'https://creativecommons.org/licenses/by-sa/4.0/',
    link: 'https://www.edrdg.org/krad/kradinf.html'
  },
  {
    name: 'KanjiVG',
    description: 'Stroke order data for kanji characters.',
    license: 'Creative Commons Attribution-ShareAlike 3.0 (CC BY-SA 3.0)',
    license_link: 'https://creativecommons.org/licenses/by-sa/3.0/',
    link: 'https://github.com/KanjiVG/kanjivg'
  },
  {
    name: 'JmdictFurigana',
    description: 'Furigana data for kanji characters.',
    license: 'MIT',
    license_link: 'http://github.com/Doublevil/JmdictFurigana?tab=MIT-1-ov-file',
    link: 'https://github.com/Doublevil/JmdictFurigana'
  },
  {
    name: 'kanji-data',
    description: 'Additional kanji data for kanji characters.',
    license: 'MIT',
    license_link: 'https://github.com/davidluzgouveia/kanji-data/tree/master?tab=MIT-1-ov-file',
    link: 'https://github.com/davidluzgouveia/kanji-data/tree/master'
  },
  {
    name: 'kanjium',
    description: 'Enhancement for radical data.',
    license: 'Creative Commons Attribution-ShareAlike 4.0 (CC BY-SA 4.0)',
    license_link: 'https://creativecommons.org/licenses/by-sa/4.0/',
    link: 'https://github.com/mifunetoshiro/kanjium/tree/master'
  },
  {
    name: 'JLPT vocabulary by level',
    description: 'JLPT level for vocabulary data by Robin Pourtaud',
    license: 'Creative Commons Attribution-ShareAlike 4.0 (CC BY-SA 4.0)',
    license_link: 'https://creativecommons.org/licenses/by-sa/4.0/',
    link: 'https://www.kaggle.com/datasets/robinpourtaud/jlpt-words-by-level'
  }
]

const techStack = [
  'Vue 3',
  'TypeScript',
  'Vuetify 3',
  'Pinia',
  '.NET 8',
  'PostgreSQL',
  'Docker'
]

const libraries = [
  { name: 'vue-dmak', purpose: 'Kanji stroke animations', link: 'https://github.com/karollan/vue-dmak' },
  { name: 'jmdict-simplified', purpose: 'EDRDG XML to JSON parser', link: 'https://github.com/scriptin/jmdict-simplified', license: 'Creative Commons Attribution-ShareAlike 4.0 (CC BY-SA 4.0)', licenseLink: 'https://creativecommons.org/licenses/by-sa/4.0/'},
  { name: 'WanaKana-net', purpose: 'Transliteriation of Hiragana, Katakana and Romaji', link: 'https://github.com/MartinZikmund/WanaKana-net/tree/dev'}
]

useHead({
  title: 'About - JP Reference',
  meta: [
    {
      name: 'description',
      content: 'Learn more about the JP Reference project, its features, and the dictionary data sources used.'
    }
  ]
})
const lastUpdate = ref<string | null>(null)

const formattedLastUpdate = computed(() => {
  if (!lastUpdate.value) return 'Unknown'
  return new Date(lastUpdate.value).toLocaleString()
})

onMounted(async () => {
  try {
    lastUpdate.value = (await StatusService.getDatabaseStatus()).lastUpdate
  } catch (error) {
    console.error('Failed to fetch database status', error)
  }
})
</script>

<style scoped>
.min-height-dense {
  min-height: 32px !important;
}

h2 {
  text-align: center;
}

.db-update-info {
  position: fixed;
  bottom: calc(var(--v-layout-bottom) + 8px);
  right: 8px;
  pointer-events: none; /* Let clicks pass through if needed, though it's text */
  user-select: none;
  z-index: 100;
  opacity: 0.7;
}
</style>
