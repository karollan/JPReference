import type { VocabularyResponse } from "@/types/Vocabulary"
import type { ProperNounResponse } from "@/types/ProperNoun"
import type { KanjiResponse } from "@/types/Kanji"

export interface GlobalSearchResponse {
    vocabularyResults: VocabularyResponse
    properNounResults: ProperNounResponse
    kanjiResults: KanjiResponse
}

export interface GlobalSearchCache {
    [key: string]: GlobalSearchResponse
}