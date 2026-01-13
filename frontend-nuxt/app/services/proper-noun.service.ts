import type { ProperNounDetails } from '@/types/ProperNoun'
import { getApiUrl } from './api'

export const ProperNounService = {
  async getProperNounDetails(term: string): Promise<ProperNounDetails> {
    const data = await $fetch<ProperNounDetails>(`${getApiUrl()}/ProperNoun/${term}`)
    return data
  },
}
