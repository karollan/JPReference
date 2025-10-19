export interface Kanji {
    id: string
    character: string
    meanings: string[]
    readingsOn?: string[]
    readingsKun?: string[]
    strokeCount?: number
    grade?: number
    frequency?: number
    jlptOld?: number
    jlptNew?: number
    codepoints?: string[]
    radicals?: string[]
    variants?: string[]
    radicalNames?: string[]
    dictionaryReferences?: string[]
    queryCodes?: string[]
    nanori?: string[]
}

export interface KanjiListCache {
[key: string]: {
        pages: {
        [page: number]: Kanji[]
        };
        totalCount: number;
        hasMorePages: boolean;
    };
}