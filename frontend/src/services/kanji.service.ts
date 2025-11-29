import type { KanjiResponse, KanjiSummary } from '@/types/Kanji'
import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api'

export const KanjiService = {
  async fetchKanjis (query: string | null, page = 1, pageSize = 50): Promise<KanjiResponse> {
    const response = await axios.get<KanjiResponse>(`${API_URL}/kanji`, {
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

  async fetchKanji (guid: string): Promise<KanjiSummary> {
    const { data } = await axios.get(`${API_URL}/kanji/${guid}`)
    return data
  },
}
