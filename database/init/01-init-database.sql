-- JLPT Reference Database Initialization Script
-- This script runs when the PostgreSQL container is first created

-- Create the main database if it doesn't exist
-- (The database is already created by POSTGRES_DB environment variable)

-- Create extensions that might be useful for JLPT reference data
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create a schema for JLPT-specific tables
CREATE SCHEMA IF NOT EXISTS jlpt;

-- Set search path to include jlpt schema
SET search_path TO jlpt, public;

-- Create a function to update the updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- ============================================
-- KANJI
-- ============================================
CREATE TABLE IF NOT EXISTS kanji (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    literal CHAR(1) NOT NULL UNIQUE,
    grade INTEGER,
    stroke_count INTEGER NOT NULL,
    frequency INTEGER,
    jlpt_level_old INTEGER,
    jlpt_level_new INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_kanji_literal ON kanji(literal);
CREATE INDEX IF NOT EXISTS idx_kanji_frequency ON kanji(frequency);
CREATE INDEX IF NOT EXISTS idx_kanji_jlpt ON kanji(jlpt_level_new);

-- Codepoints for kanjis
CREATE TABLE IF NOT EXISTS kanji_codepoint (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    type VARCHAR(20) NOT NULL,
    value VARCHAR(50) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_kanji_codepoint_kanji ON kanji_codepoint(kanji_id);

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
CREATE INDEX IF NOT EXISTS idx_kanji_dict_ref_kanji ON kanji_dictionary_reference(kanji_id);
CREATE INDEX IF NOT EXISTS idx_kanji_dict_ref_type ON kanji_dictionary_reference(type);

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
CREATE INDEX IF NOT EXISTS idx_kanji_query_code_kanji ON kanji_query_code(kanji_id);

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
CREATE INDEX IF NOT EXISTS idx_kanji_reading_kanji ON kanji_reading(kanji_id);
CREATE INDEX IF NOT EXISTS idx_kanji_reading_type ON kanji_reading(type);

-- Kanji meanings
CREATE TABLE IF NOT EXISTS kanji_meaning (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    lang VARCHAR(10) NOT NULL,
    value TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_kanji_meaning_kanji ON kanji_meaning(kanji_id);
CREATE INDEX IF NOT EXISTS idx_kanji_meaning_lang ON kanji_meaning(lang);

-- Kanji nanori
CREATE TABLE IF NOT EXISTS kanji_nanori (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    value VARCHAR(50) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_kanji_nanori_kanji ON kanji_nanori(kanji_id);


-- ============================================
-- RADICAL
-- ============================================
CREATE TABLE IF NOT EXISTS radical (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    literal CHAR(1) UNIQUE NOT NULL,
    stroke_count INTEGER,
    code VARCHAR(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_radical_literal ON radical(literal);

-- Radicals used in kanji
CREATE TABLE IF NOT EXISTS kanji_radical (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    radical_id UUID NOT NULL REFERENCES radical(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(kanji_id, radical_id)
);
CREATE INDEX IF NOT EXISTS idx_kanji_radical_kanji ON kanji_radical(kanji_id);
CREATE INDEX IF NOT EXISTS idx_kanji_radical_radical ON kanji_radical(radical_id);


-- ============================================
-- VOCABULARY
-- ============================================
CREATE TABLE IF NOT EXISTS tag (
    code VARCHAR(50) PRIMARY KEY,
    description TEXT NOT NULL,
    category VARCHAR(30) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_tag_category ON tag(category);

-- Vocabulary
CREATE TABLE IF NOT EXISTS vocabulary (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    jmdict_id VARCHAR(20) UNIQUE NOT NULL,
    jlpt_level_new INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_vocabulary_jlpt ON vocabulary(jlpt_level_new);

-- Kanji forms of vocabulary
CREATE TABLE IF NOT EXISTS vocabulary_kanji (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    vocabulary_id UUID NOT NULL REFERENCES vocabulary(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
    is_common BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_vocab ON vocabulary_kanji(vocabulary_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_text ON vocabulary_kanji(text);

-- Vocabulary kanji tags (references tag table)
CREATE TABLE IF NOT EXISTS vocabulary_kanji_tag (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    vocabulary_kanji_id UUID NOT NULL REFERENCES vocabulary_kanji(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_tag_vocab_kanji ON vocabulary_kanji_tag(vocabulary_kanji_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_tag_code ON vocabulary_kanji_tag(tag_code);

-- Kana forms for vocabulary
CREATE TABLE IF NOT EXISTS vocabulary_kana (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    vocabulary_id UUID NOT NULL REFERENCES vocabulary(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
    applies_to_kanji TEXT[],
    is_common BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_vocab ON vocabulary_kana(vocabulary_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_text ON vocabulary_kana(text);

-- Vocabulary kana tags (references tag table)
CREATE TABLE vocabulary_kana_tag (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    vocabulary_kana_id UUID NOT NULL REFERENCES vocabulary_kana(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_tag_vocab_kana ON vocabulary_kana_tag(vocabulary_kana_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_tag_code ON vocabulary_kana_tag(tag_code);

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
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_vocab ON vocabulary_sense(vocabulary_id);

-- Part of speech for senses
CREATE TABLE IF NOT EXISTS vocabulary_sense_pos (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    sense_id UUID NOT NULL REFERENCES vocabulary_sense(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_pos_sense ON vocabulary_sense_pos(sense_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_pos_tag ON vocabulary_sense_pos(tag_code);

-- Field tags for senses
CREATE TABLE IF NOT EXISTS vocabulary_sense_field (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    sense_id UUID NOT NULL REFERENCES vocabulary_sense(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_field_sense ON vocabulary_sense_field(sense_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_field_tag ON vocabulary_sense_field(tag_code);

-- Dialect tags for senses
CREATE TABLE IF NOT EXISTS vocabulary_sense_dialect (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    sense_id UUID NOT NULL REFERENCES vocabulary_sense(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_dialect_sense ON vocabulary_sense_dialect(sense_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_dialect_tag ON vocabulary_sense_dialect(tag_code);

-- Misc tags for senses
CREATE TABLE IF NOT EXISTS vocabulary_sense_misc (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    sense_id UUID NOT NULL REFERENCES vocabulary_sense(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_misc_sense ON vocabulary_sense_misc(sense_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_misc_tag ON vocabulary_sense_misc(tag_code);

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
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_relation_source ON vocabulary_sense_relation(source_sense_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_relation_target ON vocabulary_sense_relation(target_vocab_id);

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
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_language_source_sense ON vocabulary_sense_language_source(sense_id);

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
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_gloss_sense ON vocabulary_sense_gloss(sense_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_gloss_lang ON vocabulary_sense_gloss(lang);

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
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_example_sense ON vocabulary_sense_example(sense_id);

-- Sentence translations
CREATE TABLE IF NOT EXISTS vocabulary_sense_example_sentence (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    example_id UUID NOT NULL REFERENCES vocabulary_sense_example(id) ON DELETE CASCADE,
    lang VARCHAR(10) NOT NULL,
    text TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_example_sentence_example ON vocabulary_sense_example_sentence(example_id);


-- ============================================
-- PROPER NOUNS (Names, Places, Companies)
-- ============================================
CREATE TABLE IF NOT EXISTS proper_noun (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    jmnedict_id VARCHAR(20) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Kanji forms of proper nouns
CREATE TABLE IF NOT EXISTS proper_noun_kanji (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    proper_noun_id UUID NOT NULL REFERENCES proper_noun(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_noun ON proper_noun_kanji(proper_noun_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_text ON proper_noun_kanji(text);

-- Proper noun kanji tags
CREATE TABLE IF NOT EXISTS proper_noun_kanji_tag (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    proper_noun_kanji_id UUID NOT NULL REFERENCES proper_noun_kanji(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_tag_noun_kanji ON proper_noun_kanji_tag(proper_noun_kanji_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_tag_code ON proper_noun_kanji_tag(tag_code);

-- Kana readings for proper nouns
CREATE TABLE IF NOT EXISTS proper_noun_kana (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    proper_noun_id UUID NOT NULL REFERENCES proper_noun(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
    applies_to_kanji TEXT[],
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_noun ON proper_noun_kana(proper_noun_id);

-- Proper noun kana tags
CREATE TABLE IF NOT EXISTS proper_noun_kana_tag (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    proper_noun_kana_id UUID NOT NULL REFERENCES proper_noun_kana(id) ON DELETE CASCADE,
    tag_code VARCHAR(50) NOT NULL REFERENCES tag(code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_tag_noun_kana ON proper_noun_kana_tag(proper_noun_kana_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_tag_code ON proper_noun_kana_tag(tag_code);

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
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_type_trans ON proper_noun_translation_type(translation_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_type_tag ON proper_noun_translation_type(tag_code);

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
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_related_trans ON proper_noun_translation_related(translation_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_related_ref_trans ON proper_noun_translation_related(reference_proper_noun_id);

-- Translation text
CREATE TABLE IF NOT EXISTS proper_noun_translation_text (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    translation_id UUID NOT NULL REFERENCES proper_noun_translation(id) ON DELETE CASCADE,
    lang VARCHAR(10) NOT NULL,
    text TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_text_trans ON proper_noun_translation_text(translation_id);

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
CREATE INDEX IF NOT EXISTS idx_vocab_uses_kanji_vocab ON vocabulary_uses_kanji(vocabulary_id);
CREATE INDEX IF NOT EXISTS idx_vocab_uses_kanji_kanji ON vocabulary_uses_kanji(kanji_id);

CREATE TABLE IF NOT EXISTS proper_noun_uses_kanji (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    proper_noun_id UUID NOT NULL REFERENCES proper_noun(id) ON DELETE CASCADE,
    kanji_id UUID NOT NULL REFERENCES kanji(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(proper_noun_id, kanji_id)
);
CREATE INDEX IF NOT EXISTS idx_proper_noun_uses_kanji_noun ON proper_noun_uses_kanji(proper_noun_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_uses_kanji_kanji ON proper_noun_uses_kanji(kanji_id);

-- ============================================
-- COMPREHENSIVE PERFORMANCE INDEXES
-- ============================================

-- ============================================
-- KANJI PERFORMANCE INDEXES
-- ============================================
-- JLPT level with frequency for pagination
CREATE INDEX idx_kanji_jlpt_frequency ON kanji(jlpt_level_new, frequency DESC, id);

-- Stroke count for kanji lookup
CREATE INDEX idx_kanji_stroke_count ON kanji(stroke_count, jlpt_level_new);

-- Grade level for educational content
CREATE INDEX idx_kanji_grade ON kanji(grade, jlpt_level_new);

-- Frequency-based ordering
CREATE INDEX idx_kanji_frequency_desc ON kanji(frequency DESC NULLS LAST);

-- Radical-based kanji lookup
CREATE INDEX idx_kanji_radical_lookup ON kanji_radical(radical_id, kanji_id);

-- Kanji readings for search
CREATE INDEX idx_kanji_reading_search ON kanji_reading(kanji_id, type, value);

-- Kanji meanings for English search
CREATE INDEX idx_kanji_meaning_fts ON kanji_meaning USING gin(to_tsvector('english', value));
CREATE INDEX idx_kanji_meaning_lang_id ON kanji_meaning(lang, kanji_id);

-- ============================================
-- VOCABULARY PERFORMANCE INDEXES
-- ============================================
-- JLPT level vocabulary with common status
CREATE INDEX idx_vocabulary_jlpt_common ON vocabulary(jlpt_level_new, id);

-- Vocabulary kanji text search (Japanese)
CREATE INDEX idx_vocabulary_kanji_text_gin ON vocabulary_kanji USING gin(to_tsvector('japanese', text));
CREATE INDEX idx_vocabulary_kanji_text_trgm ON vocabulary_kanji USING gin(text gin_trgm_ops);

-- Vocabulary kana text search (Japanese)
CREATE INDEX idx_vocabulary_kana_text_gin ON vocabulary_kana USING gin(to_tsvector('japanese', text));
CREATE INDEX idx_vocabulary_kana_text_trgm ON vocabulary_kana USING gin(text gin_trgm_ops);

-- Common vocabulary optimization
CREATE INDEX idx_vocabulary_kanji_common ON vocabulary_kanji(vocabulary_id, is_common) WHERE is_common = true;
CREATE INDEX idx_vocabulary_kana_common ON vocabulary_kana(vocabulary_id, is_common) WHERE is_common = true;

-- Vocabulary sense glosses for English search
CREATE INDEX idx_vocabulary_gloss_fts ON vocabulary_sense_gloss USING gin(to_tsvector('english', text));
CREATE INDEX idx_vocabulary_gloss_lang ON vocabulary_sense_gloss(lang, sense_id);

-- Part of speech filtering
CREATE INDEX idx_vocabulary_sense_pos_lookup ON vocabulary_sense_pos(sense_id, tag_code);

-- Vocabulary relationships
CREATE INDEX idx_vocabulary_uses_kanji_lookup ON vocabulary_uses_kanji(vocabulary_id, kanji_id);
CREATE INDEX idx_vocabulary_uses_kanji_reverse ON vocabulary_uses_kanji(kanji_id, vocabulary_id);

-- ============================================
-- PROPER NOUN PERFORMANCE INDEXES
-- ============================================
-- Proper noun text search
CREATE INDEX idx_proper_noun_kanji_text_gin ON proper_noun_kanji USING gin(to_tsvector('japanese', text));
CREATE INDEX idx_proper_noun_kana_text_gin ON proper_noun_kana USING gin(to_tsvector('japanese', text));

-- Proper noun relationships
CREATE INDEX idx_proper_noun_uses_kanji_lookup ON proper_noun_uses_kanji(proper_noun_id, kanji_id);
CREATE INDEX idx_proper_noun_uses_kanji_reverse ON proper_noun_uses_kanji(kanji_id, proper_noun_id);

-- ============================================
-- RADICAL PERFORMANCE INDEXES
-- ============================================
-- Radical lookup by stroke count
CREATE INDEX idx_radical_stroke_count ON radical(stroke_count, literal);

-- ============================================
-- TAG PERFORMANCE INDEXES
-- ============================================
-- Tag category lookup
CREATE INDEX idx_tag_category_lookup ON tag(category, code);

-- ============================================
-- COMPOSITE INDEXES FOR COMPLEX QUERIES
-- ============================================
-- Kanji with readings and meanings (for detailed views)
CREATE INDEX idx_kanji_detailed ON kanji(id, jlpt_level_new, stroke_count, frequency);

-- Vocabulary with senses (for detailed views)
CREATE INDEX idx_vocabulary_detailed ON vocabulary(id, jlpt_level_new);

-- ============================================
-- SEARCH OPTIMIZATION INDEXES
-- ============================================
-- Prefix search optimization
CREATE INDEX idx_vocabulary_kanji_prefix ON vocabulary_kanji(text varchar_pattern_ops);
CREATE INDEX idx_vocabulary_kana_prefix ON vocabulary_kana(text varchar_pattern_ops);
CREATE INDEX idx_kanji_reading_prefix ON kanji_reading(value varchar_pattern_ops);

-- Suffix search optimization
CREATE INDEX idx_vocabulary_kanji_suffix ON vocabulary_kanji(reverse(text) varchar_pattern_ops);
CREATE INDEX idx_vocabulary_kana_suffix ON vocabulary_kana(reverse(text) varchar_pattern_ops);

-- ============================================
-- UTILITY VIEWS
-- ============================================
-- View for kanji with all basic info
CREATE VIEW IF NOT EXISTS kanji_summary AS
SELECT 
    k.id,
    k.literal,
    k.grade,
    k.stroke_count,
    k.frequency,
    k.jlpt_level_old,
    k.jlpt_level_new,
    r.literal as radical_literal,
    STRING_AGG(DISTINCT kr.value, ', ' ORDER BY kr.value) FILTER (WHERE kr.type = 'ja_on') as on_readings,
    STRING_AGG(DISTINCT kr.value, ', ' ORDER BY kr.value) FILTER (WHERE kr.type = 'ja_kun') as kun_readings,
    STRING_AGG(DISTINCT km.value, '; ' ORDER BY km.value) FILTER (WHERE km.lang = 'en') as meanings_en
FROM kanji k
LEFT JOIN radical r ON r.id = k.classical_radical
LEFT JOIN kanji_reading kr ON kr.kanji_id = k.id
LEFT JOIN kanji_meaning km ON km.kanji_id = k.id
GROUP BY k.id, k.literal, k.grade, k.stroke_count, k.frequency, k.jlpt_level_old, k.jlpt_level_new, r.literal;

-- View for vocabulary with basic info and tags
CREATE VIEW IF NOT EXISTS vocabulary_summary AS
SELECT 
    v.id,
    STRING_AGG(DISTINCT vk.text, ', ' ORDER BY vk.text) as kanji_forms,
    STRING_AGG(DISTINCT vka.text, ', ' ORDER BY vka.text) as kana_readings,
    v.jlpt_level_new,
    STRING_AGG(DISTINCT t_pos.code, ', ') as part_of_speech_tags,
    STRING_AGG(DISTINCT t_field.code, ', ') as field_tags,
    STRING_AGG(DISTINCT t_dialect.code, ', ') as dialect_tags,
    STRING_AGG(DISTINCT t_misc.code, ', ') as misc_tags
FROM vocabulary v
LEFT JOIN vocabulary_kanji vk ON vk.vocabulary_id = v.id
LEFT JOIN vocabulary_kana vka ON vka.vocabulary_id = v.id
LEFT JOIN vocabulary_sense vs ON vs.vocabulary_id = v.id
LEFT JOIN vocabulary_sense_pos vsp ON vsp.sense_id = vs.id
LEFT JOIN tag t_pos ON t_pos.code = vsp.tag_code
LEFT JOIN vocabulary_sense_field vsf ON vsf.sense_id = vs.id
LEFT JOIN tag t_field ON t_field.code = vsf.tag_code
LEFT JOIN vocabulary_sense_dialect vsd ON vsd.sense_id = vs.id
LEFT JOIN tag t_dialect ON t_dialect.code = vsd.tag_code
LEFT JOIN vocabulary_sense_misc vsm ON vsm.sense_id = vs.id
LEFT JOIN tag t_misc ON t_misc.code = vsm.tag_code
GROUP BY v.id, v.jlpt_level_new;

-- ============================================
-- MATERIALIZED VIEWS FOR PERFORMANCE
-- ============================================

-- Kanji statistics by JLPT level
CREATE MATERIALIZED VIEW kanji_stats AS
SELECT 
    jlpt_level_new,
    COUNT(*) as kanji_count,
    AVG(stroke_count) as avg_strokes,
    AVG(frequency) as avg_frequency,
    MIN(stroke_count) as min_strokes,
    MAX(stroke_count) as max_strokes,
    MIN(frequency) as min_frequency,
    MAX(frequency) as max_frequency
FROM kanji 
WHERE jlpt_level_new IS NOT NULL
GROUP BY jlpt_level_new;

-- Vocabulary statistics by JLPT level
CREATE MATERIALIZED VIEW vocabulary_stats AS
SELECT 
    v.jlpt_level_new,
    COUNT(DISTINCT v.id) as vocabulary_count,
    COUNT(DISTINCT vk.id) as kanji_form_count,
    COUNT(DISTINCT vka.id) as kana_form_count,
    COUNT(DISTINCT vs.id) as sense_count
FROM vocabulary v
LEFT JOIN vocabulary_kanji vk ON vk.vocabulary_id = v.id
LEFT JOIN vocabulary_kana vka ON vka.vocabulary_id = v.id
LEFT JOIN vocabulary_sense vs ON vs.vocabulary_id = v.id
WHERE v.jlpt_level_new IS NOT NULL
GROUP BY v.jlpt_level_new;

-- Radical usage statistics
CREATE MATERIALIZED VIEW radical_stats AS
SELECT 
    r.id as radical_id,
    r.literal as radical_literal,
    r.stroke_count,
    COUNT(kr.kanji_id) as kanji_count,
    COUNT(DISTINCT k.jlpt_level_new) as jlpt_levels_used
FROM radical r
LEFT JOIN kanji_radical kr ON kr.radical_id = r.id
LEFT JOIN kanji k ON k.id = kr.kanji_id
GROUP BY r.id, r.literal, r.stroke_count;

-- Most common kanji by frequency
CREATE MATERIALIZED VIEW common_kanji AS
SELECT 
    k.id,
    k.literal,
    k.jlpt_level_new,
    k.stroke_count,
    k.frequency,
    STRING_AGG(DISTINCT kr.value, ', ' ORDER BY kr.value) FILTER (WHERE kr.type = 'ja_on') as on_readings,
    STRING_AGG(DISTINCT kr.value, ', ' ORDER BY kr.value) FILTER (WHERE kr.type = 'ja_kun') as kun_readings,
    STRING_AGG(DISTINCT km.value, '; ' ORDER BY km.value) FILTER (WHERE km.lang = 'en') as meanings_en
FROM kanji k
LEFT JOIN kanji_reading kr ON kr.kanji_id = k.id
LEFT JOIN kanji_meaning km ON km.kanji_id = k.id
WHERE k.frequency IS NOT NULL AND k.frequency > 0
GROUP BY k.id, k.literal, k.jlpt_level_new, k.stroke_count, k.frequency
ORDER BY k.frequency ASC
LIMIT 1000;

-- Vocabulary with kanji relationships
CREATE MATERIALIZED VIEW vocabulary_kanji_relationships AS
SELECT 
    v.id as vocabulary_id,
    v.jlpt_level_new,
    STRING_AGG(DISTINCT vk.text, ', ' ORDER BY vk.text) as kanji_forms,
    STRING_AGG(DISTINCT vka.text, ', ' ORDER BY vka.text) as kana_readings,
    STRING_AGG(DISTINCT k.literal, '' ORDER BY k.literal) as used_kanji,
    COUNT(DISTINCT vuk.kanji_id) as kanji_count
FROM vocabulary v
LEFT JOIN vocabulary_kanji vk ON vk.vocabulary_id = v.id
LEFT JOIN vocabulary_kana vka ON vka.vocabulary_id = v.id
LEFT JOIN vocabulary_uses_kanji vuk ON vuk.vocabulary_id = v.id
LEFT JOIN kanji k ON k.id = vuk.kanji_id
WHERE v.jlpt_level_new IS NOT NULL
GROUP BY v.id, v.jlpt_level_new;

-- Create indexes on materialized views
CREATE INDEX idx_kanji_stats_jlpt ON kanji_stats(jlpt_level_new);
CREATE INDEX idx_vocabulary_stats_jlpt ON vocabulary_stats(jlpt_level_new);
CREATE INDEX idx_radical_stats_stroke ON radical_stats(stroke_count);
CREATE INDEX idx_radical_stats_usage ON radical_stats(kanji_count DESC);
CREATE INDEX idx_common_kanji_frequency ON common_kanji(frequency ASC);
CREATE INDEX idx_vocabulary_kanji_relationships_jlpt ON vocabulary_kanji_relationships(jlpt_level_new);
CREATE INDEX idx_vocabulary_kanji_relationships_kanji_count ON vocabulary_kanji_relationships(kanji_count);

-- ============================================
-- TRIGGERS FOR UPDATED_AT COLUMNS
-- ============================================
-- Create triggers for all tables with updated_at columns
CREATE TRIGGER trigger_kanji_updated_at
    BEFORE UPDATE ON kanji
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_kanji_codepoint_updated_at
    BEFORE UPDATE ON kanji_codepoint
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_kanji_dictionary_reference_updated_at
    BEFORE UPDATE ON kanji_dictionary_reference
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_kanji_query_code_updated_at
    BEFORE UPDATE ON kanji_query_code
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_kanji_reading_updated_at
    BEFORE UPDATE ON kanji_reading
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_kanji_meaning_updated_at
    BEFORE UPDATE ON kanji_meaning
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_kanji_nanori_updated_at
    BEFORE UPDATE ON kanji_nanori
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_radical_updated_at
    BEFORE UPDATE ON radical
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_kanji_radical_updated_at
    BEFORE UPDATE ON kanji_radical
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_tag_updated_at
    BEFORE UPDATE ON tag
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_updated_at
    BEFORE UPDATE ON vocabulary
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_kanji_updated_at
    BEFORE UPDATE ON vocabulary_kanji
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_kanji_tag_updated_at
    BEFORE UPDATE ON vocabulary_kanji_tag
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_kana_updated_at
    BEFORE UPDATE ON vocabulary_kana
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_kana_tag_updated_at
    BEFORE UPDATE ON vocabulary_kana_tag
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_sense_updated_at
    BEFORE UPDATE ON vocabulary_sense
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_sense_pos_updated_at
    BEFORE UPDATE ON vocabulary_sense_pos
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_sense_field_updated_at
    BEFORE UPDATE ON vocabulary_sense_field
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_sense_dialect_updated_at
    BEFORE UPDATE ON vocabulary_sense_dialect
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_sense_misc_updated_at
    BEFORE UPDATE ON vocabulary_sense_misc
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_sense_relation_updated_at
    BEFORE UPDATE ON vocabulary_sense_relation
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_sense_language_source_updated_at
    BEFORE UPDATE ON vocabulary_sense_language_source
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_sense_gloss_updated_at
    BEFORE UPDATE ON vocabulary_sense_gloss
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_sense_example_updated_at
    BEFORE UPDATE ON vocabulary_sense_example
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_sense_example_sentence_updated_at
    BEFORE UPDATE ON vocabulary_sense_example_sentence
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_proper_noun_updated_at
    BEFORE UPDATE ON proper_noun
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_proper_noun_kanji_updated_at
    BEFORE UPDATE ON proper_noun_kanji
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_proper_noun_kanji_tag_updated_at
    BEFORE UPDATE ON proper_noun_kanji_tag
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_proper_noun_kana_updated_at
    BEFORE UPDATE ON proper_noun_kana
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_proper_noun_kana_tag_updated_at
    BEFORE UPDATE ON proper_noun_kana_tag
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_proper_noun_translation_updated_at
    BEFORE UPDATE ON proper_noun_translation
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_proper_noun_translation_type_updated_at
    BEFORE UPDATE ON proper_noun_translation_type
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_proper_noun_translation_related_updated_at
    BEFORE UPDATE ON proper_noun_translation_related
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_proper_noun_translation_text_updated_at
    BEFORE UPDATE ON proper_noun_translation_text
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_vocabulary_uses_kanji_updated_at
    BEFORE UPDATE ON vocabulary_uses_kanji
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_proper_noun_uses_kanji_updated_at
    BEFORE UPDATE ON proper_noun_uses_kanji
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- SEARCH FUNCTIONS FOR DICTIONARY API
-- ============================================

-- Function to search kanji by JLPT level with pagination
CREATE OR REPLACE FUNCTION search_kanji_by_jlpt(
    p_jlpt_level INTEGER,
    p_limit INTEGER DEFAULT 50,
    p_offset INTEGER DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    literal CHAR(1),
    jlpt_level_new INTEGER,
    stroke_count INTEGER,
    frequency INTEGER,
    on_readings TEXT,
    kun_readings TEXT,
    meanings_en TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        k.id,
        k.literal,
        k.jlpt_level_new,
        k.stroke_count,
        k.frequency,
        STRING_AGG(DISTINCT kr.value, ', ' ORDER BY kr.value) FILTER (WHERE kr.type = 'ja_on') as on_readings,
        STRING_AGG(DISTINCT kr.value, ', ' ORDER BY kr.value) FILTER (WHERE kr.type = 'ja_kun') as kun_readings,
        STRING_AGG(DISTINCT km.value, '; ' ORDER BY km.value) FILTER (WHERE km.lang = 'en') as meanings_en
    FROM kanji k
    LEFT JOIN kanji_reading kr ON kr.kanji_id = k.id
    LEFT JOIN kanji_meaning km ON km.kanji_id = k.id
    WHERE k.jlpt_level_new = p_jlpt_level
    GROUP BY k.id, k.literal, k.jlpt_level_new, k.stroke_count, k.frequency
    ORDER BY k.frequency ASC NULLS LAST, k.stroke_count ASC
    LIMIT p_limit OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;

-- Function to search vocabulary by JLPT level with pagination
CREATE OR REPLACE FUNCTION search_vocabulary_by_jlpt(
    p_jlpt_level INTEGER,
    p_limit INTEGER DEFAULT 50,
    p_offset INTEGER DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    jlpt_level_new INTEGER,
    kanji_forms TEXT,
    kana_readings TEXT,
    part_of_speech_tags TEXT,
    field_tags TEXT,
    dialect_tags TEXT,
    misc_tags TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        v.id,
        v.jlpt_level_new,
        STRING_AGG(DISTINCT vk.text, ', ' ORDER BY vk.text) as kanji_forms,
        STRING_AGG(DISTINCT vka.text, ', ' ORDER BY vka.text) as kana_readings,
        STRING_AGG(DISTINCT t_pos.code, ', ') as part_of_speech_tags,
        STRING_AGG(DISTINCT t_field.code, ', ') as field_tags,
        STRING_AGG(DISTINCT t_dialect.code, ', ') as dialect_tags,
        STRING_AGG(DISTINCT t_misc.code, ', ') as misc_tags
    FROM vocabulary v
    LEFT JOIN vocabulary_kanji vk ON vk.vocabulary_id = v.id
    LEFT JOIN vocabulary_kana vka ON vka.vocabulary_id = v.id
    LEFT JOIN vocabulary_sense vs ON vs.vocabulary_id = v.id
    LEFT JOIN vocabulary_sense_pos vsp ON vsp.sense_id = vs.id
    LEFT JOIN tag t_pos ON t_pos.code = vsp.tag_code
    LEFT JOIN vocabulary_sense_field vsf ON vsf.sense_id = vs.id
    LEFT JOIN tag t_field ON t_field.code = vsf.tag_code
    LEFT JOIN vocabulary_sense_dialect vsd ON vsd.sense_id = vs.id
    LEFT JOIN tag t_dialect ON t_dialect.code = vsd.tag_code
    LEFT JOIN vocabulary_sense_misc vsm ON vsm.sense_id = vs.id
    LEFT JOIN tag t_misc ON t_misc.code = vsm.tag_code
    WHERE v.jlpt_level_new = p_jlpt_level
    GROUP BY v.id, v.jlpt_level_new
    ORDER BY v.id
    LIMIT p_limit OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;

-- Function to search kanji by text (readings or meanings)
CREATE OR REPLACE FUNCTION search_kanji_by_text(
    p_search_text TEXT,
    p_limit INTEGER DEFAULT 50,
    p_offset INTEGER DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    literal CHAR(1),
    jlpt_level_new INTEGER,
    stroke_count INTEGER,
    frequency INTEGER,
    on_readings TEXT,
    kun_readings TEXT,
    meanings_en TEXT,
    relevance_score REAL
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        k.id,
        k.literal,
        k.jlpt_level_new,
        k.stroke_count,
        k.frequency,
        STRING_AGG(DISTINCT kr.value, ', ' ORDER BY kr.value) FILTER (WHERE kr.type = 'ja_on') as on_readings,
        STRING_AGG(DISTINCT kr.value, ', ' ORDER BY kr.value) FILTER (WHERE kr.type = 'ja_kun') as kun_readings,
        STRING_AGG(DISTINCT km.value, '; ' ORDER BY km.value) FILTER (WHERE km.lang = 'en') as meanings_en,
        ts_rank(
            to_tsvector('english', STRING_AGG(DISTINCT km.value, ' ' ORDER BY km.value) FILTER (WHERE km.lang = 'en')),
            plainto_tsquery('english', p_search_text)
        ) as relevance_score
    FROM kanji k
    LEFT JOIN kanji_reading kr ON kr.kanji_id = k.id
    LEFT JOIN kanji_meaning km ON km.kanji_id = k.id
    WHERE 
        k.literal = p_search_text OR
        kr.value ILIKE '%' || p_search_text || '%' OR
        to_tsvector('english', km.value) @@ plainto_tsquery('english', p_search_text)
    GROUP BY k.id, k.literal, k.jlpt_level_new, k.stroke_count, k.frequency
    ORDER BY relevance_score DESC NULLS LAST, k.frequency ASC NULLS LAST
    LIMIT p_limit OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;

-- Function to search vocabulary by text
CREATE OR REPLACE FUNCTION search_vocabulary_by_text(
    p_search_text TEXT,
    p_limit INTEGER DEFAULT 50,
    p_offset INTEGER DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    jlpt_level_new INTEGER,
    kanji_forms TEXT,
    kana_readings TEXT,
    part_of_speech_tags TEXT,
    relevance_score REAL
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        v.id,
        v.jlpt_level_new,
        STRING_AGG(DISTINCT vk.text, ', ' ORDER BY vk.text) as kanji_forms,
        STRING_AGG(DISTINCT vka.text, ', ' ORDER BY vka.text) as kana_readings,
        STRING_AGG(DISTINCT t_pos.code, ', ') as part_of_speech_tags,
        ts_rank(
            to_tsvector('japanese', STRING_AGG(DISTINCT vk.text, ' ' ORDER BY vk.text)),
            plainto_tsquery('japanese', p_search_text)
        ) as relevance_score
    FROM vocabulary v
    LEFT JOIN vocabulary_kanji vk ON vk.vocabulary_id = v.id
    LEFT JOIN vocabulary_kana vka ON vka.vocabulary_id = v.id
    LEFT JOIN vocabulary_sense vs ON vs.vocabulary_id = v.id
    LEFT JOIN vocabulary_sense_pos vsp ON vsp.sense_id = vs.id
    LEFT JOIN tag t_pos ON t_pos.code = vsp.tag_code
    WHERE 
        vk.text ILIKE '%' || p_search_text || '%' OR
        vka.text ILIKE '%' || p_search_text || '%' OR
        to_tsvector('japanese', vk.text) @@ plainto_tsquery('japanese', p_search_text) OR
        to_tsvector('japanese', vka.text) @@ plainto_tsquery('japanese', p_search_text)
    GROUP BY v.id, v.jlpt_level_new
    ORDER BY relevance_score DESC NULLS LAST, v.id
    LIMIT p_limit OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;

-- Function to get kanji used in vocabulary
CREATE OR REPLACE FUNCTION get_kanji_in_vocabulary(
    p_vocabulary_id UUID
)
RETURNS TABLE (
    kanji_id UUID,
    kanji_literal CHAR(1),
    jlpt_level_new INTEGER,
    stroke_count INTEGER,
    frequency INTEGER
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        k.id as kanji_id,
        k.literal as kanji_literal,
        k.jlpt_level_new,
        k.stroke_count,
        k.frequency
    FROM vocabulary_uses_kanji vuk
    JOIN kanji k ON k.id = vuk.kanji_id
    WHERE vuk.vocabulary_id = p_vocabulary_id
    ORDER BY k.frequency ASC NULLS LAST, k.stroke_count ASC;
END;
$$ LANGUAGE plpgsql;

-- Function to get vocabulary using kanji
CREATE OR REPLACE FUNCTION get_vocabulary_using_kanji(
    p_kanji_id UUID,
    p_limit INTEGER DEFAULT 50,
    p_offset INTEGER DEFAULT 0
)
RETURNS TABLE (
    vocabulary_id UUID,
    jlpt_level_new INTEGER,
    kanji_forms TEXT,
    kana_readings TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        v.id as vocabulary_id,
        v.jlpt_level_new,
        STRING_AGG(DISTINCT vk.text, ', ' ORDER BY vk.text) as kanji_forms,
        STRING_AGG(DISTINCT vka.text, ', ' ORDER BY vka.text) as kana_readings
    FROM vocabulary_uses_kanji vuk
    JOIN vocabulary v ON v.id = vuk.vocabulary_id
    LEFT JOIN vocabulary_kanji vk ON vk.vocabulary_id = v.id
    LEFT JOIN vocabulary_kana vka ON vka.vocabulary_id = v.id
    WHERE vuk.kanji_id = p_kanji_id
    GROUP BY v.id, v.jlpt_level_new
    ORDER BY v.jlpt_level_new ASC, v.id
    LIMIT p_limit OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;

-- Function to refresh materialized views
CREATE OR REPLACE FUNCTION refresh_dictionary_views()
RETURNS VOID AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY kanji_stats;
    REFRESH MATERIALIZED VIEW CONCURRENTLY vocabulary_stats;
    REFRESH MATERIALIZED VIEW CONCURRENTLY radical_stats;
    REFRESH MATERIALIZED VIEW CONCURRENTLY common_kanji;
    REFRESH MATERIALIZED VIEW CONCURRENTLY vocabulary_kanji_relationships;
END;
$$ LANGUAGE plpgsql;