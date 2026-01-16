-- JLPT Reference Database Initialization Script
-- This script runs when the PostgreSQL container is first created

-- Create extensions that might be useful for JLPT reference data
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create a schema for JLPT-specific tables
CREATE SCHEMA IF NOT EXISTS jlpt;

-- Set search path to include jlpt schema
SET search_path TO jlpt, public;

-- ============================================
-- META
-- ============================================
CREATE TABLE IF NOT EXISTS status (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    last_update TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ============================================
-- KANJI
-- ============================================
CREATE TABLE IF NOT EXISTS kanji (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    literal VARCHAR(1) NOT NULL UNIQUE,
    grade INTEGER,
    stroke_count INTEGER NOT NULL,
    frequency INTEGER,
    jlpt_level_old INTEGER,
    jlpt_level_new INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Codepoints for kanjis
CREATE TABLE IF NOT EXISTS kanji_codepoint (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    type VARCHAR(20) NOT NULL,
    value VARCHAR(50) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Kanji dictionary references
CREATE TABLE IF NOT EXISTS kanji_dictionary_reference (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL,
    value VARCHAR(50) NOT NULL,
    morohashi_volume INTEGER,
    morohashi_page INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Kanji query codes
CREATE TABLE IF NOT EXISTS kanji_query_code (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    type VARCHAR(30) NOT NULL,
    value VARCHAR(50) NOT NULL,
    skip_missclassification VARCHAR(30),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Kanji readings
CREATE TABLE IF NOT EXISTS kanji_reading (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    type VARCHAR(20) NOT NULL,
    value VARCHAR(50) NOT NULL,
    status VARCHAR(20),
    on_type VARCHAR(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Kanji meanings
CREATE TABLE IF NOT EXISTS kanji_meaning (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    lang VARCHAR(10) NOT NULL,
    value TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Kanji nanori
CREATE TABLE IF NOT EXISTS kanji_nanori (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    value VARCHAR(50) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


-- ============================================
-- RADICAL
-- ============================================
CREATE TABLE IF NOT EXISTS radical_group (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    canonical_literal VARCHAR(10) UNIQUE NOT NULL,
    kang_xi_number INTEGER,
    meanings TEXT[],
    readings TEXT[],
    notes TEXT[],
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Links any character (from source or reference, including variants) to its group
CREATE TABLE IF NOT EXISTS radical_group_member (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    group_id UUID NOT NULL REFERENCES radical_group(id) ON DELETE CASCADE,
    literal VARCHAR(10) NOT NULL,
    is_canonical BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(group_id, literal)
);

-- Source list from source.json for search engine and kanji composition
CREATE TABLE IF NOT EXISTS radical (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    literal VARCHAR(10) UNIQUE NOT NULL,
    stroke_count INTEGER,
    code VARCHAR(20),
    group_id UUID REFERENCES radical_group(id) ON DELETE SET NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Radicals used in kanji
CREATE TABLE IF NOT EXISTS kanji_radical (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    radical_id UUID NOT NULL REFERENCES radical(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(kanji_id, radical_id)
);

-- ============================================
-- VOCABULARY
-- ============================================
CREATE TABLE IF NOT EXISTS tag (
    code VARCHAR(50) PRIMARY KEY,
    description TEXT NOT NULL,
    category VARCHAR(30) NOT NULL,
    source TEXT[] NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Vocabulary
CREATE TABLE IF NOT EXISTS vocabulary (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    jmdict_id VARCHAR(20) UNIQUE NOT NULL,
    jlpt_level_new INTEGER,
    slug TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Kanji forms of vocabulary
CREATE TABLE IF NOT EXISTS vocabulary_kanji (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    vocabulary_id UUID NOT NULL REFERENCES vocabulary(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
    is_primary BOOLEAN DEFAULT FALSE,
    is_common BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Vocabulary kanji tags (references tag table)
CREATE TABLE IF NOT EXISTS vocabulary_kanji_tag (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    vocabulary_kanji_id UUID NOT NULL REFERENCES vocabulary_kanji(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Kana forms for vocabulary
CREATE TABLE IF NOT EXISTS vocabulary_kana (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    vocabulary_id UUID NOT NULL REFERENCES vocabulary(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
    is_primary BOOLEAN DEFAULT FALSE,
    applies_to_kanji TEXT[],
    is_common BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Vocabulary kana tags (references tag table)
CREATE TABLE vocabulary_kana_tag (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    vocabulary_kana_id UUID NOT NULL REFERENCES vocabulary_kana(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Vocabulary senses (definitions)
CREATE TABLE IF NOT EXISTS vocabulary_sense (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    vocabulary_id UUID NOT NULL REFERENCES vocabulary(id) ON DELETE CASCADE,
    applies_to_kanji TEXT[],
    applies_to_kana TEXT[],
    info TEXT[],
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Vocabulary sense tags (unified)
CREATE TABLE IF NOT EXISTS vocabulary_sense_tag (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    sense_id UUID NOT NULL REFERENCES vocabulary_sense(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    tag_type VARCHAR(20) NOT NULL CHECK (tag_type IN ('pos', 'field', 'dialect', 'misc')),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(sense_id, tag_code, tag_type)
);

-- Vocabulary relation
CREATE TABLE IF NOT EXISTS vocabulary_sense_relation (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    source_sense_id UUID NOT NULL REFERENCES vocabulary_sense(id) ON DELETE CASCADE,
    target_vocab_id UUID REFERENCES vocabulary(id) ON DELETE SET NULL,
    target_sense_id UUID REFERENCES vocabulary_sense(id) ON DELETE SET NULL,
    target_term TEXT NOT NULL,
    target_reading TEXT,
    relation_type TEXT NOT NULL CHECK (relation_type IN ('related', 'antonym')),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Language source
CREATE TABLE IF NOT EXISTS vocabulary_sense_language_source (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    sense_id UUID NOT NULL REFERENCES vocabulary_sense(id) ON DELETE CASCADE,
    lang VARCHAR(10),
    text TEXT,
    "full" BOOLEAN,
    wasei BOOLEAN,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Glosses
CREATE TABLE IF NOT EXISTS vocabulary_sense_gloss (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    sense_id UUID NOT NULL REFERENCES vocabulary_sense(id) ON DELETE CASCADE,
    lang VARCHAR(10) NOT NULL,
    text TEXT NOT NULL,
    gender VARCHAR(20),
    type VARCHAR(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Example sentences
CREATE TABLE IF NOT EXISTS vocabulary_sense_example (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    sense_id UUID NOT NULL REFERENCES vocabulary_sense(id) ON DELETE CASCADE,
    source_type VARCHAR(30),
    source_value VARCHAR(50),
    text TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Sentence translations
CREATE TABLE IF NOT EXISTS vocabulary_sense_example_sentence (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    example_id UUID NOT NULL REFERENCES vocabulary_sense_example(id) ON DELETE CASCADE,
    lang VARCHAR(10) NOT NULL,
    text TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Furigana data for vocabulary
CREATE TABLE IF NOT EXISTS vocabulary_furigana (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    vocabulary_id UUID NOT NULL REFERENCES vocabulary(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
    reading TEXT NOT NULL,
    furigana JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(vocabulary_id, text, reading)
);

-- ============================================
-- PROPER NOUNS (Names, Places, Companies)
-- ============================================
CREATE TABLE IF NOT EXISTS proper_noun (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    jmnedict_id VARCHAR(20) UNIQUE NOT NULL,
    slug TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Kanji forms of proper nouns
CREATE TABLE IF NOT EXISTS proper_noun_kanji (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    proper_noun_id UUID NOT NULL REFERENCES proper_noun(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
    is_primary BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Proper noun kanji tags
CREATE TABLE IF NOT EXISTS proper_noun_kanji_tag (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    proper_noun_kanji_id UUID NOT NULL REFERENCES proper_noun_kanji(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Kana readings for proper nouns
CREATE TABLE IF NOT EXISTS proper_noun_kana (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    proper_noun_id UUID NOT NULL REFERENCES proper_noun(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
    applies_to_kanji TEXT[],
    is_primary BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Proper noun kana tags
CREATE TABLE IF NOT EXISTS proper_noun_kana_tag (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    proper_noun_kana_id UUID NOT NULL REFERENCES proper_noun_kana(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Proper noun translations
CREATE TABLE IF NOT EXISTS proper_noun_translation (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    proper_noun_id UUID NOT NULL REFERENCES proper_noun(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Translation types
CREATE TABLE IF NOT EXISTS proper_noun_translation_type (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    translation_id UUID NOT NULL REFERENCES proper_noun_translation(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Related terms for proper nouns
CREATE TABLE IF NOT EXISTS proper_noun_translation_related (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    translation_id UUID NOT NULL REFERENCES proper_noun_translation(id) ON DELETE CASCADE,
    related_term TEXT NOT NULL,
    related_reading TEXT,
    reference_proper_noun_id UUID REFERENCES proper_noun(id) ON DELETE SET NULL,
    reference_proper_noun_translation_id UUID REFERENCES proper_noun_translation(id) ON DELETE SET NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Translation text
CREATE TABLE IF NOT EXISTS proper_noun_translation_text (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    translation_id UUID NOT NULL REFERENCES proper_noun_translation(id) ON DELETE CASCADE,
    lang VARCHAR(10) NOT NULL,
    text TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Furigana data for proper nouns
CREATE TABLE IF NOT EXISTS proper_noun_furigana (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    proper_noun_id UUID NOT NULL REFERENCES proper_noun(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
    reading TEXT NOT NULL,
    furigana JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(proper_noun_id, text, reading)
);

-- ============================================
-- RELATIONSHIPS
-- ============================================
CREATE TABLE IF NOT EXISTS vocabulary_uses_kanji (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    vocabulary_id UUID NOT NULL REFERENCES vocabulary(id) ON DELETE CASCADE,
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(vocabulary_id, kanji_id)
);

CREATE TABLE IF NOT EXISTS proper_noun_uses_kanji (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    proper_noun_id UUID NOT NULL REFERENCES proper_noun(id) ON DELETE CASCADE,
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(proper_noun_id, kanji_id)
);

-- ============================================
-- KANJI INDEXES
-- ============================================
CREATE INDEX IF NOT EXISTS idx_kanji_literal ON kanji(literal);
CREATE INDEX IF NOT EXISTS idx_kanji_frequency ON kanji(frequency);
CREATE INDEX IF NOT EXISTS idx_kanji_jlpt ON kanji(jlpt_level_new);
CREATE INDEX IF NOT EXISTS idx_kanji_codepoint_kanji ON kanji_codepoint(kanji_id);
CREATE INDEX IF NOT EXISTS idx_kanji_dict_ref_kanji ON kanji_dictionary_reference(kanji_id);
CREATE INDEX IF NOT EXISTS idx_kanji_dict_ref_type ON kanji_dictionary_reference(type);
CREATE INDEX IF NOT EXISTS idx_kanji_query_code_kanji ON kanji_query_code(kanji_id);
CREATE INDEX IF NOT EXISTS idx_kanji_reading_kanji ON kanji_reading(kanji_id);
CREATE INDEX IF NOT EXISTS idx_kanji_reading_type ON kanji_reading(type);
CREATE INDEX IF NOT EXISTS idx_kanji_meaning_kanji ON kanji_meaning(kanji_id);
CREATE INDEX IF NOT EXISTS idx_kanji_meaning_lang ON kanji_meaning(lang);
CREATE INDEX IF NOT EXISTS idx_kanji_nanori_kanji ON kanji_nanori(kanji_id);
-- JLPT level with frequency for pagination
CREATE INDEX IF NOT EXISTS idx_kanji_jlpt_frequency ON kanji(jlpt_level_new, frequency DESC, id);
-- Stroke count for kanji lookup
CREATE INDEX IF NOT EXISTS idx_kanji_stroke_count ON kanji(stroke_count, jlpt_level_new);
-- Grade level for educational content
CREATE INDEX IF NOT EXISTS idx_kanji_grade ON kanji(grade, jlpt_level_new);
-- Frequency-based ordering
CREATE INDEX IF NOT EXISTS idx_kanji_frequency_desc ON kanji(frequency DESC NULLS LAST);
-- Radical-based kanji lookup
CREATE INDEX IF NOT EXISTS idx_kanji_radical_lookup ON kanji_radical(radical_id, kanji_id);
-- Kanji readings for search
CREATE INDEX IF NOT EXISTS idx_kanji_reading_search ON kanji_reading(kanji_id, type, value);
-- Kanji meanings for English search
CREATE INDEX IF NOT EXISTS idx_kanji_literal_trgm ON kanji USING gist (literal gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_kanji_literal_trgm_gin ON kanji USING gin (literal gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_kanji_meaning_value_trgm ON kanji_meaning USING gist(value gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_kanji_meaning_value_trgm_gin ON kanji_meaning USING gin(value gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_kanji_reading_value_trgm ON kanji_reading USING gist(value gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_kanji_reading_value_trgm_gin ON kanji_reading USING gin(value gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_kanji_meaning_lang_id ON kanji_meaning(lang, kanji_id);

-- ============================================
-- VOCABULARY INDEXES
-- ============================================
CREATE INDEX IF NOT EXISTS idx_tag_category ON tag(category);
CREATE INDEX IF NOT EXISTS idx_vocabulary_jlpt ON vocabulary(jlpt_level_new);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_vocab ON vocabulary_kanji(vocabulary_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_text ON vocabulary_kanji(text);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_tag_vocab_kanji ON vocabulary_kanji_tag(vocabulary_kanji_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_tag_code ON vocabulary_kanji_tag(tag_code);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_vocab ON vocabulary_kana(vocabulary_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_text ON vocabulary_kana(text);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_gloss_sense_id_lang ON vocabulary_sense_gloss(sense_id, lang);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_tag_vocab_kana ON vocabulary_kana_tag(vocabulary_kana_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_tag_code ON vocabulary_kana_tag(tag_code);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_vocab ON vocabulary_sense(vocabulary_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_tag_sense ON vocabulary_sense_tag(sense_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_tag_tag ON vocabulary_sense_tag(tag_code);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_tag_type ON vocabulary_sense_tag(tag_type);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_tag_sense_type ON vocabulary_sense_tag(sense_id, tag_type);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_relation_source ON vocabulary_sense_relation(source_sense_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_relation_target ON vocabulary_sense_relation(target_vocab_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_language_source_sense ON vocabulary_sense_language_source(sense_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_gloss_sense ON vocabulary_sense_gloss(sense_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_gloss_lang ON vocabulary_sense_gloss(lang);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_example_sense ON vocabulary_sense_example(sense_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_example_sentence_example ON vocabulary_sense_example_sentence(example_id);
-- JLPT level vocabulary with common status
CREATE INDEX IF NOT EXISTS idx_vocabulary_jlpt_common ON vocabulary(jlpt_level_new, id);
-- Vocabulary kanji text search (Japanese)
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_text_trgm ON vocabulary_kanji USING gist (text gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_text_trgm_gin ON vocabulary_kanji USING gin (text gin_trgm_ops);
-- Vocabulary kana text search (Japanese)
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_text_trgm ON vocabulary_kana USING gist (text gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_text_trgm_gin ON vocabulary_kana USING gin (text gin_trgm_ops);
-- Common vocabulary optimization
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_common_all ON vocabulary_kana(vocabulary_id, is_common);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_common_all ON vocabulary_kanji(vocabulary_id, is_common);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_common ON vocabulary_kanji(vocabulary_id, is_common) WHERE is_common = true;
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_common ON vocabulary_kana(vocabulary_id, is_common) WHERE is_common = true;
-- Primary form lookup optimization
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_primary ON vocabulary_kanji(vocabulary_id, is_primary) WHERE is_primary = true;
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_primary ON vocabulary_kana(vocabulary_id, is_primary) WHERE is_primary = true;
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_text_primary ON vocabulary_kanji(text, is_primary) WHERE is_primary = true;
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_text_primary ON vocabulary_kana(text, is_primary) WHERE is_primary = true;
-- Vocabulary sense glosses for English search
CREATE INDEX IF NOT EXISTS idx_vocabulary_gloss_text_trgm ON vocabulary_sense_gloss USING gist (text gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_vocabulary_gloss_text_trgm_gin ON vocabulary_sense_gloss USING gin (text gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_vocabulary_gloss_lang ON vocabulary_sense_gloss(lang, sense_id);
-- Sense tag filtering
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_tag_lookup ON vocabulary_sense_tag(sense_id, tag_type, tag_code);
-- Vocabulary relationships
CREATE INDEX IF NOT EXISTS idx_vocabulary_uses_kanji_lookup ON vocabulary_uses_kanji(vocabulary_id, kanji_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_uses_kanji_reverse ON vocabulary_uses_kanji(kanji_id, vocabulary_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_text_pattern_ops ON vocabulary_kana (text varchar_pattern_ops);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_text_pattern_ops ON vocabulary_kanji (text varchar_pattern_ops);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_gloss_text_pattern_ops ON vocabulary_sense_gloss (text varchar_pattern_ops);

-- ============================================
-- PROPER NOUN INDEXES
-- ============================================
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_noun ON proper_noun_kanji(proper_noun_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_text ON proper_noun_kanji(text);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_tag_noun_kanji ON proper_noun_kanji_tag(proper_noun_kanji_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_tag_code ON proper_noun_kanji_tag(tag_code);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_noun ON proper_noun_kana(proper_noun_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_translation_text_translation_id_lang ON proper_noun_translation_text(translation_id, lang);
-- Primary form lookup optimization
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_primary ON proper_noun_kanji(proper_noun_id, is_primary) WHERE is_primary = true;
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_primary ON proper_noun_kana(proper_noun_id, is_primary) WHERE is_primary = true;
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_text_primary ON proper_noun_kanji(text, is_primary) WHERE is_primary = true;
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_text_primary ON proper_noun_kana(text, is_primary) WHERE is_primary = true;
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_tag_noun_kana ON proper_noun_kana_tag(proper_noun_kana_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_tag_code ON proper_noun_kana_tag(tag_code);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_type_trans ON proper_noun_translation_type(translation_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_type_tag ON proper_noun_translation_type(tag_code);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_related_trans ON proper_noun_translation_related(translation_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_related_ref_trans ON proper_noun_translation_related(reference_proper_noun_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_text_trans ON proper_noun_translation_text(translation_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_text_lang ON proper_noun_translation_text(lang);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_text_lang_id ON proper_noun_translation_text(translation_id, lang);
-- Proper noun text search
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_text_pattern_ops ON proper_noun_kanji (text varchar_pattern_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_text_pattern_ops ON proper_noun_kana (text varchar_pattern_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_translation_text_pattern_ops ON proper_noun_translation_text (text varchar_pattern_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_text_trgm ON proper_noun_kanji USING gist (text gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_text_trgm_gin ON proper_noun_kanji USING gin (text gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_text_trgm ON proper_noun_kana USING gist (text gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_text_trgm_gin ON proper_noun_kana USING gin (text gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_translation_text_trgm ON proper_noun_translation_text USING gist (text gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_translation_text_trgm_gin ON proper_noun_translation_text USING gin (text gin_trgm_ops);
-- FK indexes (for JOINs)
CREATE INDEX IF NOT EXISTS idx_proper_noun_translation_pn_id ON jlpt.proper_noun_translation (proper_noun_id);
-- Proper noun relationships
CREATE INDEX IF NOT EXISTS idx_proper_noun_uses_kanji_lookup ON proper_noun_uses_kanji(proper_noun_id, kanji_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_uses_kanji_reverse ON proper_noun_uses_kanji(kanji_id, proper_noun_id);

-- ============================================
-- RADICAL INDEXES
-- ============================================
CREATE INDEX IF NOT EXISTS idx_radical_literal ON radical(literal);
CREATE INDEX IF NOT EXISTS idx_radical_group_id ON radical(group_id);
CREATE INDEX IF NOT EXISTS idx_kanji_radical_kanji ON kanji_radical(kanji_id);
CREATE INDEX IF NOT EXISTS idx_kanji_radical_radical ON kanji_radical(radical_id);
-- Radical lookup by stroke count
CREATE INDEX IF NOT EXISTS idx_radical_stroke_count ON radical(stroke_count, literal);
-- Radical group indexes
CREATE INDEX IF NOT EXISTS idx_radical_group_canonical ON radical_group(canonical_literal);
CREATE INDEX IF NOT EXISTS idx_radical_group_kx ON radical_group(kang_xi_number);
CREATE INDEX IF NOT EXISTS idx_radical_group_member_group ON radical_group_member(group_id);
CREATE INDEX IF NOT EXISTS idx_radical_group_member_literal ON radical_group_member(literal);

-- ============================================
-- RELATIONSHIPS INDEXES
-- ============================================
CREATE INDEX IF NOT EXISTS idx_vocab_uses_kanji_vocab ON vocabulary_uses_kanji(vocabulary_id);
CREATE INDEX IF NOT EXISTS idx_vocab_uses_kanji_kanji ON vocabulary_uses_kanji(kanji_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_uses_kanji_noun ON proper_noun_uses_kanji(proper_noun_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_uses_kanji_kanji ON proper_noun_uses_kanji(kanji_id);

-- ============================================
-- TAG PERFORMANCE INDEXES
-- ============================================
-- Tag category lookup
CREATE INDEX IF NOT EXISTS idx_tag_category_lookup ON tag(category, code);

-- ============================================
-- TAG FILTERING COMPOSITE INDEXES
-- (Critical for filter_tags = ANY() queries)
-- ============================================
-- Proper noun tag filtering
CREATE INDEX IF NOT EXISTS idx_pn_kanji_tag_code_fk 
    ON proper_noun_kanji_tag(tag_code, proper_noun_kanji_id);
CREATE INDEX IF NOT EXISTS idx_pn_kana_tag_code_fk 
    ON proper_noun_kana_tag(tag_code, proper_noun_kana_id);
CREATE INDEX IF NOT EXISTS idx_pn_trans_type_code_fk 
    ON proper_noun_translation_type(tag_code, translation_id);

-- Vocabulary tag filtering
CREATE INDEX IF NOT EXISTS idx_vocab_kanji_tag_code_fk 
    ON vocabulary_kanji_tag(tag_code, vocabulary_kanji_id);
CREATE INDEX IF NOT EXISTS idx_vocab_kana_tag_code_fk 
    ON vocabulary_kana_tag(tag_code, vocabulary_kana_id);
CREATE INDEX IF NOT EXISTS idx_vocab_sense_tag_code_fk 
    ON vocabulary_sense_tag(tag_code, sense_id);

-- ============================================
-- COMPOSITE INDEXES FOR COMPLEX QUERIES
-- ============================================
-- Kanji with readings and meanings (for detailed views)
CREATE INDEX IF NOT EXISTS idx_kanji_detailed ON kanji(id, jlpt_level_new, stroke_count, frequency);
-- Vocabulary with senses (for detailed views)
CREATE INDEX IF NOT EXISTS idx_vocabulary_detailed ON vocabulary(id, jlpt_level_new);

-- ============================================
-- SLUG INDEXES
-- ============================================
CREATE INDEX IF NOT EXISTS idx_vocabulary_slug ON vocabulary(slug);
CREATE INDEX IF NOT EXISTS idx_proper_noun_slug ON proper_noun(slug);