// Filter type definitions
export type FilterOperator = '=' | '>' | '<' | 'range'

export type FilterType = 'boolean' | 'enum' | 'equality' | 'range' | 'multi-op'

export interface FilterDefinition {
  key: string
  type: FilterType
  operators?: FilterOperator[] // Only for multi-op
  valueType?: 'int' | 'string'
  min?: number
  max?: number
  enumValues?: number[] | string[]
  description?: string
}

// Filter registry
const FILTER_REGISTRY: Map<string, FilterDefinition> = new Map([
  // Boolean filters (zero-argument, commit immediately after key)
  ['common', { key: 'common', type: 'boolean' }],

  // Enum filters (single value from set)
  ['jlpt', {
    key: 'jlpt',
    type: 'enum',
    valueType: 'int',
    enumValues: [1, 2, 3, 4, 5],
    description: 'JLPT level (1-5)',
  }],

  // Range filters
  ['stroke', {
    key: 'stroke',
    type: 'range',
    valueType: 'int',
    min: 1,
    max: 84,
    description: 'Stroke count range (1-84)',
  }],

  // Equality filters (single int value)
  ['grade', {
    key: 'grade',
    type: 'equality',
    valueType: 'int',
    min: 1,
    max: 12,
    description: 'Grade level (1-12)',
  }],

  // Multi-op filters (future - commented out for now)
  // ['freq', {
  //   key: 'freq',
  //   type: 'multi-op',
  //   operators: ['=', '>', '<', 'range'],
  //   valueType: 'int',
  //   min: 0,
  //   max: 10000
  // }],

  ['surg', { key: 'surg', type: 'boolean' }],
  ['given', { key: 'given', type: 'boolean' }],
  ['electr', { key: 'electr', type: 'boolean' }],
  ['m-sl', { key: 'm-sl', type: 'boolean' }],
  ['v5aru', { key: 'v5aru', type: 'boolean' }],
  ['engr', { key: 'engr', type: 'boolean' }],
  ['ktb', { key: 'ktb', type: 'boolean' }],
  ['v2m-s', { key: 'v2m-s', type: 'boolean' }],
  ['aux-adj', { key: 'aux-adj', type: 'boolean' }],
  ['pol', { key: 'pol', type: 'boolean' }],
  ['golf', { key: 'golf', type: 'boolean' }],
  ['ev', { key: 'ev', type: 'boolean' }],
  ['paleo', { key: 'paleo', type: 'boolean' }],
  ['pref', { key: 'pref', type: 'boolean' }],
  ['product', { key: 'product', type: 'boolean' }],
  ['physiol', { key: 'physiol', type: 'boolean' }],
  ['hob', { key: 'hob', type: 'boolean' }],
  ['adj-no', { key: 'adj-no', type: 'boolean' }],
  ['person', { key: 'person', type: 'boolean' }],
  ['tsb', { key: 'tsb', type: 'boolean' }],
  ['cop', { key: 'cop', type: 'boolean' }],
  ['telec', { key: 'telec', type: 'boolean' }],
  ['male', { key: 'male', type: 'boolean' }],
  ['iK', { key: 'iK', type: 'boolean' }],
  ['vs-s', { key: 'vs-s', type: 'boolean' }],
  ['adj-f', { key: 'adj-f', type: 'boolean' }],
  ['v5b', { key: 'v5b', type: 'boolean' }],
  ['v5s', { key: 'v5s', type: 'boolean' }],
  ['vr', { key: 'vr', type: 'boolean' }],
  ['vs', { key: 'vs', type: 'boolean' }],
  ['v4g', { key: 'v4g', type: 'boolean' }],
  ['music', { key: 'music', type: 'boolean' }],
  ['serv', { key: 'serv', type: 'boolean' }],
  ['adj-ku', { key: 'adj-ku', type: 'boolean' }],
  ['v4m', { key: 'v4m', type: 'boolean' }],
  ['hist', { key: 'hist', type: 'boolean' }],
  ['sports', { key: 'sports', type: 'boolean' }],
  ['min', { key: 'min', type: 'boolean' }],
  ['num', { key: 'num', type: 'boolean' }],
  ['n-pref', { key: 'n-pref', type: 'boolean' }],
  ['chn', { key: 'chn', type: 'boolean' }],
  ['manga', { key: 'manga', type: 'boolean' }],
  ['v2d-s', { key: 'v2d-s', type: 'boolean' }],
  ['grmyth', { key: 'grmyth', type: 'boolean' }],
  ['euph', { key: 'euph', type: 'boolean' }],
  ['motor', { key: 'motor', type: 'boolean' }],
  ['surname', { key: 'surname', type: 'boolean' }],
  ['psy', { key: 'psy', type: 'boolean' }],
  ['ship', { key: 'ship', type: 'boolean' }],
  ['kyb', { key: 'kyb', type: 'boolean' }],
  ['v2b-k', { key: 'v2b-k', type: 'boolean' }],
  ['gikun', { key: 'gikun', type: 'boolean' }],
  ['vk', { key: 'vk', type: 'boolean' }],
  ['cloth', { key: 'cloth', type: 'boolean' }],
  ['v5k-s', { key: 'v5k-s', type: 'boolean' }],
  ['kyu', { key: 'kyu', type: 'boolean' }],
  ['hon', { key: 'hon', type: 'boolean' }],
  ['v5r', { key: 'v5r', type: 'boolean' }],
  ['v4t', { key: 'v4t', type: 'boolean' }],
  ['company', { key: 'company', type: 'boolean' }],
  ['hum', { key: 'hum', type: 'boolean' }],
  ['archit', { key: 'archit', type: 'boolean' }],
  ['fict', { key: 'fict', type: 'boolean' }],
  ['logic', { key: 'logic', type: 'boolean' }],
  ['pathol', { key: 'pathol', type: 'boolean' }],
  ['bra', { key: 'bra', type: 'boolean' }],
  ['ent', { key: 'ent', type: 'boolean' }],
  ['char', { key: 'char', type: 'boolean' }],
  ['v2a-s', { key: 'v2a-s', type: 'boolean' }],
  ['adj-nari', { key: 'adj-nari', type: 'boolean' }],
  ['v5m', { key: 'v5m', type: 'boolean' }],
  ['sens', { key: 'sens', type: 'boolean' }],
  ['v5r-i', { key: 'v5r-i', type: 'boolean' }],
  ['adj-i', { key: 'adj-i', type: 'boolean' }],
  ['finc', { key: 'finc', type: 'boolean' }],
  ['fem', { key: 'fem', type: 'boolean' }],
  ['arch', { key: 'arch', type: 'boolean' }],
  ['proverb', { key: 'proverb', type: 'boolean' }],
  ['v2s-s', { key: 'v2s-s', type: 'boolean' }],
  ['v2r-s', { key: 'v2r-s', type: 'boolean' }],
  ['fish', { key: 'fish', type: 'boolean' }],
  ['quote', { key: 'quote', type: 'boolean' }],
  ['derog', { key: 'derog', type: 'boolean' }],
  ['rk', { key: 'rk', type: 'boolean' }],
  ['fam', { key: 'fam', type: 'boolean' }],
  ['geol', { key: 'geol', type: 'boolean' }],
  ['v2h-s', { key: 'v2h-s', type: 'boolean' }],
  ['Buddh', { key: 'Buddh', type: 'boolean' }],
  ['adv-to', { key: 'adv-to', type: 'boolean' }],
  ['adv', { key: 'adv', type: 'boolean' }],
  ['adj-t', { key: 'adj-t', type: 'boolean' }],
  ['embryo', { key: 'embryo', type: 'boolean' }],
  ['v4s', { key: 'v4s', type: 'boolean' }],
  ['v2t-s', { key: 'v2t-s', type: 'boolean' }],
  ['tv', { key: 'tv', type: 'boolean' }],
  ['mahj', { key: 'mahj', type: 'boolean' }],
  ['osb', { key: 'osb', type: 'boolean' }],
  ['thb', { key: 'thb', type: 'boolean' }],
  ['jpmyth', { key: 'jpmyth', type: 'boolean' }],
  ['int', { key: 'int', type: 'boolean' }],
  ['v4b', { key: 'v4b', type: 'boolean' }],
  ['prt', { key: 'prt', type: 'boolean' }],
  ['sl', { key: 'sl', type: 'boolean' }],
  ['io', { key: 'io', type: 'boolean' }],
  ['internet', { key: 'internet', type: 'boolean' }],
  ['figskt', { key: 'figskt', type: 'boolean' }],
  ['exp', { key: 'exp', type: 'boolean' }],
  ['obj', { key: 'obj', type: 'boolean' }],
  ['v4r', { key: 'v4r', type: 'boolean' }],
  ['id', { key: 'id', type: 'boolean' }],
  ['cryst', { key: 'cryst', type: 'boolean' }],
  ['dent', { key: 'dent', type: 'boolean' }],
  ['v2w-s', { key: 'v2w-s', type: 'boolean' }],
  ['v2g-k', { key: 'v2g-k', type: 'boolean' }],
  ['politics', { key: 'politics', type: 'boolean' }],
  ['ok', { key: 'ok', type: 'boolean' }],
  ['Christn', { key: 'Christn', type: 'boolean' }],
  ['organization', { key: 'organization', type: 'boolean' }],
  ['film', { key: 'film', type: 'boolean' }],
  ['aux', { key: 'aux', type: 'boolean' }],
  ['vn', { key: 'vn', type: 'boolean' }],
  ['mech', { key: 'mech', type: 'boolean' }],
  ['rommyth', { key: 'rommyth', type: 'boolean' }],
  ['ecol', { key: 'ecol', type: 'boolean' }],
  ['cards', { key: 'cards', type: 'boolean' }],
  ['pharm', { key: 'pharm', type: 'boolean' }],
  ['unclass', { key: 'unclass', type: 'boolean' }],
  ['vs-i', { key: 'vs-i', type: 'boolean' }],
  ['poet', { key: 'poet', type: 'boolean' }],
  ['abbr', { key: 'abbr', type: 'boolean' }],
  ['dated', { key: 'dated', type: 'boolean' }],
  ['aviat', { key: 'aviat', type: 'boolean' }],
  ['go', { key: 'go', type: 'boolean' }],
  ['rK', { key: 'rK', type: 'boolean' }],
  ['MA', { key: 'MA', type: 'boolean' }],
  ['vulg', { key: 'vulg', type: 'boolean' }],
  ['sumo', { key: 'sumo', type: 'boolean' }],
  ['met', { key: 'met', type: 'boolean' }],
  ['ling', { key: 'ling', type: 'boolean' }],
  ['ateji', { key: 'ateji', type: 'boolean' }],
  ['mining', { key: 'mining', type: 'boolean' }],
  ['print', { key: 'print', type: 'boolean' }],
  ['elec', { key: 'elec', type: 'boolean' }],
  ['rail', { key: 'rail', type: 'boolean' }],
  ['creat', { key: 'creat', type: 'boolean' }],
  ['oK', { key: 'oK', type: 'boolean' }],
  ['vt', { key: 'vt', type: 'boolean' }],
  ['uk', { key: 'uk', type: 'boolean' }],
  ['v5u', { key: 'v5u', type: 'boolean' }],
  ['gramm', { key: 'gramm', type: 'boolean' }],
  ['hanaf', { key: 'hanaf', type: 'boolean' }],
  ['geom', { key: 'geom', type: 'boolean' }],
  ['station', { key: 'station', type: 'boolean' }],
  ['law', { key: 'law', type: 'boolean' }],
  ['ctr', { key: 'ctr', type: 'boolean' }],
  ['aux-v', { key: 'aux-v', type: 'boolean' }],
  ['leg', { key: 'leg', type: 'boolean' }],
  ['shogi', { key: 'shogi', type: 'boolean' }],
  ['ik', { key: 'ik', type: 'boolean' }],
  ['v2h-k', { key: 'v2h-k', type: 'boolean' }],
  ['v4h', { key: 'v4h', type: 'boolean' }],
  ['econ', { key: 'econ', type: 'boolean' }],
  ['work', { key: 'work', type: 'boolean' }],
  ['conj', { key: 'conj', type: 'boolean' }],
  ['net-sl', { key: 'net-sl', type: 'boolean' }],
  ['dei', { key: 'dei', type: 'boolean' }],
  ['group', { key: 'group', type: 'boolean' }],
  ['doc', { key: 'doc', type: 'boolean' }],
  ['stat', { key: 'stat', type: 'boolean' }],
  ['v2z-s', { key: 'v2z-s', type: 'boolean' }],
  ['vet', { key: 'vet', type: 'boolean' }],
  ['myth', { key: 'myth', type: 'boolean' }],
  ['audvid', { key: 'audvid', type: 'boolean' }],
  ['photo', { key: 'photo', type: 'boolean' }],
  ['rare', { key: 'rare', type: 'boolean' }],
  ['anat', { key: 'anat', type: 'boolean' }],
  ['pn', { key: 'pn', type: 'boolean' }],
  ['art', { key: 'art', type: 'boolean' }],
  ['adj-ix', { key: 'adj-ix', type: 'boolean' }],
  ['psych', { key: 'psych', type: 'boolean' }],
  ['sk', { key: 'sk', type: 'boolean' }],
  ['v2y-k', { key: 'v2y-k', type: 'boolean' }],
  ['unc', { key: 'unc', type: 'boolean' }],
  ['form', { key: 'form', type: 'boolean' }],
  ['ksb', { key: 'ksb', type: 'boolean' }],
  ['yoji', { key: 'yoji', type: 'boolean' }],
  ['ski', { key: 'ski', type: 'boolean' }],
  ['v4k', { key: 'v4k', type: 'boolean' }],
  ['bot', { key: 'bot', type: 'boolean' }],
  ['horse', { key: 'horse', type: 'boolean' }],
  ['vi', { key: 'vi', type: 'boolean' }],
  ['ornith', { key: 'ornith', type: 'boolean' }],
  ['on-mim', { key: 'on-mim', type: 'boolean' }],
  ['vs-c', { key: 'vs-c', type: 'boolean' }],
  ['archeol', { key: 'archeol', type: 'boolean' }],
  ['stockm', { key: 'stockm', type: 'boolean' }],
  ['med', { key: 'med', type: 'boolean' }],
  ['vz', { key: 'vz', type: 'boolean' }],
  ['chem', { key: 'chem', type: 'boolean' }],
  ['phil', { key: 'phil', type: 'boolean' }],
  ['bus', { key: 'bus', type: 'boolean' }],
  ['suf', { key: 'suf', type: 'boolean' }],
  ['genet', { key: 'genet', type: 'boolean' }],
  ['v2k-k', { key: 'v2k-k', type: 'boolean' }],
  ['baseb', { key: 'baseb', type: 'boolean' }],
  ['nab', { key: 'nab', type: 'boolean' }],
  ['v2n-s', { key: 'v2n-s', type: 'boolean' }],
  ['masc', { key: 'masc', type: 'boolean' }],
  ['v1', { key: 'v1', type: 'boolean' }],
  ['v5t', { key: 'v5t', type: 'boolean' }],
  ['adj-pn', { key: 'adj-pn', type: 'boolean' }],
  ['math', { key: 'math', type: 'boolean' }],
  ['prowres', { key: 'prowres', type: 'boolean' }],
  ['adj-shiku', { key: 'adj-shiku', type: 'boolean' }],
  ['astron', { key: 'astron', type: 'boolean' }],
  ['noh', { key: 'noh', type: 'boolean' }],
  ['n-suf', { key: 'n-suf', type: 'boolean' }],
  ['v5k', { key: 'v5k', type: 'boolean' }],
  ['adj-na', { key: 'adj-na', type: 'boolean' }],
  ['kabuki', { key: 'kabuki', type: 'boolean' }],
  ['geogr', { key: 'geogr', type: 'boolean' }],
  ['v2r-k', { key: 'v2r-k', type: 'boolean' }],
  ['v5u-s', { key: 'v5u-s', type: 'boolean' }],
  ['civeng', { key: 'civeng', type: 'boolean' }],
  ['obs', { key: 'obs', type: 'boolean' }],
  ['n', { key: 'n', type: 'boolean' }],
  ['tradem', { key: 'tradem', type: 'boolean' }],
  ['physics', { key: 'physics', type: 'boolean' }],
  ['food', { key: 'food', type: 'boolean' }],
  ['biochem', { key: 'biochem', type: 'boolean' }],
  ['place', { key: 'place', type: 'boolean' }],
  ['Shinto', { key: 'Shinto', type: 'boolean' }],
  ['v1-s', { key: 'v1-s', type: 'boolean' }],
  ['v2y-s', { key: 'v2y-s', type: 'boolean' }],
  ['rkb', { key: 'rkb', type: 'boolean' }],
  ['tsug', { key: 'tsug', type: 'boolean' }],
  ['mil', { key: 'mil', type: 'boolean' }],
  ['vidg', { key: 'vidg', type: 'boolean' }],
  ['v2k-s', { key: 'v2k-s', type: 'boolean' }],
  ['comp', { key: 'comp', type: 'boolean' }],
  ['v2g-s', { key: 'v2g-s', type: 'boolean' }],
  ['v2t-k', { key: 'v2t-k', type: 'boolean' }],
  ['gardn', { key: 'gardn', type: 'boolean' }],
  ['v5n', { key: 'v5n', type: 'boolean' }],
])

// Helper functions
export function getFilterDefinition (key: string): FilterDefinition | undefined {
  return FILTER_REGISTRY.get(key.toLowerCase())
}

export function getAllFilterKeys (): string[] {
  return Array.from(FILTER_REGISTRY.keys())
}

export function isValidFilterValue (def: FilterDefinition, value: string): boolean {
  if (def.type === 'boolean') {
    return true // Boolean filters don't have values
  }

  if (def.valueType === 'int') {
    const num = Number.parseInt(value, 10)
    if (Number.isNaN(num)) {
      return false
    }

    if (def.type === 'enum') {
      const enumVals = def.enumValues as number[] | undefined
      return enumVals?.includes(num) ?? false
    }

    if (def.min !== undefined && num < def.min) {
      return false
    }
    if (def.max !== undefined && num > def.max) {
      return false
    }

    return true
  }

  return true
}

export function parseFilterValue (def: FilterDefinition, value: string): any {
  if (def.type === 'boolean') {
    return true
  }

  if (def.valueType === 'int') {
    return Number.parseInt(value, 10)
  }

  return value
}
