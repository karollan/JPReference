import { LANGUAGE_PAIRS } from './language'
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
    type: 'range',
    valueType: 'int',
    min: 1,
    max: 5,
    description: 'JLPT level range (1-5)',
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
    type: 'range',
    valueType: 'int',
    min: 1,
    max: 12,
    description: 'Grade level range (1-12)',
  }],

  // Multi-op filters (future - commented out for now)
  ['freq', {
    key: 'freq',
    type: 'range', // multi-op is not supported yet
    operators: ['=', '>', '<', 'range'],
    valueType: 'int',
    min: 0,
    max: 10_000,
    description: 'Frequency range (0-10000)',
  }],

  [
    'lang', {
      key: 'lang',
      type: 'enum',
      enumValues: Object.keys(LANGUAGE_PAIRS),
      description: 'Language',
      valueType: 'string',
    },
  ],

  ['surg', { key: 'surg', type: 'boolean', description: 'Surgery' }],
  ['given', { key: 'given', type: 'boolean', description: 'Given name or forename, gender not specified' }],
  ['electr', { key: 'electr', type: 'boolean', description: 'Electronics' }],
  ['m-sl', { key: 'm-sl', type: 'boolean', description: 'Manga slang' }],
  ['v5aru', { key: 'v5aru', type: 'boolean', description: 'Godan verb - -aru special class' }],
  ['engr', { key: 'engr', type: 'boolean', description: 'Engineering' }],
  ['ktb', { key: 'ktb', type: 'boolean', description: 'Kantou-ben' }],
  ['v2m-s', { key: 'v2m-s', type: 'boolean', description: 'Nidan verb (lower class) with \'mu\' ending (archaic)' }],
  ['aux-adj', { key: 'aux-adj', type: 'boolean', description: 'Auxiliary adjective' }],
  ['pol', { key: 'pol', type: 'boolean', description: 'Polite (teineigo) language' }],
  ['golf', { key: 'golf', type: 'boolean', description: 'Golf' }],
  ['ev', { key: 'ev', type: 'boolean', description: 'Event' }],
  ['paleo', { key: 'paleo', type: 'boolean', description: 'Paleontology' }],
  ['pref', { key: 'pref', type: 'boolean', description: 'Prefix' }],
  ['product', { key: 'product', type: 'boolean', description: 'Product name' }],
  ['physiol', { key: 'physiol', type: 'boolean', description: 'Physiology' }],
  ['hob', { key: 'hob', type: 'boolean', description: 'Hokkaido-ben' }],
  ['adj-no', { key: 'adj-no', type: 'boolean', description: 'Nouns which may take the genitive case particle \'no\'' }],
  ['person', { key: 'person', type: 'boolean', description: 'Full name of a particular person' }],
  ['tsb', { key: 'tsb', type: 'boolean', description: 'Tosa-ben' }],
  ['cop', { key: 'cop', type: 'boolean', description: 'Copula' }],
  ['telec', { key: 'telec', type: 'boolean', description: 'Telecommunications' }],
  ['male', { key: 'male', type: 'boolean', description: 'Male term or language' }],
  ['iK', { key: 'iK', type: 'boolean', description: 'Word containing irregular kanji usage' }],
  ['vs-s', { key: 'vs-s', type: 'boolean', description: 'Suru verb - special class' }],
  ['adj-f', { key: 'adj-f', type: 'boolean', description: 'Noun or verb acting prenominally' }],
  ['v5b', { key: 'v5b', type: 'boolean', description: 'Godan verb with \'bu\' ending' }],
  ['v5s', { key: 'v5s', type: 'boolean', description: 'Godan verb with \'su\' ending' }],
  ['vr', { key: 'vr', type: 'boolean', description: 'Irregular ru verb, plain form ends with -ri' }],
  ['vs', { key: 'vs', type: 'boolean', description: 'Noun or participle which takes the aux. verb suru' }],
  ['v4g', { key: 'v4g', type: 'boolean', description: 'Yodan verb with \'gu\' ending (archaic)' }],
  ['music', { key: 'music', type: 'boolean', description: 'Music' }],
  ['serv', { key: 'serv', type: 'boolean', description: 'Service' }],
  ['adj-ku', { key: 'adj-ku', type: 'boolean', description: '\'ku\' adjective (archaic)' }],
  ['v4m', { key: 'v4m', type: 'boolean', description: 'Yodan verb with \'mu\' ending (archaic)' }],
  ['hist', { key: 'hist', type: 'boolean', description: 'Historical term' }],
  ['sports', { key: 'sports', type: 'boolean', description: 'Sports' }],
  ['min', { key: 'min', type: 'boolean', description: 'Mineralogy' }],
  ['num', { key: 'num', type: 'boolean', description: 'Numeric' }],
  ['n-pref', { key: 'n-pref', type: 'boolean', description: 'Noun, used as a prefix' }],
  ['chn', { key: 'chn', type: 'boolean', description: 'Children\'s language' }],
  ['manga', { key: 'manga', type: 'boolean', description: 'Manga' }],
  ['v2d-s', { key: 'v2d-s', type: 'boolean', description: 'Nidan verb (lower class) with \'dzu\' ending (archaic)' }],
  ['grmyth', { key: 'grmyth', type: 'boolean', description: 'Greek mythology' }],
  ['euph', { key: 'euph', type: 'boolean', description: 'Euphemistic' }],
  ['motor', { key: 'motor', type: 'boolean', description: 'Motorsport' }],
  ['surname', { key: 'surname', type: 'boolean', description: 'Family or surname' }],
  ['psy', { key: 'psy', type: 'boolean', description: 'Psychiatry' }],
  ['ship', { key: 'ship', type: 'boolean', description: 'Ship name' }],
  ['kyb', { key: 'kyb', type: 'boolean', description: 'Kyoto-ben' }],
  ['v2b-k', { key: 'v2b-k', type: 'boolean', description: 'Nidan verb (upper class) with \'bu\' ending (archaic)' }],
  ['gikun', { key: 'gikun', type: 'boolean', description: 'Gikun (meaning as reading) or jukujikun (special kanji reading)' }],
  ['vk', { key: 'vk', type: 'boolean', description: 'Kuru verb - special class' }],
  ['cloth', { key: 'cloth', type: 'boolean', description: 'Clothing' }],
  ['v5k-s', { key: 'v5k-s', type: 'boolean', description: 'Godan verb - Iku/Yuku special class' }],
  ['kyu', { key: 'kyu', type: 'boolean', description: 'Kyuushuu-ben' }],
  ['hon', { key: 'hon', type: 'boolean', description: 'Honorific or respectful (sonkeigo) language' }],
  ['v5r', { key: 'v5r', type: 'boolean', description: 'Godan verb with \'ru\' ending' }],
  ['v4t', { key: 'v4t', type: 'boolean', description: 'Yodan verb with \'tsu\' ending (archaic)' }],
  ['company', { key: 'company', type: 'boolean', description: 'Company name' }],
  ['hum', { key: 'hum', type: 'boolean', description: 'Humble (kenjougo) language' }],
  ['archit', { key: 'archit', type: 'boolean', description: 'Architecture' }],
  ['fict', { key: 'fict', type: 'boolean', description: 'Fiction' }],
  ['logic', { key: 'logic', type: 'boolean', description: 'Logic' }],
  ['pathol', { key: 'pathol', type: 'boolean', description: 'Pathology' }],
  ['bra', { key: 'bra', type: 'boolean', description: 'Brazilian' }],
  ['ent', { key: 'ent', type: 'boolean', description: 'Entomology' }],
  ['char', { key: 'char', type: 'boolean', description: 'Character' }],
  ['v2a-s', { key: 'v2a-s', type: 'boolean', description: 'Nidan verb with \'u\' ending (archaic)' }],
  ['adj-nari', { key: 'adj-nari', type: 'boolean', description: 'Archaic/formal form of na-adjective' }],
  ['v5m', { key: 'v5m', type: 'boolean', description: 'Godan verb with \'mu\' ending' }],
  ['sens', { key: 'sens', type: 'boolean', description: 'Sensitive' }],
  ['v5r-i', { key: 'v5r-i', type: 'boolean', description: 'Godan verb with \'ru\' ending (irregular verb)' }],
  ['adj-i', { key: 'adj-i', type: 'boolean', description: 'Adjective (keiyoushi)' }],
  ['finc', { key: 'finc', type: 'boolean', description: 'Finance' }],
  ['fem', { key: 'fem', type: 'boolean', description: 'Female given name or forename' }],
  ['arch', { key: 'arch', type: 'boolean', description: 'Archaic' }],
  ['proverb', { key: 'proverb', type: 'boolean', description: 'Proverb' }],
  ['v2s-s', { key: 'v2s-s', type: 'boolean', description: 'Nidan verb (lower class) with \'su\' ending (archaic)' }],
  ['v2r-s', { key: 'v2r-s', type: 'boolean', description: 'Nidan verb (lower class) with \'ru\' ending (archaic)' }],
  ['fish', { key: 'fish', type: 'boolean', description: 'Fishing' }],
  ['quote', { key: 'quote', type: 'boolean', description: 'Quotation' }],
  ['derog', { key: 'derog', type: 'boolean', description: 'Derogatory' }],
  ['rk', { key: 'rk', type: 'boolean', description: 'Rarely used kana form' }],
  ['fam', { key: 'fam', type: 'boolean', description: 'Familiar language' }],
  ['geol', { key: 'geol', type: 'boolean', description: 'Geology' }],
  ['v2h-s', { key: 'v2h-s', type: 'boolean', description: 'Nidan verb (lower class) with \'hu/fu\' ending (archaic)' }],
  ['Buddh', { key: 'Buddh', type: 'boolean', description: 'Buddhism' }],
  ['adv-to', { key: 'adv-to', type: 'boolean', description: 'Adverb taking the \'to\' particle' }],
  ['adv', { key: 'adv', type: 'boolean', description: 'Adverb (fukushi)' }],
  ['adj-t', { key: 'adj-t', type: 'boolean', description: '\'taru\' adjective' }],
  ['embryo', { key: 'embryo', type: 'boolean', description: 'Embryology' }],
  ['v4s', { key: 'v4s', type: 'boolean', description: 'Yodan verb with \'su\' ending (archaic)' }],
  ['v2t-s', { key: 'v2t-s', type: 'boolean', description: 'Nidan verb (lower class) with \'tsu\' ending (archaic)' }],
  ['tv', { key: 'tv', type: 'boolean', description: 'Television' }],
  ['mahj', { key: 'mahj', type: 'boolean', description: 'Mahjong' }],
  ['osb', { key: 'osb', type: 'boolean', description: 'Osaka-ben' }],
  ['thb', { key: 'thb', type: 'boolean', description: 'Touhoku-ben' }],
  ['jpmyth', { key: 'jpmyth', type: 'boolean', description: 'Japanese mythology' }],
  ['int', { key: 'int', type: 'boolean', description: 'Interjection (kandoushi)' }],
  ['v4b', { key: 'v4b', type: 'boolean', description: 'Yodan verb with \'bu\' ending (archaic)' }],
  ['prt', { key: 'prt', type: 'boolean', description: 'Particle' }],
  ['sl', { key: 'sl', type: 'boolean', description: 'Slang' }],
  ['io', { key: 'io', type: 'boolean', description: 'Irregular okurigana usage' }],
  ['internet', { key: 'internet', type: 'boolean', description: 'Internet' }],
  ['figskt', { key: 'figskt', type: 'boolean', description: 'Figure skating' }],
  ['exp', { key: 'exp', type: 'boolean', description: 'Expressions (phrases, clauses, etc.)' }],
  ['obj', { key: 'obj', type: 'boolean', description: 'Object' }],
  ['v4r', { key: 'v4r', type: 'boolean', description: 'Yodan verb with \'ru\' ending (archaic)' }],
  ['id', { key: 'id', type: 'boolean', description: 'Idiomatic expression' }],
  ['cryst', { key: 'cryst', type: 'boolean', description: 'Crystallography' }],
  ['dent', { key: 'dent', type: 'boolean', description: 'Dentistry' }],
  ['v2w-s', { key: 'v2w-s', type: 'boolean', description: 'Nidan verb (lower class) with \'u\' ending and \'we\' conjugation (archaic)' }],
  ['v2g-k', { key: 'v2g-k', type: 'boolean', description: 'Nidan verb (upper class) with \'gu\' ending (archaic)' }],
  ['politics', { key: 'politics', type: 'boolean', description: 'Politics' }],
  ['ok', { key: 'ok', type: 'boolean', description: 'Out-dated or obsolete kana usage' }],
  ['Christn', { key: 'Christn', type: 'boolean', description: 'Christianity' }],
  ['organization', { key: 'organization', type: 'boolean', description: 'Organization name' }],
  ['film', { key: 'film', type: 'boolean', description: 'Film' }],
  ['aux', { key: 'aux', type: 'boolean', description: 'Auxiliary' }],
  ['vn', { key: 'vn', type: 'boolean', description: 'Irregular nu verb' }],
  ['mech', { key: 'mech', type: 'boolean', description: 'Mechanical engineering' }],
  ['rommyth', { key: 'rommyth', type: 'boolean', description: 'Roman mythology' }],
  ['ecol', { key: 'ecol', type: 'boolean', description: 'Ecology' }],
  ['cards', { key: 'cards', type: 'boolean', description: 'Card games' }],
  ['pharm', { key: 'pharm', type: 'boolean', description: 'Pharmacology' }],
  ['unclass', { key: 'unclass', type: 'boolean', description: 'Unclassified name' }],
  ['vs-i', { key: 'vs-i', type: 'boolean', description: 'Suru verb - included' }],
  ['poet', { key: 'poet', type: 'boolean', description: 'Poetical term' }],
  ['abbr', { key: 'abbr', type: 'boolean', description: 'Abbreviation' }],
  ['dated', { key: 'dated', type: 'boolean', description: 'Dated term' }],
  ['aviat', { key: 'aviat', type: 'boolean', description: 'Aviation' }],
  ['go', { key: 'go', type: 'boolean', description: 'Go (game)' }],
  ['rK', { key: 'rK', type: 'boolean', description: 'Rarely used kanji form' }],
  ['MA', { key: 'MA', type: 'boolean', description: 'Martial arts' }],
  ['vulg', { key: 'vulg', type: 'boolean', description: 'Vulgar expression or word' }],
  ['sumo', { key: 'sumo', type: 'boolean', description: 'Sumo' }],
  ['met', { key: 'met', type: 'boolean', description: 'Meteorology' }],
  ['ling', { key: 'ling', type: 'boolean', description: 'Linguistics' }],
  ['ateji', { key: 'ateji', type: 'boolean', description: 'Ateji (phonetic) reading' }],
  ['mining', { key: 'mining', type: 'boolean', description: 'Mining' }],
  ['print', { key: 'print', type: 'boolean', description: 'Printing' }],
  ['elec', { key: 'elec', type: 'boolean', description: 'Electricity, elec. eng.' }],
  ['rail', { key: 'rail', type: 'boolean', description: 'Railway' }],
  ['creat', { key: 'creat', type: 'boolean', description: 'Creature' }],
  ['oK', { key: 'oK', type: 'boolean', description: 'Word containing out-dated kanji or kanji usage' }],
  ['vt', { key: 'vt', type: 'boolean', description: 'Transitive verb' }],
  ['uk', { key: 'uk', type: 'boolean', description: 'Word usually written using kana alone' }],
  ['v5u', { key: 'v5u', type: 'boolean', description: 'Godan verb with \'u\' ending' }],
  ['gramm', { key: 'gramm', type: 'boolean', description: 'Grammar' }],
  ['hanaf', { key: 'hanaf', type: 'boolean', description: 'Hanafuda' }],
  ['geom', { key: 'geom', type: 'boolean', description: 'Geometry' }],
  ['station', { key: 'station', type: 'boolean', description: 'Railway station' }],
  ['law', { key: 'law', type: 'boolean', description: 'Law' }],
  ['ctr', { key: 'ctr', type: 'boolean', description: 'Counter' }],
  ['aux-v', { key: 'aux-v', type: 'boolean', description: 'Auxiliary verb' }],
  ['leg', { key: 'leg', type: 'boolean', description: 'Legend' }],
  ['shogi', { key: 'shogi', type: 'boolean', description: 'Shogi' }],
  ['ik', { key: 'ik', type: 'boolean', description: 'Word containing irregular kana usage' }],
  ['v2h-k', { key: 'v2h-k', type: 'boolean', description: 'Nidan verb (upper class) with \'hu/fu\' ending (archaic)' }],
  ['v4h', { key: 'v4h', type: 'boolean', description: 'Yodan verb with \'hu/fu\' ending (archaic)' }],
  ['econ', { key: 'econ', type: 'boolean', description: 'Economics' }],
  ['work', { key: 'work', type: 'boolean', description: 'Work of art, literature, music, etc. name' }],
  ['conj', { key: 'conj', type: 'boolean', description: 'Conjunction' }],
  ['net-sl', { key: 'net-sl', type: 'boolean', description: 'Internet slang' }],
  ['dei', { key: 'dei', type: 'boolean', description: 'Deity' }],
  ['group', { key: 'group', type: 'boolean', description: 'Group' }],
  ['doc', { key: 'doc', type: 'boolean', description: 'Document' }],
  ['stat', { key: 'stat', type: 'boolean', description: 'Statistics' }],
  ['v2z-s', { key: 'v2z-s', type: 'boolean', description: 'Nidan verb (lower class) with \'zu\' ending (archaic)' }],
  ['vet', { key: 'vet', type: 'boolean', description: 'Veterinary terms' }],
  ['myth', { key: 'myth', type: 'boolean', description: 'Mythology' }],
  ['audvid', { key: 'audvid', type: 'boolean', description: 'Audiovisual' }],
  ['photo', { key: 'photo', type: 'boolean', description: 'Photography' }],
  ['rare', { key: 'rare', type: 'boolean', description: 'Rare term' }],
  ['anat', { key: 'anat', type: 'boolean', description: 'Anatomy' }],
  ['pn', { key: 'pn', type: 'boolean', description: 'Pronoun' }],
  ['art', { key: 'art', type: 'boolean', description: 'Art, aesthetics' }],
  ['adj-ix', { key: 'adj-ix', type: 'boolean', description: 'Adjective (keiyoushi) - yoi/ii class' }],
  ['psych', { key: 'psych', type: 'boolean', description: 'Psychology' }],
  ['sk', { key: 'sk', type: 'boolean', description: 'Search-only kana form' }],
  ['v2y-k', { key: 'v2y-k', type: 'boolean', description: 'Nidan verb (upper class) with \'yu\' ending (archaic)' }],
  ['unc', { key: 'unc', type: 'boolean', description: 'Unclassified' }],
  ['form', { key: 'form', type: 'boolean', description: 'Formal or literary term' }],
  ['ksb', { key: 'ksb', type: 'boolean', description: 'Kansai-ben' }],
  ['yoji', { key: 'yoji', type: 'boolean', description: 'Yojijukugo' }],
  ['ski', { key: 'ski', type: 'boolean', description: 'Skiing' }],
  ['v4k', { key: 'v4k', type: 'boolean', description: 'Yodan verb with \'ku\' ending (archaic)' }],
  ['bot', { key: 'bot', type: 'boolean', description: 'Botany' }],
  ['horse', { key: 'horse', type: 'boolean', description: 'Horse racing' }],
  ['vi', { key: 'vi', type: 'boolean', description: 'Intransitive verb' }],
  ['ornith', { key: 'ornith', type: 'boolean', description: 'Ornithology' }],
  ['on-mim', { key: 'on-mim', type: 'boolean', description: 'Onomatopoeic or mimetic word' }],
  ['vs-c', { key: 'vs-c', type: 'boolean', description: 'Su verb - precursor to the modern suru' }],
  ['archeol', { key: 'archeol', type: 'boolean', description: 'Archeology' }],
  ['stockm', { key: 'stockm', type: 'boolean', description: 'Stock market' }],
  ['med', { key: 'med', type: 'boolean', description: 'Medicine' }],
  ['vz', { key: 'vz', type: 'boolean', description: 'Ichidan verb - zuru verb (alternative form of -jiru verbs)' }],
  ['chem', { key: 'chem', type: 'boolean', description: 'Chemistry' }],
  ['phil', { key: 'phil', type: 'boolean', description: 'Philosophy' }],
  ['bus', { key: 'bus', type: 'boolean', description: 'Business' }],
  ['suf', { key: 'suf', type: 'boolean', description: 'Suffix' }],
  ['genet', { key: 'genet', type: 'boolean', description: 'Genetics' }],
  ['v2k-k', { key: 'v2k-k', type: 'boolean', description: 'Nidan verb (upper class) with \'ku\' ending (archaic)' }],
  ['baseb', { key: 'baseb', type: 'boolean', description: 'Baseball' }],
  ['nab', { key: 'nab', type: 'boolean', description: 'Nagano-ben' }],
  ['v2n-s', { key: 'v2n-s', type: 'boolean', description: 'Nidan verb (lower class) with \'nu\' ending (archaic)' }],
  ['masc', { key: 'masc', type: 'boolean', description: 'Male given name or forename' }],
  ['v1', { key: 'v1', type: 'boolean', description: 'Ichidan verb' }],
  ['v5t', { key: 'v5t', type: 'boolean', description: 'Godan verb with \'tsu\' ending' }],
  ['adj-pn', { key: 'adj-pn', type: 'boolean', description: 'Pre-noun adjectival (rentaishi)' }],
  ['math', { key: 'math', type: 'boolean', description: 'Mathematics' }],
  ['prowres', { key: 'prowres', type: 'boolean', description: 'Professional wrestling' }],
  ['adj-shiku', { key: 'adj-shiku', type: 'boolean', description: '\'shiku\' adjective (archaic)' }],
  ['astron', { key: 'astron', type: 'boolean', description: 'Astronomy' }],
  ['noh', { key: 'noh', type: 'boolean', description: 'Noh' }],
  ['n-suf', { key: 'n-suf', type: 'boolean', description: 'Noun, used as a suffix' }],
  ['v5k', { key: 'v5k', type: 'boolean', description: 'Godan verb with \'ku\' ending' }],
  ['adj-na', { key: 'adj-na', type: 'boolean', description: 'Adjectival nouns or quasi-adjectives (keiyodoshi)' }],
  ['kabuki', { key: 'kabuki', type: 'boolean', description: 'Kabuki' }],
  ['geogr', { key: 'geogr', type: 'boolean', description: 'Geography' }],
  ['v2r-k', { key: 'v2r-k', type: 'boolean', description: 'Nidan verb (upper class) with \'ru\' ending (archaic)' }],
  ['v5u-s', { key: 'v5u-s', type: 'boolean', description: 'Godan verb with \'u\' ending (special class)' }],
  ['civeng', { key: 'civeng', type: 'boolean', description: 'Civil engineering' }],
  ['obs', { key: 'obs', type: 'boolean', description: 'Obsolete term' }],
  ['n', { key: 'n', type: 'boolean', description: 'Noun (common) (futsuumeishi)' }],
  ['tradem', { key: 'tradem', type: 'boolean', description: 'Trademark' }],
  ['physics', { key: 'physics', type: 'boolean', description: 'Physics' }],
  ['food', { key: 'food', type: 'boolean', description: 'Food, cooking' }],
  ['biochem', { key: 'biochem', type: 'boolean', description: 'Biochemistry' }],
  ['place', { key: 'place', type: 'boolean', description: 'Place name' }],
  ['Shinto', { key: 'Shinto', type: 'boolean', description: 'Shinto' }],
  ['v1-s', { key: 'v1-s', type: 'boolean', description: 'Ichidan verb - kureru special class' }],
  ['v2y-s', { key: 'v2y-s', type: 'boolean', description: 'Nidan verb (lower class) with \'yu\' ending (archaic)' }],
  ['rkb', { key: 'rkb', type: 'boolean', description: 'Ryuukyuu-ben' }],
  ['tsug', { key: 'tsug', type: 'boolean', description: 'Tsugaru-ben' }],
  ['mil', { key: 'mil', type: 'boolean', description: 'Military' }],
  ['vidg', { key: 'vidg', type: 'boolean', description: 'Video games' }],
  ['v2k-s', { key: 'v2k-s', type: 'boolean', description: 'Nidan verb (lower class) with \'ku\' ending (archaic)' }],
  ['comp', { key: 'comp', type: 'boolean', description: 'Computing' }],
  ['v2g-s', { key: 'v2g-s', type: 'boolean', description: 'Nidan verb (lower class) with \'gu\' ending (archaic)' }],
  ['v2t-k', { key: 'v2t-k', type: 'boolean', description: 'Nidan verb (upper class) with \'tsu\' ending (archaic)' }],
  ['gardn', { key: 'gardn', type: 'boolean', description: 'Gardening, horticulture' }],
  ['v5n', { key: 'v5n', type: 'boolean', description: 'Godan verb with \'nu\' ending' }],
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

  if (def.valueType === 'string' && def.type === 'enum') {
    return (def.enumValues as string[])?.includes(value) ?? false
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
