import type { ProperNounDetails } from '@/types/ProperNoun'

export const useProperNounService = () => {
  const config = useRuntimeConfig()

  const baseUrl = import.meta.server ? config.apiUrl : config.public.apiUrl;

  return {
    getProperNounDetails: (term: string) => $fetch<ProperNounDetails>(`${baseUrl}/ProperNoun/${encodeURIComponent(term)}`)
  }
}
