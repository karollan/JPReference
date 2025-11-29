import type { PaginationMetadata, TagInfo } from './Common'

export interface VocabularyResponse {
  data: VocabularySummary[]
  pagination: PaginationMetadata
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
  jlptLevel?: number
  isCommon: boolean
}

interface KanjiForm {
  text: string
  isCommon: boolean
  tags: TagInfo[]
}

interface KanaForm {
  text: string
  isCommon: boolean
  tags: TagInfo[]
  appliesToKanji: string[]
}

interface SenseSummary {
  appliesToKanji: string[]
  appliesToKana: string[]
  info: string[]
  glosses: SenseGloss[]
  tags: TagInfo[]
}

interface SenseGloss {
  language: string
  text: string
  gender?: string
  type?: string
}
