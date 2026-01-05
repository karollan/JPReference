import axios from 'axios'
import type { RadicalSearchResult, RadicalSummary, RadicalDetails } from '@/types/Radical'

const API_URL = import.meta.env.VITE_API_URL || '/api'

export class RadicalService {
    async getRadicalsList(): Promise<RadicalSummary[]> {
        const { data } = await axios.get(`${API_URL}/radical/list`)
        return data
    }

    async searchKanjiByRadicals(radicalIds: string[]): Promise<RadicalSearchResult> {
        const { data } = await axios.post(`${API_URL}/radical/search`, radicalIds)
        return data
    }

    async fetchRadicalByLiteral(literal: string): Promise<RadicalDetails> {
        const { data } = await axios.get(`${API_URL}/radical/${literal}`)
        return data
    }
}

export default new RadicalService()