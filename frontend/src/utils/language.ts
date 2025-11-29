const LANGUAGE_PAIRS: Array<[string, string, string]> = [
  ['eng', 'en', 'English'],
  ['ger', 'de', 'German'],
  ['rus', 'ru', 'Russian'],
  ['hun', 'hu', 'Hungarian'],
  ['dut', 'nl', 'Dutch'],
  ['spa', 'es', 'Spanish'],
  ['fre', 'fr', 'French'],
  ['swe', 'sv', 'Swedish'],
  ['slv', 'sl', 'Slovenian'],
  ['por', 'pt', 'Portuguese'],
  ['ita', 'it', 'Italian'],
  ['jpn', 'ja', 'Japanese'],
]

const LANGUAGE_FLAGS: Record<string, string> = {
  eng: '🇬🇧',
  en: '🇬🇧',
  ger: '🇩🇪',
  de: '🇩🇪',
  rus: '🇷🇺',
  ru: '🇷🇺',
  hun: '🇭🇺',
  hu: '🇭🇺',
  dut: '🇳🇱',
  nl: '🇳🇱',
  spa: '🇪🇸',
  es: '🇪🇸',
  fre: '🇫🇷',
  fr: '🇫🇷',
  swe: '🇸🇪',
  sv: '🇸🇪',
  slv: '🇸🇮',
  sl: '🇸🇮',
  por: '🇵🇹',
  pt: '🇵🇹',
  ita: '🇮🇹',
  it: '🇮🇹',
  jpn: '🇯🇵',
  ja: '🇯🇵',
}

const languageEntries = LANGUAGE_PAIRS.map(([three, two, name]) => ({ three, two, name }))

const THREE_TO_TWO: Record<string, string> = Object.fromEntries(languageEntries.map(({ three, two }) => [three, two]))
const TWO_TO_THREE: Record<string, string> = Object.fromEntries(languageEntries.map(({ three, two }) => [two, three]))
const LANGUAGE_NAMES: Record<string, string> = Object.fromEntries(
  languageEntries.flatMap(({ three, two, name }) => [
    [three, name],
    [two, name],
  ]),
)

export const DEFAULT_LANGUAGE = 'eng'
const FALLBACK_FLAG = '🏳️'

export function normalizeLanguageCode (code?: string | null): string {
  return code?.toLowerCase() ?? ''
}

function collectVariants (code?: string | null): string[] {
  const normalized = normalizeLanguageCode(code)
  if (!normalized) {
    return []
  }

  const variants = new Set<string>([normalized])

  const twoLetter = THREE_TO_TWO[normalized]
  if (twoLetter) {
    variants.add(twoLetter)
  }

  const threeLetter = TWO_TO_THREE[normalized]
  if (threeLetter) {
    variants.add(threeLetter)
  }

  return Array.from(variants)
}

export function languageMatches (codeA?: string | null, codeB?: string | null): boolean {
  if (!codeA || !codeB) {
    return false
  }

  const variantsA = collectVariants(codeA)
  const variantsB = collectVariants(codeB)

  return variantsA.some(variant => variantsB.includes(variant))
}

export function getLanguageFlag (code?: string | null): string {
  const normalized = normalizeLanguageCode(code)
  return LANGUAGE_FLAGS[normalized] || FALLBACK_FLAG
}

export function getLanguageName (code?: string | null): string {
  const normalized = normalizeLanguageCode(code)
  if (!normalized) {
    return ''
  }

  return LANGUAGE_NAMES[normalized] || normalized.toUpperCase()
}
