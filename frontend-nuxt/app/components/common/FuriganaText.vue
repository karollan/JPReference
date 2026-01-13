<template>
  <span class="furigana-text">
    <template v-if="displaySegments.length > 0">
      <template v-for="(segment, idx) in displaySegments" :key="idx">
        <ruby v-if="segment.rt">
          {{ segment.ruby }}<rt>{{ segment.rt }}</rt>
        </ruby>
        <template v-else>
          {{ segment.ruby }}
        </template>
      </template>
    </template>
    <template v-else>
      {{ text }}
    </template>
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Furigana, FuriganaPart } from '@/types/Common'

const props = defineProps<{
  text: string
  reading?: string | null
  furigana?: Furigana[] | null
}>()

const displaySegments = computed<FuriganaPart[]>(() => {
  // 1. Try to find explicit furigana data match
  if (props.furigana && props.reading) {
    const match = props.furigana.find(f => 
      f.text === props.text && (f.reading === props.reading || !f.reading)
    )
    if (match && match.furigana.length > 0) {
      return match.furigana
    }
  }

  // 2. If no reading provided, just return text
  if (!props.reading) {
    return [{ ruby: props.text }]
  }

  // 3. Smart Fallback: Strip common prefixes/suffixes
  // This helps with cases like "テスタデルガルガノ岬" where prefix matches
  const text = props.text
  const reading = props.reading

  // Find common prefix
  let prefixLen = 0
  const minLen = Math.min(text.length, reading.length)
  while (prefixLen < minLen && text[prefixLen] === reading[prefixLen]) {
    prefixLen++
  }

  // Find common suffix
  let suffixLen = 0
  // Don't overlap with prefix
  while (suffixLen < (minLen - prefixLen) && 
         text[text.length - 1 - suffixLen] === reading[reading.length - 1 - suffixLen]) {
    suffixLen++
  }

  const segments: FuriganaPart[] = []

  // Add Prefix
  if (prefixLen > 0) {
    segments.push({ ruby: text.substring(0, prefixLen) })
  }

  // Add Middle (Ruby part)
  const midText = text.substring(prefixLen, text.length - suffixLen)
  const midReading = reading.substring(prefixLen, reading.length - suffixLen)
  
  if (midText.length > 0 || midReading.length > 0) {
    segments.push({ 
      ruby: midText, 
      rt: midReading 
    })
  }

  // Add Suffix
  if (suffixLen > 0) {
    segments.push({ ruby: text.substring(text.length - suffixLen) })
  }

  return segments
})
</script>