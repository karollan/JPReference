import type { GlobalSearchResponse } from '@/types/GlobalSearch'
import { getApiUrl } from './api'

export const SearchService = {
  async fetchGlobalSearch(query: string, page = 1, pageSize = 50, signal?: AbortSignal): Promise<GlobalSearchResponse> {
    const data = await $fetch<GlobalSearchResponse>(`${getApiUrl()}/Search`, {
      params: {
        query,
        page,
        pageSize,
      },
      signal,
    })
    return data
  },
}
