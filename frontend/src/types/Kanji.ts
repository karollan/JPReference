import type { PaginationMetadata } from './Common'
import type { RadicalSummary } from './Radical'
import type { VocabularySummary } from './Vocabulary'

export interface KanjiResponse {
  data: KanjiSummary[]
  pagination: PaginationMetadata
}

export interface KanjiDetails {
  id: string
  literal: string
  strokeCount: number
  frequency?: number
  grade?: number
  jlptLevel?: number
  readings: KanjiReading[] | null
  meanings: KanjiMeaning[] | null
  codepoints: KanjiCodepoint[] | null
  dictionaryReferences: KanjiDictionaryReference[] | null
  queryCodes: KanjiQueryCode[] | null
  nanori: KanjiNanori[] | null
  radicals: RadicalSummary[] | null
  vocabularyReferences: KanjiVocabulary | null
}

// Used to represent a summary of a kanji in search lists
export interface KanjiSummary {
  id: string
  relevanceScore: number
  literal: string
  strokeCount: number
  frequency?: number
  grade?: number
  jlptLevel?: number
  kunyomiReadings: KanjiReading[] | null
  onyomiReadings: KanjiReading[] | null
  meanings: KanjiMeaning[] | null
  radicals: RadicalSummary[] | null
}

interface KanjiReading {
  type: string
  value: string
  status?: string
  onType?: string
}

interface KanjiMeaning {
  language: string
  meaning: string
}

interface KanjiCodepoint {
  type: string
  value: string
}

interface KanjiQueryCode {
  type: string
  value: string
  skipMisclassification?: string
}

interface KanjiNanori {
  value: string
}

interface KanjiDictionaryReference {
  type: string
  value: string
  morohashiVolume?: number
  morohashiPage?: number
}

export interface KanjiListCache {
  [key: string]: {
    pages: {
      [page: number]: KanjiSummary[]
    }
    totalCount: number
    hasMorePages: boolean
  }
}

interface KanjiVocabulary {
  totalCount: number
  vocabulary: VocabularySummary[]
}
