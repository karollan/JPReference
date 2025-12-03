import type { VocabularyDetails } from '@/types/Vocabulary'
import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api'

export const VocabularyService = {
  async getVocabularyDetails (term: string): Promise<VocabularyDetails> {
    const response = await axios.get<VocabularyDetails>(`${API_URL}/vocabulary/${term}`)
    return response.data
  },
}
