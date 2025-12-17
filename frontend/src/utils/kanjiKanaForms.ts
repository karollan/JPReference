import type { TagInfo } from '@/types/Common'

/**
 * Generic interfaces for kanji/kana forms that can be used by both
 * Vocabulary and ProperNoun components
 */
export interface KanjiFormBase {
    text: string
    tags: TagInfo[]
    isCommon?: boolean
}

export interface KanaFormBase {
    text: string
    tags: TagInfo[]
    appliesToKanji: string[]
    isCommon?: boolean
}

export interface KanjiWithReadings<K extends KanjiFormBase, R extends KanaFormBase> {
    kanji: K
    readings: R[]
}

/**
 * Groups kanji forms with their applicable kana readings based on appliesToKanji.
 * @param otherKanjiForms - Array of other kanji forms to group
 * @param allKanaForms - All kana forms (primary + other) to match against
 * @returns Array of kanji forms with their matched readings
 */
export function groupKanjiWithReadings<K extends KanjiFormBase, R extends KanaFormBase>(
    otherKanjiForms: K[] | null | undefined,
    allKanaForms: R[],
): KanjiWithReadings<K, R>[] {
    if (!otherKanjiForms) return []

    return otherKanjiForms.map(kanji => {
        const readings = allKanaForms.filter(kana => {
            if (!kana.appliesToKanji) return false
            return kana.appliesToKanji.includes(kanji.text) || kana.appliesToKanji.includes('*')
        })

        return {
            kanji,
            readings,
        }
    })
}

/**
 * Collects all kana forms (primary + other) into a single array for matching.
 * @param primaryKana - The primary kana form
 * @param otherKanaForms - Other kana forms
 * @returns Combined array of all kana forms
 */
export function collectAllKanaForms<R extends KanaFormBase>(
    primaryKana: R | null | undefined,
    otherKanaForms: R[] | null | undefined,
): R[] {
    return [
        ...(primaryKana ? [primaryKana] : []),
        ...(otherKanaForms || []),
    ]
}

/**
 * Tracks which kana forms have been used (matched to kanji).
 * @param primaryKanji - The primary kanji form
 * @param primaryKana - The primary kana form
 * @param otherKanjiForms - Other kanji forms
 * @param allKanaForms - All kana forms
 * @returns Set of kana texts that have been used
 */
export function getUsedKanaTexts<K extends KanjiFormBase, R extends KanaFormBase>(
    primaryKanji: K | null | undefined,
    primaryKana: R | null | undefined,
    otherKanjiForms: K[] | null | undefined,
    allKanaForms: R[],
): Set<string> {
    const used = new Set<string>()

    // Primary kana is always used with primary kanji
    if (primaryKanji && primaryKana) {
        used.add(primaryKana.text)
    }

    // Mark kana used by other kanji forms
    for (const kanjiForm of (otherKanjiForms || [])) {
        for (const kana of allKanaForms) {
            if (kana.appliesToKanji?.includes(kanjiForm.text) || kana.appliesToKanji?.includes('*')) {
                used.add(kana.text)
            }
        }
    }

    return used
}

/**
 * Finds standalone kana forms that are not matched to any kanji.
 * @param primaryKanji - The primary kanji form
 * @param otherKanjiForms - Other kanji forms
 * @param otherKanaForms - Other kana forms
 * @param usedKanaTexts - Set of kana texts already used
 * @returns Array of standalone kana forms
 */
export function getStandaloneKanaForms<K extends KanjiFormBase, R extends KanaFormBase>(
    primaryKanji: K | null | undefined,
    otherKanjiForms: K[] | null | undefined,
    otherKanaForms: R[] | null | undefined,
    usedKanaTexts: Set<string>,
): R[] {
    // If there's no primary kanji, we don't show standalone kana separately
    // because the main entry is already kana-based
    if (!primaryKanji) return []

    const otherKana = otherKanaForms || []

    return otherKana.filter(kana => {
        // Skip if already used (matched to a kanji)
        if (usedKanaTexts.has(kana.text)) return false

        // Collect all kanji texts
        const allKanji = [
            primaryKanji?.text,
            ...(otherKanjiForms?.map(k => k.text) || []),
        ].filter(Boolean) as string[]

        // If appliesToKanji contains "*", it's used for all kanji so not standalone
        if (kana.appliesToKanji?.includes('*')) return false

        // Check if it applies to any kanji
        const matchesAnyKanji = kana.appliesToKanji?.some(k => allKanji.includes(k)) ?? false

        // It's standalone if it doesn't match any kanji
        return !matchesAnyKanji
    })
}
