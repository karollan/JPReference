import type { GlobalSearchResponse } from '@/types/GlobalSearch'
import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api'

export const SearchService = {
  async fetchGlobalSearch (query: string, page = 1, pageSize = 50): Promise<GlobalSearchResponse> {
    const response = await axios.get<GlobalSearchResponse>(`${API_URL}/Search`, {
      params: {
        query,
        page,
        pageSize,
      },
      paramsSerializer: {
        indexes: null,
      },
    })
    return response.data
  },
}
