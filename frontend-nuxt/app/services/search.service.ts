import type { GlobalSearchResponse } from '@/types/GlobalSearch'

export const useSearchService = () => {
  const config = useRuntimeConfig()

  const baseUrl = import.meta.server ? config.apiUrl : config.public.apiUrl

  return {
    fetchGlobalSearch: (query: string, page = 1, pageSize = 50, signal?: AbortSignal) => $fetch<GlobalSearchResponse>(`${baseUrl}/Search`, {
      params: {
        query,
        page,
        pageSize
      },
      signal
    })
  }
}
