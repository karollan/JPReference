import type { VocabularyDetails } from '@/types/Vocabulary'
// Utilities
import { defineStore } from 'pinia'
import { VocabularyService } from '@/services/vocabulary.service'

export const useVocabularyStore = defineStore('vocabulary', () => {
  // State
  const vocabularyDetails = ref<VocabularyDetails | null>(null)
  const vocabularyDetailsCache = reactive<{ [term: string]: VocabularyDetails }>({})
  const loading = ref<boolean>(false)
  const error = ref<string | null>(null)

  // Utils

  // Actions
  const getVocabularyDetails = async (
    term: string,
  ) => {
    const existing = vocabularyDetailsCache[term]
    if (existing) {
      return existing
    }
    loading.value = true
    error.value = null

    try {
      const response = await VocabularyService.getVocabularyDetails(term)

      // Validate response structure
      if (!response) {
        throw new Error('Vocabulary not found')
      }

      // Cache hygiene
      if (Object.keys(vocabularyDetailsCache).length > 10) {
        delete vocabularyDetailsCache[Object.keys(vocabularyDetailsCache)[0]!]
      }

      // Cache the response
      vocabularyDetailsCache[term] = response
      vocabularyDetails.value = response
      return response
    } catch (error_: any) {
      console.error('VocabularyStore error:', error_)
      error.value = error_.message ?? 'Failed to fetch vocabulary'
      vocabularyDetails.value = null
    } finally {
      loading.value = false
    }
  }

  // Getters

  return {
    // Expose state, actions, getters here
    vocabularyDetails,
    vocabularyDetailsCache,
    error,
    loading,
    getVocabularyDetails,
  }
})
