import type { KanjiDetails } from '@/types/Kanji'
import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api'

export const KanjiService = {
  async fetchKanjiByLiteral (literal: string): Promise<KanjiDetails> {
    const { data } = await axios.get(`${API_URL}/kanji/${literal}`)
    return data
  },
}
