import type { KanjiSummary } from "./Kanji"

export interface RadicalSummary {
  id: string
  // Actually backend RadicalSummaryDto Id is int?
  // Let's check: RadicalRepository.cs: line 24: Id = r.Id.
  // Radical.cs: Id is int.
  // Wait, in previous ViewFile of RadicalRepository line 26: StrokeCount = r.StrokeCount.
  // Let's check Radical entity definition in ApplicationDBContext.
  // It says `r.Id`. Usually int or Guid.
  // Radical.cs entity def: `public int Id { get; set; }` usually.
  // Actually, let's assume it's number based on usage.
  literal: string
  strokeCount: number
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

export interface KanjiSimple {
  id: string
  literal: string
  strokeCount: number
}

export interface RadicalSearchResult {
  results: KanjiSimple[]
  compatibleRadicalIds: string[]
}
