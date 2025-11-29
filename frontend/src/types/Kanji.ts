import type { PaginationMetadata } from './Common'
import type { RadicalSummary } from './Radical'

export interface KanjiResponse {
  data: KanjiSummary[]
  pagination: PaginationMetadata
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

export interface KanjiListCache {
  [key: string]: {
    pages: {
      [page: number]: KanjiSummary[]
    }
    totalCount: number
    hasMorePages: boolean
  }
}
