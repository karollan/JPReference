import type { ProperNounDetails } from '@/types/ProperNoun'
// Utilities
import { defineStore } from 'pinia'
import { ProperNounService } from '@/services/proper-noun.service'

export const useProperNounStore = defineStore('proper-noun', () => {
  // State
  const properNounDetails = ref<ProperNounDetails | null>(null)
  const properNounDetailsCache = reactive<{ [term: string]: ProperNounDetails }>({})
  const loading = ref<boolean>(false)
  const error = ref<string | null>(null)

  // Utils

  // Actions
  const getProperNounDetails = async (
    term: string,
  ) => {
    const foundProperNoun = getProperNounCache(term)
    if (foundProperNoun) {
      properNounDetails.value = foundProperNoun
      return foundProperNoun
    }
    loading.value = true
    error.value = null

    try {
      const response = await ProperNounService.getProperNounDetails(term)

      // Validate response structure
      if (!response) {
        throw new Error('Proper noun not found')
      }

      // Cache hygiene
      if (Object.keys(properNounDetailsCache).length > 10) {
        delete properNounDetailsCache[Object.keys(properNounDetailsCache)[0]!]
      }

      // Cache the response
      properNounDetailsCache[term] = response
      properNounDetails.value = response
      return response
    } catch (error_: any) {
      console.error('ProperNounStore error:', error_)
      error.value = error_.message ?? 'Failed to fetch proper noun'
      properNounDetails.value = null
    } finally {
      loading.value = false
    }
  }

  // Getters
  const getProperNounCache = (term: string) => {
    return properNounDetailsCache[term]
  }

  return {
    // Expose state, actions, getters here
    properNounDetails,
    properNounDetailsCache,
    error,
    loading,
    getProperNounDetails,
  }
}, { persist: true })
