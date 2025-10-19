<template>
  <v-card
    class="kanji-card elevation-2 ma-2"
    :class="{ 'kanji-card--hover': true }"
    height="100%"
    @click="onCardClick"
  >
    <v-card-text class="text-center pa-4">
      <!-- Kanji Character -->
      <div class="kanji-character mb-3">
        <span class="text-h3 font-weight-bold primary--text">{{ kanji.character }}</span>
      </div>

      <!-- JLPT Level Badge -->
      <v-chip
        v-if="kanji.jlptNew"
        :color="getJllptColor(kanji.jlptNew)"
        size="small"
        class="mb-2"
      >
        N{{ kanji.jlptNew }}
      </v-chip>

      <!-- Meanings -->
      <div class="meanings mb-3">
        <div class="text-caption text-grey-darken-1 mb-1">Meanings</div>
        <div class="text-body-2 font-weight-medium" :title="kanji.meanings.join(', ')">
          {{ truncateText(kanji.meanings.join(', '), 50) }}
        </div>
      </div>

      <!-- Readings -->
      <v-row no-gutters class="readings">
        <!-- On Readings -->
        <v-col cols="6" class="pr-1">
          <div class="text-caption text-grey-darken-1 mb-1">On</div>
          <div class="text-body-2" :title="kanji.readingsOn?.join(', ') || 'No on readings'">
            {{ kanji.readingsOn?.length ? truncateText(kanji.readingsOn.join(', '), 20) : '-' }}
          </div>
        </v-col>
        
        <!-- Kun Readings -->
        <v-col cols="6" class="pl-1">
          <div class="text-caption text-grey-darken-1 mb-1">Kun</div>
          <div class="text-body-2" :title="kanji.readingsKun?.join(', ') || 'No kun readings'">
            {{ kanji.readingsKun?.length ? truncateText(kanji.readingsKun.join(', '), 20) : '-' }}
          </div>
        </v-col>
      </v-row>

      <!-- Additional Info -->
      <div class="additional-info mt-3">
        <!-- Stroke Count -->
        <div v-if="kanji.strokeCount" class="stroke-count mb-1">
          <v-icon size="16" class="mr-1">mdi-pencil</v-icon>
          <span class="text-caption">{{ kanji.strokeCount }} strokes</span>
        </div>
        
        <!-- Grade -->
        <div v-if="kanji.grade" class="grade mb-1">
          <v-icon size="16" class="mr-1">mdi-school</v-icon>
          <span class="text-caption">Grade {{ kanji.grade }}</span>
        </div>
        
        <!-- Frequency -->
        <div v-if="kanji.frequency" class="frequency mb-1">
          <v-icon size="16" class="mr-1">mdi-chart-line</v-icon>
          <span class="text-caption">Freq: {{ kanji.frequency }}</span>
        </div>
        
        <!-- Radicals -->
        <div v-if="kanji.radicals && kanji.radicals.length" class="radicals mb-1">
          <v-icon size="16" class="mr-1">mdi-puzzle</v-icon>
          <span class="text-caption">{{ kanji.radicals.slice(0, 3).join(', ') }}{{ kanji.radicals.length > 3 ? '...' : '' }}</span>
        </div>
        
        <!-- Nanori -->
        <div v-if="kanji.nanori && kanji.nanori.length" class="nanori mb-1">
          <v-icon size="16" class="mr-1">mdi-account</v-icon>
          <span class="text-caption">{{ kanji.nanori.slice(0, 2).join(', ') }}{{ kanji.nanori.length > 2 ? '...' : '' }}</span>
        </div>
      </div>
    </v-card-text>
  </v-card>
</template>

<script lang="ts" setup>
import type { Kanji } from '@/types/Kanji'

interface Props {
  kanji: Kanji
}

const props = defineProps<Props>()
const router = useRouter()

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

const truncateText = (text: string, maxLength: number) => {
  if (text.length <= maxLength) return text
  return text.substring(0, maxLength) + '...'
} 

const onCardClick = () => {
  router.push(`/kanji/${props.kanji.id}`)
}
</script>

<style lang="scss" scoped>
.kanji-card {
  transition: all 0.3s ease;
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  
  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
  }
}

.kanji-character {
  line-height: 1;
  min-height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.meanings {
  min-height: 48px;
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
}

.readings {
  min-height: 48px;
}

.additional-info {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
}

.stroke-count,
.grade,
.frequency,
.radicals,
.nanori {
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
