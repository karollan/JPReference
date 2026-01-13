import type { VocabularyDetails } from '@/types/Vocabulary'
import { getApiUrl } from './api'

export const VocabularyService = {
  async getVocabularyDetails(term: string): Promise<VocabularyDetails> {
    const data = await $fetch<VocabularyDetails>(`${getApiUrl()}/Vocabulary/${term}`)
    return data
  },
}
