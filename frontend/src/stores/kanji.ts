import type { KanjiDetails } from '@/types/Kanji'
// Utilities
import { defineStore } from 'pinia'
import { KanjiService } from '@/services/kanji.service'

export const useKanjiStore = defineStore('kanji', () => {
  // State
  const kanjiDetails = ref<KanjiDetails | null>(null)
  const kanjiDetailsCache = reactive<{ [literal: string]: KanjiDetails }>({})
  const loading = ref<boolean>(false)
  const error = ref<string | null>(null)

  // Utils

  // Actions
  const getKanjiByLiteral = async (
    literal: string,
  ) => {
    const foundKanji = getKanjiCache(literal)
    if (foundKanji) {
      kanjiDetails.value = foundKanji
      return foundKanji
    }
    loading.value = true
    error.value = null

    try {
      const response = await KanjiService.fetchKanjiByLiteral(literal)

      // Validate response structure
      if (!response) {
        throw new Error('Kanji not found')
      }

      // Cache hygiene
      if (Object.keys(kanjiDetailsCache).length > 10) {
        delete kanjiDetailsCache[Object.keys(kanjiDetailsCache)[0]!]
      }

      // Cache the response
      kanjiDetailsCache[literal] = response
      kanjiDetails.value = response
      return response
    } catch (error_: any) {
      console.error('KanjiStore error:', error_)
      error.value = error_.message ?? 'Failed to fetch kanji'
      kanjiDetails.value = null
    } finally {
      loading.value = false
    }
  }

  // Getters
  const getKanjiCache = (literal: string) => {
    return kanjiDetailsCache[literal]
  }

  return {
    // Expose state, actions, getters here
    kanjiDetails,
    kanjiDetailsCache,
    error,
    loading,
    getKanjiByLiteral,
  }
}, { persist: true })
