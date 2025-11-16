import type { PaginationMetadata, TagInfo } from './Common'

export interface ProperNounResponse {
    data: ProperNounSummary[]
    pagination: PaginationMetadata
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
}

interface KanjiForm {
    text: string
    tags: TagInfo[]
}

interface KanaForm {
    text: string
    tags: TagInfo[]
    appliesToKanji: string[]
}

interface TranslationSummary {
    types: TagInfo[]
    text: TranslationText[]
}

interface TranslationText {
    language: string
    text: string
}