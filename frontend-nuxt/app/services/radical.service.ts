import type { RadicalSearchResult, RadicalSummary, RadicalDetails } from '@/types/Radical'
import { getApiUrl } from './api'

export class RadicalService {
    async getRadicalsList(): Promise<RadicalSummary[]> {
        const data = await $fetch<RadicalSummary[]>(`${getApiUrl()}/radical/list`)
        return data
    }

    async searchKanjiByRadicals(radicalIds: string[]): Promise<RadicalSearchResult> {
        const data = await $fetch<RadicalSearchResult>(`${getApiUrl()}/radical/search`, {
            method: 'POST',
            body: radicalIds
        })
        return data
    }

    async fetchRadicalByLiteral(literal: string): Promise<RadicalDetails> {
        const data = await $fetch<RadicalDetails>(`${getApiUrl()}/radical/${literal}`)
        return data
    }
}

export default new RadicalService()