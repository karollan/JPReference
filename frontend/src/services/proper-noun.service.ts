import type { ProperNounDetails } from '@/types/ProperNoun'
import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api'

export const ProperNounService = {
  async getProperNounDetails (term: string): Promise<ProperNounDetails> {
    const response = await axios.get<ProperNounDetails>(`${API_URL}/proper-noun/${term}`)
    return response.data
  },
}
