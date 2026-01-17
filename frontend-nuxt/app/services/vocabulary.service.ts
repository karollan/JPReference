import type { VocabularyDetails } from '@/types/Vocabulary'

export const useVocabularyService = () => {
  const config = useRuntimeConfig()

  const baseUrl = import.meta.server ? config.apiUrl : config.public.apiUrl

  return {
    getVocabularyDetails: (term: string) => $fetch<VocabularyDetails>(`${baseUrl}/vocabulary/${encodeURIComponent(term)}`)
  }
}