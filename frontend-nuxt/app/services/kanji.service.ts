import type { KanjiDetails } from '@/types/Kanji'

export const useKanjiService = () => {
  const config = useRuntimeConfig()

  const baseUrl = import.meta.server ? config.apiUrl : config.public.apiUrl
  return {
    fetchKanjiByLiteral: (literal: string) => $fetch<KanjiDetails>(`${baseUrl}/kanji/${encodeURIComponent(literal)}`)
  }
}