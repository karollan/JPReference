import type { KanjiDetails } from '@/types/Kanji'
import { getApiUrl } from './api'

export const KanjiService = {
  async fetchKanjiByLiteral(literal: string): Promise<KanjiDetails> {
    const data = await $fetch<KanjiDetails>(`${getApiUrl()}/kanji/${literal}`)
    return data
  },
}
