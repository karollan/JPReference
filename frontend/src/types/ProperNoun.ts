import type { PaginationMetadata, TagInfo, Furigana } from './Common'

export interface ProperNounResponse {
  data: ProperNounSummary[]
  pagination: PaginationMetadata
}

export interface ProperNounDetails {
  id: string
  jmnedictId: string
  kanjiForms: KanjiForm[]
  kanaForms: KanaForm[]
  translations: TranslationDetails[]
  furigana: Furigana[] | null
  containedKanji: KanjiInfo[]
}

// Used to represent a summary of a proper noun in search lists
export interface ProperNounSummary {
  id: string
  dictionaryId: string
  relevanceScore: number
  primaryKanji: KanjiForm | null
  primaryKana: KanaForm | null
  otherKanjiForms: KanjiForm[] | null
  otherKanaForms: KanaForm[] | null
  translations: TranslationSummary[] | null
  furigana: Furigana[] | null
  slug: string
}

export interface KanjiInfo {
  id: string
  literal: string
}

export interface KanjiForm {
  text: string
  tags: TagInfo[]
}

export interface KanaForm {
  text: string
  tags: TagInfo[]
  appliesToKanji: string[]
}

export interface TranslationDetails {
  types: TagInfo[]
  related: TranslationRelated[]
  text: TranslationText[]
}

export interface TranslationRelated {
  term: string
  reading: string | null
  referenceProperNounId: string | null
  referenceProperNounTranslationId: string | null
}

export interface TranslationSummary {
  types: TagInfo[]
  translations: TranslationText[]
}

export interface TranslationText {
  language: string
  text: string
}
