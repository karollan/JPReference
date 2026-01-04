import type { RadicalDetails } from '@/types/Radical'
import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api'

export const RadicalService = {
    async fetchRadicalByLiteral(literal: string): Promise<RadicalDetails> {
        const { data } = await axios.get(`${API_URL}/radical/${literal}`)
        return data
    },
}
