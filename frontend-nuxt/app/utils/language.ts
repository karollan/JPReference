export const LANGUAGE_PAIRS: Record<string, string> = {
  eng: 'English',
  ger: 'German',
  rus: 'Russian',
  hun: 'Hungarian',
  dut: 'Dutch',
  spa: 'Spanish',
  fre: 'French',
  swe: 'Swedish',
  slv: 'Slovenian',
  por: 'Portuguese'
}

const LANGUAGE_FLAGS: Record<string, string> = {
  eng: '🇬🇧',
  ger: '🇩🇪',
  rus: '🇷🇺',
  hun: '🇭🇺',
  dut: '🇳🇱',
  spa: '🇪🇸',
  fre: '🇫🇷',
  swe: '🇸🇪',
  slv: '🇸🇮',
  por: '🇵🇹'
}

export const DEFAULT_LANGUAGE = 'eng'
const FALLBACK_FLAG = '🏳️'

export function normalizeLanguageCode(code?: string | null): string {
  return code?.toLowerCase() ?? ''
}

function collectVariants(code?: string | null): string[] {
  const normalized = normalizeLanguageCode(code)
  if (!normalized) {
    return []
  }

  const variants = new Set<string>([normalized])

  const threeLetter = LANGUAGE_PAIRS[normalized]
  if (threeLetter) {
    variants.add(threeLetter)
  }

  return Array.from(variants)
}

export function languageMatches(codeA?: string | null, codeB?: string | null): boolean {
  if (!codeA || !codeB) {
    return false
  }

  const variantsA = collectVariants(codeA)
  const variantsB = collectVariants(codeB)

  return variantsA.some(variant => variantsB.includes(variant))
}

export function getLanguageFlag(code?: string | null): string {
  const normalized = normalizeLanguageCode(code)
  return LANGUAGE_FLAGS[normalized] || FALLBACK_FLAG
}

export function getLanguageName(code?: string | null): string {
  const normalized = normalizeLanguageCode(code)
  if (!normalized) {
    return ''
  }

  return LANGUAGE_PAIRS[normalized] || normalized.toUpperCase()
}
