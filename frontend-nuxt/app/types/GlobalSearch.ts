import type { KanjiResponse } from '@/types/Kanji'
import type { ProperNounResponse } from '@/types/ProperNoun'
import type { VocabularyResponse } from '@/types/Vocabulary'

export interface GlobalSearchResponse {
  searchedTerms: string[]
  vocabularyResults: VocabularyResponse
  properNounResults: ProperNounResponse
  kanjiResults: KanjiResponse
}

export interface GlobalSearchCache {
  [key: string]: GlobalSearchResponse
}
