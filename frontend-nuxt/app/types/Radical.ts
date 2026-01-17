import type { KanjiSummary } from "./Kanji"

export interface RadicalSummary {
  id: string
  literal: string
  strokeCount: number
  hasDetails: boolean
}

export interface RadicalDetails {
  id: string
  literal: string
  strokeCount: number
  code: string | null
  kangXiNumber: number | null
  variants: RadicalGroupMember[] | null
  meanings: string[] | null
  readings: string[] | null
  notes: string[] | null
  kanji: KanjiSummary[] | null
  updatedAt: Date
}

export interface RadicalGroupMember {
  id: string
  literal: string
  kanji: KanjiSummary[] | null
}

export interface KanjiSimple {
  id: string
  literal: string
  strokeCount: number
}

export interface RadicalSearchResult {
  results: KanjiSimple[]
  compatibleRadicalIds: string[]
}
