import type { KanjiSummary } from "./Kanji"

export interface RadicalSummary {
  id: string
  literal: string
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
}

export interface RadicalGroupMember {
  id: string
  literal: string
  kanji: KanjiSummary[] | null
}
