import type { PaginationMetadata, TagInfo, Furigana } from './Common'

export interface VocabularyResponse {
  data: VocabularySummary[]
  pagination: PaginationMetadata
}

export interface VocabularyDetails {
  id: string
  jmdictId: string
  kanjiForms: KanjiForm[]
  kanaForms: KanaForm[]
  senses: SenseDetails[]
  furigana: Furigana[] | null
  jlptLevel?: number
  containedKanji: KanjiInfo[]
}

// Used to represent a summary of a vocabulary in search lists
export interface VocabularySummary {
  id: string
  dictionaryId: string
  relevanceScore: number
  primaryKanji: KanjiForm | null
  primaryKana: KanaForm | null
  otherKanjiForms: KanjiForm[] | null
  otherKanaForms: KanaForm[] | null
  senses: SenseSummary[] | null
  furigana: Furigana[] | null
  jlptLevel?: number
  isCommon: boolean
  slug: string
}

export interface KanjiInfo {
  id: string
  literal: string
}

export interface KanjiForm {
  text: string
  isCommon: boolean
  tags: TagInfo[]
}

export interface KanaForm {
  text: string
  isCommon: boolean
  tags: TagInfo[]
  appliesToKanji: string[]
}

export interface SenseSummary {
  appliesToKanji: string[]
  appliesToKana: string[]
  glosses: SenseGloss[]
  info: string[]
  tags: TagInfo[]
}

export interface SenseDetails {
  appliesToKanji: string[]
  appliesToKana: string[]
  info: string[]
  glosses: SenseGloss[]
  relations: SenseRelation[]
  languageSources: SenseLanguageSource[]
  examples: SenseExample[]
  tags: TagInfo[]
}

export interface SenseGloss {
  language: string
  text: string
  gender?: string
  type?: string
}

export interface SenseRelation {
  relationId: string
  relationSenseId: string
  term: string
  reading?: string
  relationType: string
}

export interface SenseLanguageSource {
  language: string
  text: string
  isFull?: boolean
  isWaei?: boolean
}

export interface SenseExample {
  sourceType: string
  sourceValue: string
  text: string
  sentences: SenseExampleSentence[]
}

export interface SenseExampleSentence {
  language: string
  text: string
}
