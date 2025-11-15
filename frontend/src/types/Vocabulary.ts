import type { TagInfo } from './Common'

// Used to represent a summary of a vocabulary in search lists
export interface VocaabularySummary {
    id: string
    dictionaryId: string
    relevanceScore: number
    primaryKanji: KanjiForm
    primaryKana: KanaForm
    otherKanjiForms: KanjiForm[]
    otherKanaForms: KanaForm[]
    senses: SenseSummary[]
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