<template>
  <v-dialog
    v-model="dialog"
    :fullscreen="isMobile"
    :max-width="isMobile ? undefined : '900px'"
    scrollable
  >
    <template #activator="{ props: activatorProps }">
      <v-tooltip location="bottom" :persistent="false">
        <template #activator="{ props: tooltipProps }">
          <v-btn
            v-bind="{ ...activatorProps, ...tooltipProps }"
            aria-label="Search Guide"
            color="primary"
            icon
            size="x-small"
            slim
            variant="text"
          >
            <v-icon size="18">mdi-help</v-icon>
          </v-btn>
        </template>
        <span>Search Guide</span>
      </v-tooltip>
    </template>

    <v-card>
      <v-card-title class="d-flex justify-space-between align-center pa-4">
        <div class="d-flex align-center">
          <v-icon class="mr-2" color="primary" size="28">mdi-book-open-variant</v-icon>
          <span class="text-h5">Search Guide</span>
        </div>
        <v-btn
          icon
          variant="text"
          @click="dialog = false"
        >
          <v-icon>mdi-close</v-icon>
        </v-btn>
      </v-card-title>

      <v-divider />

      <v-card-text class="pa-4">
        <v-expansion-panels variant="accordion">
          <!-- Overview -->
          <v-expansion-panel>
            <v-expansion-panel-title>
              <div class="d-flex align-center">
                <v-icon class="mr-3" color="primary">mdi-information</v-icon>
                <span class="font-weight-medium">Overview</span>
              </div>
            </v-expansion-panel-title>
            <v-expansion-panel-text>
              <p class="mb-3">
                This search engine supports multiple search modes and powerful filtering capabilities to help you find exactly what you're looking for.
              </p>
              <p>
                You can search in Japanese (kanji, hiragana, katakana), romaji, or English. The engine automatically transliterates romaji input and ranks results by relevance, JLPT level, and frequency.
              </p>
            </v-expansion-panel-text>
          </v-expansion-panel>

          <!-- Basic Search -->
          <v-expansion-panel>
            <v-expansion-panel-title>
              <div class="d-flex align-center">
                <v-icon class="mr-3" color="primary">mdi-magnify</v-icon>
                <span class="font-weight-medium">Basic Search</span>
              </div>
            </v-expansion-panel-title>
            <v-expansion-panel-text>
              <h3 class="text-subtitle-1 font-weight-bold mb-2">Direct Matching</h3>
              <p class="mb-3">
                Search directly in Japanese (kanji, hiragana, katakana), romaji, or English. The search engine will find matching entries across all forms and meanings.
              </p>

              <h3 class="text-subtitle-1 font-weight-bold mb-2">Automatic Transliteration</h3>
              <p class="mb-2">
                When you type in romaji, the search engine automatically converts it to hiragana and katakana variants:
              </p>
              <div class="example-box mb-3">
                <code>taberu</code> → matches <code>たべる</code>, <code>タベル</code>
              </div>
              <v-alert type="info" variant="tonal" class="mb-3">
                <strong>Note:</strong> Transliteration is disabled when using wildcards or quotes.
              </v-alert>

              <h3 class="text-subtitle-1 font-weight-bold mb-2">Multiple Words</h3>
              <p class="mb-2">
                Space-separated words are searched independently. All words must match (AND logic):
              </p>
              <div class="example-box mb-3">
                <code>eat food</code> → finds entries containing both "eat" AND "food"
              </div>

              <h3 class="text-subtitle-1 font-weight-bold mb-2">Phrase Search</h3>
              <p class="mb-2">
                Use double quotes to search for exact multi-word phrases, including phrases with spaces:
              </p>
              <div class="example-box mb-1">
                <code>"お はよう"</code> → finds only entries with space between characters
              </div>
            </v-expansion-panel-text>
          </v-expansion-panel>

          <!-- Wildcards -->
          <v-expansion-panel>
            <v-expansion-panel-title>
              <div class="d-flex align-center">
                <v-icon class="mr-3" color="primary">mdi-asterisk</v-icon>
                <span class="font-weight-medium">Wildcards</span>
              </div>
            </v-expansion-panel-title>
            <v-expansion-panel-text>
              <h3 class="text-subtitle-1 font-weight-bold mb-2">Single Character (<code>?</code>)</h3>
              <p class="mb-2">
                The <code>?</code> wildcard matches exactly one character:
              </p>
              <div class="example-box mb-3">
                <code>た?る</code> → matches <code>たべる</code>, <code>たける</code>, etc.
              </div>

              <h3 class="text-subtitle-1 font-weight-bold mb-2">Multiple Characters (<code>*</code>)</h3>
              <p class="mb-2">
                The <code>*</code> wildcard matches zero or more characters:
              </p>
              <div class="example-box mb-3">
                <code>食*</code> → matches <code>食べる</code>, <code>食事</code>, <code>食</code>, etc.
              </div>

              <v-alert type="warning" variant="tonal">
                <strong>Important:</strong> Using wildcards disables automatic transliteration. You must type Japanese characters directly.
              </v-alert>
            </v-expansion-panel-text>
          </v-expansion-panel>

          <!-- Advanced Filters -->
          <v-expansion-panel>
            <v-expansion-panel-title>
              <div class="d-flex align-center">
                <v-icon class="mr-3" color="primary">mdi-pound</v-icon>
                <span class="font-weight-medium">Advanced Filters (Tags)</span>
              </div>
            </v-expansion-panel-title>
            <v-expansion-panel-text>
              <h3 class="text-subtitle-1 font-weight-bold mb-2">Syntax</h3>
              <p class="mb-2">
                Filters use the format: <code>#filterKey:value</code>
              </p>

              <h3 class="text-subtitle-1 font-weight-bold mb-2 mt-4">Boolean Filters</h3>
              <p class="mb-2">
                These filters don't require a value. Just type the filter name:
              </p>
              <div class="example-box mb-3">
                <code>#common</code> → Only common words<br>
                <code>#abbr</code> → Only abbreviations<br>
                <code>#sl</code> → Only slang
              </div>

              <h3 class="text-subtitle-1 font-weight-bold mb-2">Range Filters</h3>
              <p class="mb-2">
                Use a single value or a range with a dash:
              </p>
              <div class="example-box mb-3">
                <code>#jlpt:3</code> → JLPT N3 only<br>
                <code>#jlpt:3-5</code> → JLPT N3, N4, and N5<br>
                <code>#stroke:5</code> → Exactly 5 strokes<br>
                <code>#stroke:1-10</code> → 1 to 10 strokes
              </div>

              <h3 class="text-subtitle-1 font-weight-bold mb-2">Language Filter</h3>
              <p class="mb-2">
                Filter results by meaning language:
              </p>
              <div class="example-box mb-3">
                <code>#lang:eng</code> → English meanings<br>
                <code>#lang:ger</code> → German meanings<br>
                <code>#lang:fre</code> → French meanings
              </div>

              <h3 class="text-subtitle-1 font-weight-bold mb-2">Common Filters</h3>
              <div class="filter-categories mb-3">
                <div class="mb-2">
                  <strong>JLPT Level:</strong> <code>#jlpt:1-5</code>
                </div>
                <div class="mb-2">
                  <strong>Stroke Count:</strong> <code>#stroke:1-84</code>
                </div>
                <div class="mb-2">
                  <strong>Grade Level:</strong> <code>#grade:1-12</code>
                </div>
                <div class="mb-2">
                  <strong>Frequency:</strong> <code>#freq:0-10000</code>
                </div>
                <div class="mb-2">
                  <strong>Part of Speech:</strong> <code>#v1</code> (ichidan verb), <code>#adj-i</code> (i-adjective), <code>#n</code> (noun), and 300+ more
                </div>
              </div>

              <h3 class="text-subtitle-1 font-weight-bold mb-2">Autocomplete</h3>
              <p class="mb-2">
                Start typing <code>#</code> in the search bar to see available filters with descriptions.
              </p>

              <h3 class="text-subtitle-1 font-weight-bold mb-2">Combining Filters</h3>
              <p class="mb-2">
                Use multiple filters together to refine your search:
              </p>
              <div class="example-box mb-1">
                <code>eat #jlpt:3 #common</code> → Common JLPT N3 words meaning "eat"
              </div>
            </v-expansion-panel-text>
          </v-expansion-panel>

          <!-- Search Ranking -->
          <v-expansion-panel>
            <v-expansion-panel-title>
              <div class="d-flex align-center">
                <v-icon class="mr-3" color="primary">mdi-sort</v-icon>
                <span class="font-weight-medium">How Results Are Ranked</span>
              </div>
            </v-expansion-panel-title>
            <v-expansion-panel-text>
              <p class="mb-3">
                Search results are ranked using multiple factors to show the most relevant entries first:
              </p>

              <div class="ranking-factor mb-3">
                <h4 class="text-subtitle-2 font-weight-bold">1. Exact Matches</h4>
                <p>Entries that exactly match your search term are prioritized.</p>
              </div>

              <div class="ranking-factor mb-3">
                <h4 class="text-subtitle-2 font-weight-bold">2. Common Words</h4>
                <p>Words marked as "common" (green badge) rank higher as they are more frequently used.</p>
              </div>

              <div class="ranking-factor mb-3">
                <h4 class="text-subtitle-2 font-weight-bold">3. JLPT Level</h4>
                <p>Lower JLPT levels (N5, N4, N3) are ranked higher than advanced levels (N2, N1).</p>
              </div>

              <div class="ranking-factor mb-3">
                <h4 class="text-subtitle-2 font-weight-bold">4. Frequency</h4>
                <p>More frequently used words rank higher in results.</p>
              </div>

              <div class="ranking-factor mb-3">
                <h4 class="text-subtitle-2 font-weight-bold">5. Text Similarity</h4>
                <p>The search engine uses PostgreSQL full-text search with trigram similarity to find partial matches and rank them by relevance.</p>
              </div>

              <div class="ranking-factor mb-1">
                <h4 class="text-subtitle-2 font-weight-bold">6. Multi-word Searches</h4>
                <p>When searching multiple words, all terms must match. Results are ranked by combined relevance across all matched terms.</p>
              </div>
            </v-expansion-panel-text>
          </v-expansion-panel>
        </v-expansion-panels>
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  import { ref } from 'vue'
  import { useDisplay } from 'vuetify'

  const { xs, sm } = useDisplay()
  const dialog = ref(false)
  const isMobile = computed(() => xs.value || sm.value)
</script>

<style scoped lang="scss">
.example-box {
  background-color: rgba(var(--v-theme-surface-variant), 0.3);
  border-left: 3px solid rgb(var(--v-theme-primary));
  padding: 12px 16px;
  border-radius: 4px;
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 0.9rem;
  line-height: 1.6;

  code {
    background-color: transparent;
    padding: 0;
    font-size: inherit;
  }
}

code {
  background-color: rgba(var(--v-theme-on-surface), 0.12);
  padding: 2px 6px;
  border-radius: 3px;
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 0.85em;
  color: rgb(var(--v-theme-on-surface));
  font-weight: 600;
}

.filter-categories {
  background-color: rgba(var(--v-theme-surface-variant), 0.2);
  padding: 12px 16px;
  border-radius: 4px;
}

.ranking-factor {
  h4 {
    color: rgb(var(--v-theme-primary));
    margin-bottom: 4px;
  }

  p {
    color: rgba(var(--v-theme-on-surface), 0.8);
    margin: 0;
  }
}
</style>
