import type { TagInfo } from './Common'

// Used to represent a summary of a proper noun in search lists
export interface ProperNounSummary {
    id: string
    dictionaryId: string
    relevanceScore: number
    primaryKanji: KanjiForm
    primaryKana: KanaForm
    otherKanjiForms: KanjiForm[]
    otherKanaForms: KanaForm[]
    translations: TranslationSummary[]
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