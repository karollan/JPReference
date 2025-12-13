-- JLPT Reference Database Initialization Script
-- This script runs when the PostgreSQL container is first created

-- Create the main database if it doesn't exist
-- (The database is already created by POSTGRES_DB environment variable)

-- Create extensions that might be useful for JLPT reference data
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
--CREATE EXTENSION IF NOT EXISTS "textsearch_ja";

SET pg_trgm.similarity_threshold = 0.4; -- stricter for dictionary usage

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
CREATE TABLE IF NOT EXISTS radical (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    literal VARCHAR(1) UNIQUE NOT NULL,
    stroke_count INTEGER,
    code VARCHAR(20),
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
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Vocabulary
CREATE TABLE IF NOT EXISTS vocabulary (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    jmdict_id VARCHAR(20) UNIQUE NOT NULL,
    jlpt_level_new INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Kanji forms of vocabulary
CREATE TABLE IF NOT EXISTS vocabulary_kanji (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    vocabulary_id UUID NOT NULL REFERENCES vocabulary(id) ON DELETE CASCADE,
    text TEXT NOT NULL,
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
CREATE INDEX IF NOT EXISTS idx_vocabulary_kanji_common ON vocabulary_kanji(vocabulary_id, is_common) WHERE is_common = true;
CREATE INDEX IF NOT EXISTS idx_vocabulary_kana_common ON vocabulary_kana(vocabulary_id, is_common) WHERE is_common = true;
-- Vocabulary sense glosses for English search
CREATE INDEX IF NOT EXISTS idx_vocabulary_gloss_text_trgm ON vocabulary_sense_gloss USING gist (text gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_vocabulary_gloss_text_trgm_gin ON vocabulary_sense_gloss USING gin (text gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_vocabulary_gloss_lang ON vocabulary_sense_gloss(lang, sense_id);
-- Sense tag filtering
CREATE INDEX IF NOT EXISTS idx_vocabulary_sense_tag_lookup ON vocabulary_sense_tag(sense_id, tag_type, tag_code);
-- Vocabulary relationships
CREATE INDEX IF NOT EXISTS idx_vocabulary_uses_kanji_lookup ON vocabulary_uses_kanji(vocabulary_id, kanji_id);
CREATE INDEX IF NOT EXISTS idx_vocabulary_uses_kanji_reverse ON vocabulary_uses_kanji(kanji_id, vocabulary_id);

-- ============================================
-- PROPER NOUN INDEXES
-- ============================================
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_noun ON proper_noun_kanji(proper_noun_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_text ON proper_noun_kanji(text);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_tag_noun_kanji ON proper_noun_kanji_tag(proper_noun_kanji_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_tag_code ON proper_noun_kanji_tag(tag_code);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_noun ON proper_noun_kana(proper_noun_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_tag_noun_kana ON proper_noun_kana_tag(proper_noun_kana_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_tag_code ON proper_noun_kana_tag(tag_code);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_type_trans ON proper_noun_translation_type(translation_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_type_tag ON proper_noun_translation_type(tag_code);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_related_trans ON proper_noun_translation_related(translation_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_related_ref_trans ON proper_noun_translation_related(reference_proper_noun_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_trans_text_trans ON proper_noun_translation_text(translation_id);
-- Proper noun text search
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_text_trgm ON proper_noun_kanji USING gist (text gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_text_trgm_gin ON proper_noun_kanji USING gin (text gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_text_trgm ON proper_noun_kana USING gist (text gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_text_trgm_gin ON proper_noun_kana USING gin (text gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_translation_text_trgm ON proper_noun_translation_text USING gist (text gist_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_proper_noun_translation_text_trgm_gin ON proper_noun_translation_text USING gin (text gin_trgm_ops);
-- Prefix indexes
-- CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_prefix ON jlpt.proper_noun_kanji (LEFT(text, 10));
-- CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_prefix ON jlpt.proper_noun_kana (LEFT(text, 10));
-- CREATE INDEX IF NOT EXISTS idx_proper_noun_translation_prefix ON jlpt.proper_noun_translation_text (LEFT(text, 20));
-- -- Suffix indexes
-- CREATE INDEX IF NOT EXISTS idx_proper_noun_kanji_suffix ON jlpt.proper_noun_kanji (RIGHT(text, 10));
-- CREATE INDEX IF NOT EXISTS idx_proper_noun_kana_suffix ON jlpt.proper_noun_kana (RIGHT(text, 10));
-- CREATE INDEX IF NOT EXISTS idx_proper_noun_translation_suffix ON jlpt.proper_noun_translation_text (RIGHT(text, 20));
-- FK indexes (for JOINs)
CREATE INDEX IF NOT EXISTS idx_proper_noun_translation_pn_id ON jlpt.proper_noun_translation (proper_noun_id);
-- Proper noun relationships
CREATE INDEX IF NOT EXISTS idx_proper_noun_uses_kanji_lookup ON proper_noun_uses_kanji(proper_noun_id, kanji_id);
CREATE INDEX IF NOT EXISTS idx_proper_noun_uses_kanji_reverse ON proper_noun_uses_kanji(kanji_id, proper_noun_id);

-- ============================================
-- RADICAL INDEXES
-- ============================================
CREATE INDEX IF NOT EXISTS idx_radical_literal ON radical(literal);
CREATE INDEX IF NOT EXISTS idx_kanji_radical_kanji ON kanji_radical(kanji_id);
CREATE INDEX IF NOT EXISTS idx_kanji_radical_radical ON kanji_radical(radical_id);
-- Radical lookup by stroke count
CREATE INDEX IF NOT EXISTS idx_radical_stroke_count ON radical(stroke_count, literal);

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
-- COMPOSITE INDEXES FOR COMPLEX QUERIES
-- ============================================
-- Kanji with readings and meanings (for detailed views)
CREATE INDEX IF NOT EXISTS idx_kanji_detailed ON kanji(id, jlpt_level_new, stroke_count, frequency);
-- Vocabulary with senses (for detailed views)
CREATE INDEX IF NOT EXISTS idx_vocabulary_detailed ON vocabulary(id, jlpt_level_new);


-- ============================================
-- UTILITY VIEWS
-- ============================================
-- View for kanji with all basic info
CREATE OR REPLACE VIEW kanji_summary AS
SELECT 
    k.id,
    k.literal,
    k.grade,
    k.stroke_count,
    k.frequency,
    k.jlpt_level_old,
    k.jlpt_level_new,
    STRING_AGG(DISTINCT kr.value, ', ' ORDER BY kr.value) FILTER (WHERE kr.type = 'ja_on') as on_readings,
    STRING_AGG(DISTINCT kr.value, ', ' ORDER BY kr.value) FILTER (WHERE kr.type = 'ja_kun') as kun_readings,
    STRING_AGG(DISTINCT km.value, '; ' ORDER BY km.value) FILTER (WHERE km.lang = 'en') as meanings_en
FROM kanji k
LEFT JOIN kanji_reading kr ON kr.kanji_id = k.id
LEFT JOIN kanji_meaning km ON km.kanji_id = k.id
GROUP BY k.id, k.literal, k.grade, k.stroke_count, k.frequency, k.jlpt_level_old, k.jlpt_level_new;

-- View for vocabulary with basic info and tags
CREATE OR REPLACE VIEW vocabulary_summary AS
SELECT 
    v.id,
    STRING_AGG(DISTINCT vk.text, ', ' ORDER BY vk.text) as kanji_forms,
    STRING_AGG(DISTINCT vka.text, ', ' ORDER BY vka.text) as kana_readings,
    v.jlpt_level_new,
    STRING_AGG(DISTINCT t.code, ', ') FILTER (WHERE vst.tag_type = 'pos') as part_of_speech_tags,
    STRING_AGG(DISTINCT t.code, ', ') FILTER (WHERE vst.tag_type = 'field') as field_tags,
    STRING_AGG(DISTINCT t.code, ', ') FILTER (WHERE vst.tag_type = 'dialect') as dialect_tags,
    STRING_AGG(DISTINCT t.code, ', ') FILTER (WHERE vst.tag_type = 'misc') as misc_tags
FROM vocabulary v
LEFT JOIN vocabulary_kanji vk ON vk.vocabulary_id = v.id
LEFT JOIN vocabulary_kana vka ON vka.vocabulary_id = v.id
LEFT JOIN vocabulary_sense vs ON vs.vocabulary_id = v.id
LEFT JOIN vocabulary_sense_tag vst ON vst.sense_id = vs.id
LEFT JOIN tag t ON t.code = vst.tag_code
GROUP BY v.id, v.jlpt_level_new;

-- ============================================
-- MATERIALIZED VIEWS FOR PERFORMANCE
-- ============================================

-- Kanji statistics by JLPT level
CREATE MATERIALIZED VIEW IF NOT EXISTS kanji_stats AS
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
CREATE INDEX IF NOT EXISTS idx_kanji_stats_jlpt ON kanji_stats(jlpt_level_new);

-- Vocabulary statistics by JLPT level
CREATE MATERIALIZED VIEW IF NOT EXISTS vocabulary_stats AS
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
CREATE INDEX IF NOT EXISTS idx_vocabulary_stats_jlpt ON vocabulary_stats(jlpt_level_new);

-- Radical usage statistics
CREATE MATERIALIZED VIEW IF NOT EXISTS radical_stats AS
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
CREATE INDEX IF NOT EXISTS idx_radical_stats_stroke ON radical_stats(stroke_count);
CREATE INDEX IF NOT EXISTS idx_radical_stats_usage ON radical_stats(kanji_count DESC);

-- Proper noun search materialized view

-- Vocabulary search materialized view

-- Kanji search materialized view

-- Global search materialized view

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

CREATE TRIGGER trigger_vocabulary_sense_tag_updated_at
    BEFORE UPDATE ON vocabulary_sense_tag
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

-- Function to search global by text with pagination
CREATE OR REPLACE FUNCTION search_global_by_text(
    queries TEXT[],
    relevanceThreshold FLOAT DEFAULT 0.4,
    pageSize INT DEFAULT 50,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    entry_type TEXT,
    dict_id VARCHAR(20),
    jlpt_level INT,
    relevance_score FLOAT,
    primary_kanji JSON,
    other_kanji_forms JSON,
    primary_kana JSON,
    other_kana_forms JSON,
    senses JSON,
    is_common BOOLEAN,
    translations JSON,
    literal TEXT,
    grade INT,
    stroke_count INT,
    frequency INT,
    kunyomi JSON,
    onyomi JSON,
    meanings_kanji JSON,
    radicals JSON,
    total_count_vocab BIGINT,
    total_count_proper_noun BIGINT,
    total_count_kanji BIGINT
)
AS $$
BEGIN
    RETURN QUERY
        WITH kanji_search_matches AS (
            -- Search by literal (exact match has highest priority)
            SELECT DISTINCT
                k.id as kanji_id,
                k.literal as match_text,
                'literal' as match_type,
                CASE
				    WHEN k.literal = q THEN 1.0
				    WHEN k.literal LIKE q || '%' THEN 0.95
				    WHEN k.literal LIKE '%' || q THEN 0.90
				    ELSE similarity(k.literal, q)
				END as relevance
            FROM jlpt.kanji k
			CROSS JOIN LATERAL unnest(queries) AS q
            WHERE 
			    (k.literal % q AND similarity(k.literal, q) >= relevanceThreshold)
			    OR k.literal LIKE q || '%'
			    OR k.literal LIKE '%' || q
            
            UNION ALL
            
            -- Search in readings (kunyomi and onyomi)
            SELECT DISTINCT
                k.id as kanji_id,
                kr.value as match_text,
                'reading' as match_type,
                CASE
				    WHEN kr.value = q THEN 1.0
				    WHEN kr.value LIKE q || '%' THEN 0.95
				    WHEN kr.value LIKE '%' || q THEN 0.90
				    ELSE similarity(kr.value, q)
				END as relevance
	        FROM jlpt.kanji k
	        JOIN jlpt.kanji_reading kr ON k.id = kr.kanji_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE 
			    (kr.value % q AND similarity(kr.value, q) >= relevanceThreshold)
			    OR kr.value LIKE q || '%'
			    OR kr.value LIKE '%' || q
            
            UNION ALL
            
            -- Search in meanings
            SELECT DISTINCT
                k.id as kanji_id,
                km.value as match_text,
                'meaning' as match_type,
                CASE
				    WHEN km.value = q THEN 1.0
				    WHEN km.value LIKE q || '%' THEN 0.95
				    WHEN km.value LIKE '%' || q THEN 0.90
				    ELSE similarity(km.value, q)
				END as relevance
	        FROM jlpt.kanji k
	        JOIN jlpt.kanji_meaning km ON k.id = km.kanji_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE 
			    (km.value % q AND similarity(km.value, q) >= relevanceThreshold)
			    OR km.value LIKE q || '%'
			    OR km.value LIKE '%' || q
        ),
        kanji_ranked_matches AS (
            SELECT 
                kanji_id,
                MAX(relevance)::double precision as max_relevance
            FROM kanji_search_matches
            GROUP BY kanji_id
        ),
        kanji_data AS (
            SELECT
                k.id,
                'kanji' as entry_type,
                NULL::text as dict_id,
                k.jlpt_level_new as jlpt_level,
                rm.max_relevance::double precision as relevance_score,
                NULL::json as primary_kanji,
                NULL::json as other_kanji_forms,
                NULL::json as primary_kana,
                NULL::json as other_kana_forms,
                NULL::json as senses,
                false as is_common,
                NULL::json as translations,
                k.literal,
                k.grade,
                k.stroke_count,
                k.frequency,
                
                -- Get kunyomi readings (ja_kun)
                (SELECT json_agg(json_build_object(
                    'type', kr.type,
                    'value', kr.value,
                    'status', kr.status
                ) ORDER BY kr.created_at)
                FROM jlpt.kanji_reading kr
                WHERE kr.kanji_id = k.id 
                AND kr.type = 'ja_kun'
                ) as kunyomi,
                
                -- Get onyomi readings (ja_on)
                (SELECT json_agg(json_build_object(
                    'type', kr.type,
                    'value', kr.value,
                    'status', kr.status,
                    'on_type', kr.on_type
                ) ORDER BY kr.created_at)
                FROM jlpt.kanji_reading kr
                WHERE kr.kanji_id = k.id 
                AND kr.type = 'ja_on'
                ) as onyomi,
                
                -- Get meanings
                (SELECT json_agg(json_build_object(
                    'language', km.lang,
                    'meaning', km.value
                ) ORDER BY km.created_at)
                FROM jlpt.kanji_meaning km
                WHERE km.kanji_id = k.id
                ) as meanings_kanji,
                
                -- Get radicals
                (SELECT json_agg(json_build_object(
                    'id', r.id::text,
                    'literal', r.literal
                ))
                FROM jlpt.kanji_radical kr
                JOIN jlpt.radical r ON kr.radical_id = r.id
                WHERE kr.kanji_id = k.id
                ) as radicals
                
            FROM jlpt.kanji k
            JOIN kanji_ranked_matches rm ON k.id = rm.kanji_id
            WHERE rm.max_relevance::double precision >= relevanceThreshold
        ),
        proper_noun_search_matches AS (
            -- Search in kanji
            SELECT DISTINCT
                pn.id as proper_noun_id,
                pnk.text as match_text,
                'kanji' as match_type,
                CASE
				    WHEN pnk.text = q THEN 1.0
				    WHEN pnk.text LIKE q || '%' THEN 0.95
				    WHEN pnk.text LIKE '%' || q THEN 0.90
				    ELSE similarity(pnk.text, q)
				END as relevance
	        FROM jlpt.proper_noun pn
	        JOIN jlpt.proper_noun_kanji pnk ON pn.id = pnk.proper_noun_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE (pnk.text % q AND similarity(pnk.text, q) >= relevanceThreshold)
			    OR pnk.text LIKE q || '%'
			    OR pnk.text LIKE '%' || q
            
            UNION ALL
            
            -- Search in kana
            SELECT DISTINCT
                pn.id as proper_noun_id,
                pnkn.text as match_text,
                'kana' as match_type,
                CASE
				    WHEN pnkn.text = q THEN 1.0
				    WHEN pnkn.text LIKE q || '%' THEN 0.95
				    WHEN pnkn.text LIKE '%' || q THEN 0.90
				    ELSE similarity(pnkn.text, q)
				END as relevance
	        FROM jlpt.proper_noun pn
	        JOIN jlpt.proper_noun_kana pnkn ON pn.id = pnkn.proper_noun_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE (pnkn.text % q AND similarity(pnkn.text, q) >= relevanceThreshold)
			    OR pnkn.text LIKE q || '%'
			    OR pnkn.text LIKE '%' || q
            
            UNION ALL
            
            -- Search in translations
            SELECT DISTINCT
                pn.id as proper_noun_id,
                pntt.text as match_text,
                'translation' as match_type,
                CASE
				    WHEN pntt.text = q THEN 1.0
				    WHEN pntt.text LIKE q || '%' THEN 0.95
				    WHEN pntt.text LIKE '%' || q THEN 0.90
				    ELSE similarity(pntt.text, q)
				END as relevance
	        FROM jlpt.proper_noun pn
	        JOIN jlpt.proper_noun_translation pnt ON pn.id = pnt.proper_noun_id
	        JOIN jlpt.proper_noun_translation_text pntt ON pnt.id = pntt.translation_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE (pntt.text % q AND similarity(pntt.text, q) >= relevanceThreshold)
			    OR pntt.text LIKE q || '%'
			    OR pntt.text LIKE '%' || q
        ),
        proper_noun_ranked_matches AS (
            SELECT 
                proper_noun_id,
                MAX(relevance)::double precision as max_relevance
            FROM proper_noun_search_matches
            GROUP BY proper_noun_id
        ),
        proper_noun_data AS (
            SELECT
                pn.id,
                'proper_noun' as entry_type,
                pn.jmnedict_id as dict_id,
                NULL::integer as jlpt_level,
                rm.max_relevance::double precision as relevance_score,
                
                -- Get primary kanji (first one)
                (SELECT json_build_object(
                    'text', pnk.text,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.proper_noun_kanji_tag pnkt
                        JOIN jlpt.tag t ON pnkt.tag_code = t.code
                        WHERE pnkt.proper_noun_kanji_id = pnk.id),
                        '[]'::json
                    )
                )
                FROM jlpt.proper_noun_kanji pnk
                WHERE pnk.proper_noun_id = pn.id
                ORDER BY pnk.created_at ASC
                LIMIT 1
                ) as primary_kanji,
                
                -- Get all other kanji forms
                (SELECT json_agg(json_build_object(
                    'text', pnk.text,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.proper_noun_kanji_tag pnkt
                        JOIN jlpt.tag t ON pnkt.tag_code = t.code
                        WHERE pnkt.proper_noun_kanji_id = pnk.id),
                        '[]'::json
                    )
                ) ORDER BY pnk.created_at ASC)
                FROM jlpt.proper_noun_kanji pnk
                WHERE pnk.proper_noun_id = pn.id
                OFFSET 1
                ) as other_kanji_forms,
                
                -- Get primary kana (respecting applies_to_kanji)
                (SELECT json_build_object(
                    'text', pnkn.text,
                    'applies_to_kanji', pnkn.applies_to_kanji,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.proper_noun_kana_tag pnknt
                        JOIN jlpt.tag t ON pnknt.tag_code = t.code
                        WHERE pnknt.proper_noun_kana_id = pnkn.id),
                        '[]'::json
                    )
                )
                FROM jlpt.proper_noun_kana pnkn
                WHERE pnkn.proper_noun_id = pn.id
                ORDER BY pnkn.created_at ASC
                LIMIT 1
                ) as primary_kana,
                
                -- Get all other kana forms
                (SELECT json_agg(json_build_object(
                    'text', pnkn.text,
                    'applies_to_kanji', pnkn.applies_to_kanji,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.proper_noun_kana_tag pnknt
                        JOIN jlpt.tag t ON pnknt.tag_code = t.code
                        WHERE pnknt.proper_noun_kana_id = pnkn.id),
                        '[]'::json
                    )
                ) ORDER BY pnkn.created_at ASC)
                FROM jlpt.proper_noun_kana pnkn
                WHERE pnkn.proper_noun_id = pn.id
                OFFSET 1
                ) as other_kana_forms,
                NULL::json as senses,
                false as is_common,
                -- Get all translations
                (SELECT json_agg(translation_data ORDER BY translation_order)
                FROM (
                    SELECT 
                        pnt.id,
                        ROW_NUMBER() OVER (ORDER BY pnt.created_at) as translation_order,
                        json_build_object(
                            'types', (
                                SELECT json_agg(json_build_object(
                                    'code', t.code,
                                    'description', t.description,
                                    'category', t.category
                                ))
                                FROM jlpt.proper_noun_translation_type pntt
                                JOIN jlpt.tag t ON pntt.tag_code = t.code
                                WHERE pntt.translation_id = pnt.id
                            ),
                            'translations', (
                                SELECT json_agg(json_build_object(
                                    'language', pnttxt.lang,
                                    'text', pnttxt.text
                                ))
                                FROM jlpt.proper_noun_translation_text pnttxt
                                WHERE pnttxt.translation_id = pnt.id
                            )
                        ) as translation_data
                    FROM jlpt.proper_noun_translation pnt
                    WHERE pnt.proper_noun_id = pn.id
                    ORDER BY pnt.created_at
                ) translations
                ) as translations,
                NULL::text as literal,
                NULL::integer as grade,
                NULL::integer as stroke_count,
                NULL::integer as frequency,
                NULL::json as kunyomi,
                NULL::json as onyomi,
                NULL::json as meanings_kanji,
                NULL::json as radicals
                
            FROM jlpt.proper_noun pn
            JOIN proper_noun_ranked_matches rm ON pn.id = rm.proper_noun_id
            WHERE rm.max_relevance::double precision >= relevanceThreshold
        ),
        vocabulary_search_matches AS (
            -- Search in kanji
            SELECT DISTINCT
                v.id as vocabulary_id,
                vk.text as match_text,
                'kanji' as match_type,
                CASE
				    WHEN vk.text = q THEN 1.0
				    WHEN vk.text LIKE q || '%' THEN 0.95
				    WHEN vk.text LIKE '%' || q THEN 0.90
				    ELSE similarity(vk.text, q)
				END as relevance
	        FROM jlpt.vocabulary v
	        JOIN jlpt.vocabulary_kanji vk ON v.id = vk.vocabulary_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE (vk.text % q AND similarity(vk.text, q) >= relevanceThreshold)
			    OR vk.text LIKE q || '%'
			    OR vk.text LIKE '%' || q
            
            UNION ALL
            
            -- Search in kana
            SELECT DISTINCT
                v.id as vocabulary_id,
                vkn.text as match_text,
                'kana' as match_type,
                CASE
				    WHEN vkn.text = q THEN 1.0
				    WHEN vkn.text LIKE q || '%' THEN 0.95
				    WHEN vkn.text LIKE '%' || q THEN 0.90
			    	ELSE similarity(vkn.text, q)
				END as relevance
	        FROM jlpt.vocabulary v
	        JOIN jlpt.vocabulary_kana vkn ON v.id = vkn.vocabulary_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE (vkn.text % q AND similarity(vkn.text, q) >= relevanceThreshold)
			    OR vkn.text LIKE q || '%'
			    OR vkn.text LIKE '%' || q
            
            UNION ALL
            
            -- Search in glosses
            SELECT DISTINCT
                v.id as vocabulary_id,
                vsg.text as match_text,
                'gloss' as match_type,
                CASE
				    WHEN vsg.text = q THEN 1.0
				    WHEN vsg.text LIKE q || '%' THEN 0.95
				    WHEN vsg.text LIKE '%' || q THEN 0.90
			    	ELSE similarity(vsg.text, q)
				END as relevance
	        FROM jlpt.vocabulary v
	        JOIN jlpt.vocabulary_sense vs ON v.id = vs.vocabulary_id
	        JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE (vsg.text % q AND similarity(vsg.text, q) >= relevanceThreshold)
			    OR vsg.text LIKE q || '%'
			    OR vsg.text LIKE '%' || q
        ),
        vocabulary_ranked_matches AS (
            SELECT 
                vocabulary_id,
                MAX(relevance)::double precision as max_relevance
            FROM vocabulary_search_matches
            GROUP BY vocabulary_id
        ),
        vocabulary_data AS (
            SELECT
                v.id,
                'vocabulary' as entry_type,
                v.jmdict_id as dict_id,
                v.jlpt_level_new as jlpt_level,
                rm.max_relevance::double precision as relevance_score,
                
                -- Get primary kanji (first common one, or first one)
                (SELECT json_build_object(
                    'text', vk.text,
                    'is_common', vk.is_common,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.vocabulary_kanji_tag vkt
                        JOIN jlpt.tag t ON vkt.tag_code = t.code
                        WHERE vkt.vocabulary_kanji_id = vk.id),
                        '[]'::json
                    )
                )
                FROM jlpt.vocabulary_kanji vk
                WHERE vk.vocabulary_id = v.id
                ORDER BY vk.is_common DESC, vk.created_at ASC
                LIMIT 1
                ) as primary_kanji,
                
                -- Get all other kanji forms
                (SELECT json_agg(json_build_object(
                    'text', vk.text,
                    'is_common', vk.is_common,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.vocabulary_kanji_tag vkt
                        JOIN jlpt.tag t ON vkt.tag_code = t.code
                        WHERE vkt.vocabulary_kanji_id = vk.id),
                        '[]'::json
                    )
                ) ORDER BY vk.is_common DESC, vk.created_at ASC)
                FROM jlpt.vocabulary_kanji vk
                WHERE vk.vocabulary_id = v.id
                OFFSET 1
                ) as other_kanji_forms,
                
                -- Get primary kana (respecting applies_to_kanji)
                (SELECT json_build_object(
                    'text', vkn.text,
                    'is_common', vkn.is_common,
                    'applies_to_kanji', vkn.applies_to_kanji,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.vocabulary_kana_tag vknt
                        JOIN jlpt.tag t ON vknt.tag_code = t.code
                        WHERE vknt.vocabulary_kana_id = vkn.id),
                        '[]'::json
                    )
                )
                FROM jlpt.vocabulary_kana vkn
                WHERE vkn.vocabulary_id = v.id
                ORDER BY vkn.is_common DESC, vkn.created_at ASC
                LIMIT 1
                ) as primary_kana,
                
                -- Get all other kana forms
                (SELECT json_agg(json_build_object(
                    'text', vkn.text,
                    'is_common', vkn.is_common,
                    'applies_to_kanji', vkn.applies_to_kanji,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.vocabulary_kana_tag vknt
                        JOIN jlpt.tag t ON vknt.tag_code = t.code
                        WHERE vknt.vocabulary_kana_id = vkn.id),
                        '[]'::json
                    )
                ) ORDER BY vkn.is_common DESC, vkn.created_at ASC)
                FROM jlpt.vocabulary_kana vkn
                WHERE vkn.vocabulary_id = v.id
                OFFSET 1
                ) as other_kana_forms,
                
                -- Get first 3 senses with their data
                (SELECT json_agg(sense_data ORDER BY sense_order)
                FROM (
                    SELECT 
                        vs.id,
                        ROW_NUMBER() OVER (ORDER BY vs.created_at) as sense_order,
                        json_build_object(
                            'applies_to_kanji', vs.applies_to_kanji,
                            'applies_to_kana', vs.applies_to_kana,
                            'info', vs.info,
                            'glosses', (
                                SELECT json_agg(json_build_object(
                                    'language', vsg.lang,
                                    'text', vsg.text,
                                    'gender', vsg.gender,
                                    'type', vsg.type
                                ))
                                FROM jlpt.vocabulary_sense_gloss vsg
                                WHERE vsg.sense_id = vs.id
                            ),
                            'tags', (
                                SELECT json_agg(json_build_object(
                                    'code', t.code,
                                    'description', t.description,
                                    'category', t.category,
                                    'type', vst.tag_type
                                ))
                                FROM jlpt.vocabulary_sense_tag vst
                                JOIN jlpt.tag t ON vst.tag_code = t.code
                                WHERE vst.sense_id = vs.id
                            )
                        ) as sense_data
                    FROM jlpt.vocabulary_sense vs
                    WHERE vs.vocabulary_id = v.id
                    ORDER BY vs.created_at
                    LIMIT 3
                ) senses
                ) as senses,
                CASE 
                    WHEN (
                        SELECT vk.is_common 
                        FROM jlpt.vocabulary_kanji vk 
                        WHERE vk.vocabulary_id = v.id 
                        ORDER BY vk.is_common DESC, vk.created_at ASC 
                        LIMIT 1
                    ) = true 
                    OR (
                        SELECT vkn.is_common 
                        FROM jlpt.vocabulary_kana vkn 
                        WHERE vkn.vocabulary_id = v.id 
                        ORDER BY vkn.is_common DESC, vkn.created_at ASC 
                        LIMIT 1
                    ) = true 
                    THEN true 
                    ELSE false 
                END as is_common,
                NULL::json as translations,
                NULL::text as literal,
                NULL::integer as grade,
                NULL::integer as stroke_count,
                NULL::integer as frequency,
                NULL::json as kunyomi,
                NULL::json as onyomi,
                NULL::json as meanings_kanji,
                NULL::json as radicals
            FROM jlpt.vocabulary v
            JOIN vocabulary_ranked_matches rm ON v.id = rm.vocabulary_id
            WHERE rm.max_relevance::double precision >= relevanceThreshold
        ),
        all_results AS (
            SELECT * FROM (
                SELECT 
                    vd.id, vd.entry_type, vd.dict_id, vd.jlpt_level, vd.relevance_score,
                    vd.primary_kanji, vd.other_kanji_forms, vd.primary_kana, vd.other_kana_forms,
                    vd.senses, vd.is_common, vd.translations,
                    vd.literal, vd.grade, vd.stroke_count, vd.frequency,
                    vd.kunyomi, vd.onyomi, vd.meanings_kanji, vd.radicals
                FROM vocabulary_data vd
                LIMIT pageSize OFFSET page_offset
            ) v

            UNION ALL

            SELECT * FROM (
                SELECT 
                    pnd.id, pnd.entry_type, pnd.dict_id, pnd.jlpt_level, pnd.relevance_score,
                    pnd.primary_kanji, pnd.other_kanji_forms, pnd.primary_kana, pnd.other_kana_forms,
                    pnd.senses, pnd.is_common, pnd.translations,
                    pnd.literal, pnd.grade, pnd.stroke_count, pnd.frequency,
                    pnd.kunyomi, pnd.onyomi, pnd.meanings_kanji, pnd.radicals
                FROM proper_noun_data pnd
                LIMIT pageSize OFFSET page_offset
            ) p

            UNION ALL

            SELECT * FROM (
                SELECT 
                    kd.id, kd.entry_type, kd.dict_id, kd.jlpt_level, kd.relevance_score,
                    kd.primary_kanji, kd.other_kanji_forms, kd.primary_kana, kd.other_kana_forms,
                    kd.senses, kd.is_common, kd.translations,
                    kd.literal, kd.grade, kd.stroke_count, kd.frequency,
                    kd.kunyomi, kd.onyomi, kd.meanings_kanji, kd.radicals
                FROM kanji_data kd
                LIMIT pageSize OFFSET page_offset
            ) k
        ),
        total_count_vocab AS (
            SELECT COUNT(*) as total_count_vocab FROM vocabulary_data
        ),
        total_count_proper_noun as (
            SELECT COUNT(*) as total_count_proper_noun FROM proper_noun_data
        ),
        total_count_kanji as (
            SELECT COUNT(*) as total_count_kanji FROM kanji_data
        )
        SELECT
            ar.*,
            tcv.*,
            tcpn.*,
            tck.*
        FROM all_results ar
        CROSS JOIN total_count_vocab tcv
        cross join total_count_proper_noun tcpn
        cross join total_count_kanji tck
        ORDER BY
            ar.relevance_score DESC;
END;
$$ LANGUAGE plpgsql;

-- Function to search kanji by text (readings or meanings)
CREATE OR REPLACE FUNCTION search_kanji_by_text(
    queries TEXT[],
    relevanceThreshold FLOAT DEFAULT 0.4,
    pageSize INT DEFAULT 50,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    literal VARCHAR(1),
    grade INT,
    stroke_count INT,
    frequency INT,
    jlpt_level_new INT,
    relevance_score FLOAT,
    kunyomi JSON,
    onyomi JSON,
    meanings JSON,
    radicals JSON,
    total_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    WITH search_matches AS (
        -- Search by literal (exact match has highest priority)
        SELECT DISTINCT
            k.id as kanji_id,
            k.literal as match_text,
            'literal' as match_type,
            CASE
			    WHEN k.literal = q THEN 1.0
			    WHEN k.literal LIKE q || '%' THEN 0.95
			    WHEN k.literal LIKE '%' || q THEN 0.90
			    ELSE similarity(k.literal, q)
			END as relevance
        FROM jlpt.kanji k
		CROSS JOIN LATERAL unnest(queries) AS q
        WHERE 
		    (k.literal % q AND similarity(k.literal, q) >= relevanceThreshold)
		    OR k.literal LIKE q || '%'
		    OR k.literal LIKE '%' || q
        
        UNION ALL
        
        -- Search in readings (kunyomi and onyomi)
        SELECT DISTINCT
            k.id as kanji_id,
            kr.value as match_text,
            'reading' as match_type,
            CASE
			    WHEN kr.value = q THEN 1.0
			    WHEN kr.value LIKE q || '%' THEN 0.95
			    WHEN kr.value LIKE '%' || q THEN 0.90
			    ELSE similarity(kr.value, q)
			END as relevance
        FROM jlpt.kanji k
        JOIN jlpt.kanji_reading kr ON k.id = kr.kanji_id
		CROSS JOIN LATERAL unnest(queries) AS q
        WHERE 
		    (kr.value % q AND similarity(kr.value, q) >= relevanceThreshold)
		    OR kr.value LIKE q || '%'
		    OR kr.value LIKE '%' || q
        
        UNION ALL
        
        -- Search in meanings
        SELECT DISTINCT
            k.id as kanji_id,
            km.value as match_text,
            'meaning' as match_type,
            CASE
			    WHEN km.value = q THEN 1.0
			    WHEN km.value LIKE q || '%' THEN 0.95
			    WHEN km.value LIKE '%' || q THEN 0.90
			    ELSE similarity(km.value, q)
			END as relevance
        FROM jlpt.kanji k
        JOIN jlpt.kanji_meaning km ON k.id = km.kanji_id
		CROSS JOIN LATERAL unnest(queries) AS q
        WHERE 
		    (km.value % q AND similarity(km.value, q) >= relevanceThreshold)
		    OR km.value LIKE q || '%'
		    OR km.value LIKE '%' || q
    ),
    ranked_matches AS (
        SELECT 
            kanji_id,
            MAX(relevance)::double precision as max_relevance
        FROM search_matches
        GROUP BY kanji_id
    ),
    kanji_data AS (
        SELECT
            k.id,
            k.literal,
            k.grade,
            k.stroke_count,
            k.frequency,
            k.jlpt_level_new,
            rm.max_relevance::double precision as relevance_score,
            
            -- Get kunyomi readings (ja_kun)
            (SELECT json_agg(json_build_object(
                'type', kr.type,
                'value', kr.value,
                'status', kr.status
            ) ORDER BY kr.created_at)
            FROM jlpt.kanji_reading kr
            WHERE kr.kanji_id = k.id 
            AND kr.type = 'ja_kun'
            ) as kunyomi,
            
            -- Get onyomi readings (ja_on)
            (SELECT json_agg(json_build_object(
                'type', kr.type,
                'value', kr.value,
                'status', kr.status,
                'on_type', kr.on_type
            ) ORDER BY kr.created_at)
            FROM jlpt.kanji_reading kr
            WHERE kr.kanji_id = k.id 
            AND kr.type = 'ja_on'
            ) as onyomi,
            
            -- Get meanings
            (SELECT json_agg(json_build_object(
                'language', km.lang,
                'meaning', km.value
            ) ORDER BY km.created_at)
            FROM jlpt.kanji_meaning km
            WHERE km.kanji_id = k.id
            ) as meanings,
            
            -- Get radicals
            (SELECT json_agg(json_build_object(
                'id', r.id::text,
                'literal', r.literal
            ))
            FROM jlpt.kanji_radical kr
            JOIN jlpt.radical r ON kr.radical_id = r.id
            WHERE kr.kanji_id = k.id
            ) as radicals
            
        FROM jlpt.kanji k
        JOIN ranked_matches rm ON k.id = rm.kanji_id
    ),
    all_results AS (
    SELECT 
        kd.id,
        kd.literal,
        kd.grade,
        kd.stroke_count,
        kd.frequency,
        kd.jlpt_level_new,
        kd.relevance_score,
        kd.kunyomi,
        kd.onyomi,
        kd.meanings,
        kd.radicals
    FROM kanji_data kd
    WHERE kd.relevance_score >= relevanceThreshold
    ORDER BY kd.relevance_score DESC, kd.frequency ASC NULLS LAST
    ),
    total_count AS (
        SELECT COUNT(*) as total_count FROM all_results
    )
    SELECT
        ar.*,
        tc.total_count
    FROM all_results ar
    CROSS JOIN total_count tc
    ORDER BY
        ar.relevance_score DESC
    LIMIT pageSize OFFSET page_offset;
END;
$$ LANGUAGE plpgsql;

-- Function to search vocabulary by text
CREATE OR REPLACE FUNCTION search_vocabulary_by_text(
    queries TEXT[],
    relevanceThreshold FLOAT DEFAULT 0.4,
    pageSize INT DEFAULT 50,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    dict_id VARCHAR(30),
    jlpt_level INT,
    relevance_score FLOAT,
    primary_kanji JSON,
    primary_kana JSON,
    other_kanji_forms JSON,
    other_kana_forms JSON,
    senses JSON,
    is_common BOOLEAN,
    total_count BIGINT
) AS $$
BEGIN
    -- We'll produce a ranked set by combining several indexed candidate sets.
    RETURN QUERY
    WITH search_matches AS (
			-- Search in kanji
       		SELECT DISTINCT
                v.id as vocabulary_id,
                vk.text as match_text,
                'kanji' as match_type,
                CASE
				    WHEN vk.text = q THEN 1.0
				    WHEN vk.text LIKE q || '%' THEN 0.95
				    WHEN vk.text LIKE '%' || q THEN 0.90
				    ELSE similarity(vk.text, q)
				END as relevance
	        FROM jlpt.vocabulary v
	        JOIN jlpt.vocabulary_kanji vk ON v.id = vk.vocabulary_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE (vk.text % q AND similarity(vk.text, q) >= relevanceThreshold)
			    OR vk.text LIKE q || '%'
			    OR vk.text LIKE '%' || q
            
            UNION ALL
            
            -- Search in kana
            SELECT DISTINCT
                v.id as vocabulary_id,
                vkn.text as match_text,
                'kana' as match_type,
                CASE
				    WHEN vkn.text = q THEN 1.0
				    WHEN vkn.text LIKE q || '%' THEN 0.95
				    WHEN vkn.text LIKE '%' || q THEN 0.90
			    	ELSE similarity(vkn.text, q)
				END as relevance
	        FROM jlpt.vocabulary v
	        JOIN jlpt.vocabulary_kana vkn ON v.id = vkn.vocabulary_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE (vkn.text % q AND similarity(vkn.text, q) >= relevanceThreshold)
			    OR vkn.text LIKE q || '%'
			    OR vkn.text LIKE '%' || q
            
            UNION ALL
            
            -- Search in translations
            SELECT DISTINCT
                v.id as vocabulary_id,
                vsg.text as match_text,
                'gloss' as match_type,
                CASE
				    WHEN vsg.text = q THEN 1.0
				    WHEN vsg.text LIKE q || '%' THEN 0.95
				    WHEN vsg.text LIKE '%' || q THEN 0.90
			    	ELSE similarity(vsg.text, q)
				END as relevance
	        FROM jlpt.vocabulary v
	        JOIN jlpt.vocabulary_sense vs ON v.id = vs.vocabulary_id
	        JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE (vsg.text % q AND similarity(vsg.text, q) >= relevanceThreshold)
			    OR vsg.text LIKE q || '%'
			    OR vsg.text LIKE '%' || q
    ),
    ranked_matches AS (
        SELECT 
            vocabulary_id,
            MAX(relevance)::double precision as max_relevance
        FROM search_matches
        GROUP BY vocabulary_id
    ),
    vocabulary_data AS (
        SELECT
            v.id,
            v.jmdict_id as dict_id,
			v.jlpt_level_new as jlpt_level,
            rm.max_relevance::double precision as relevance_score,

            
            -- Get primary kanji (first common one, or first one)
            (SELECT json_build_object(
                    'text', vk.text,
                    'is_common', vk.is_common,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.vocabulary_kanji_tag vkt
                        JOIN jlpt.tag t ON vkt.tag_code = t.code
                        WHERE vkt.vocabulary_kanji_id = vk.id),
                        '[]'::json
                    )
                )
                FROM jlpt.vocabulary_kanji vk
                WHERE vk.vocabulary_id = v.id
                ORDER BY vk.is_common DESC, vk.created_at ASC
                LIMIT 1
                ) as primary_kanji,
            
            -- Get primary kana (respecting applies_to_kanji)
            (SELECT json_build_object(
                    'text', vkn.text,
                    'is_common', vkn.is_common,
                    'applies_to_kanji', vkn.applies_to_kanji,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.vocabulary_kana_tag vknt
                        JOIN jlpt.tag t ON vknt.tag_code = t.code
                        WHERE vknt.vocabulary_kana_id = vkn.id),
                        '[]'::json
                    )
                )
                FROM jlpt.vocabulary_kana vkn
                WHERE vkn.vocabulary_id = v.id
                ORDER BY vkn.is_common DESC, vkn.created_at ASC
                LIMIT 1
                ) as primary_kana,
            
            -- Get all other kanji forms
            (SELECT json_agg(json_build_object(
                    'text', vk.text,
                    'is_common', vk.is_common,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.vocabulary_kanji_tag vkt
                        JOIN jlpt.tag t ON vkt.tag_code = t.code
                        WHERE vkt.vocabulary_kanji_id = vk.id),
                        '[]'::json
                    )
                ) ORDER BY vk.is_common DESC, vk.created_at ASC)
                FROM jlpt.vocabulary_kanji vk
                WHERE vk.vocabulary_id = v.id
                OFFSET 1
                ) as other_kanji_forms,
            
            -- Get all other kana forms
            (SELECT json_agg(json_build_object(
                    'text', vkn.text,
                    'is_common', vkn.is_common,
                    'applies_to_kanji', vkn.applies_to_kanji,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.vocabulary_kana_tag vknt
                        JOIN jlpt.tag t ON vknt.tag_code = t.code
                        WHERE vknt.vocabulary_kana_id = vkn.id),
                        '[]'::json
                    )
                ) ORDER BY vkn.is_common DESC, vkn.created_at ASC)
                FROM jlpt.vocabulary_kana vkn
                WHERE vkn.vocabulary_id = v.id
                OFFSET 1
                ) as other_kana_forms,
            
            -- Get first 3 senses with their data
            (SELECT json_agg(sense_data ORDER BY sense_order)
                FROM (
                    SELECT 
                        vs.id,
                        ROW_NUMBER() OVER (ORDER BY vs.created_at) as sense_order,
                        json_build_object(
                            'applies_to_kanji', vs.applies_to_kanji,
                            'applies_to_kana', vs.applies_to_kana,
                            'info', vs.info,
                            'glosses', (
                                SELECT json_agg(json_build_object(
                                    'language', vsg.lang,
                                    'text', vsg.text,
                                    'gender', vsg.gender,
                                    'type', vsg.type
                                ))
                                FROM jlpt.vocabulary_sense_gloss vsg
                                WHERE vsg.sense_id = vs.id
                            ),
                            'tags', (
                                SELECT json_agg(json_build_object(
                                    'code', t.code,
                                    'description', t.description,
                                    'category', t.category,
                                    'type', vst.tag_type
                                ))
                                FROM jlpt.vocabulary_sense_tag vst
                                JOIN jlpt.tag t ON vst.tag_code = t.code
                                WHERE vst.sense_id = vs.id
                            )
                        ) as sense_data
                    FROM jlpt.vocabulary_sense vs
                    WHERE vs.vocabulary_id = v.id
                    ORDER BY vs.created_at
                    LIMIT 3
                ) senses
                ) as senses,
				CASE 
                    WHEN (
                        SELECT vk.is_common 
                        FROM jlpt.vocabulary_kanji vk 
                        WHERE vk.vocabulary_id = v.id 
                        ORDER BY vk.is_common DESC, vk.created_at ASC 
                        LIMIT 1
                    ) = true 
                    OR (
                        SELECT vkn.is_common 
                        FROM jlpt.vocabulary_kana vkn 
                        WHERE vkn.vocabulary_id = v.id 
                        ORDER BY vkn.is_common DESC, vkn.created_at ASC 
                        LIMIT 1
                    ) = true 
                    THEN true 
                    ELSE false 
                END as is_common
            
        FROM jlpt.vocabulary v
        JOIN ranked_matches rm ON v.id = rm.vocabulary_id
    ),
    all_results AS (
        SELECT 
            vd.id,
            vd.dict_id,
			vd.jlpt_level,
            vd.relevance_score,
            vd.primary_kanji,
            vd.primary_kana,
            vd.other_kanji_forms,
            vd.other_kana_forms,
            vd.senses,
			vd.is_common
        FROM vocabulary_data vd
        WHERE vd.relevance_score >= relevanceThreshold
        ORDER BY vd.relevance_score DESC
    ),
    total_count AS (
        SELECT COUNT(*) as total_count FROM all_results
    )
    SELECT
        ar.*,
        tc.total_count
    FROM all_results ar
    CROSS JOIN total_count tc
    ORDER BY
        ar.relevance_score DESC
    LIMIT pageSize OFFSET page_offset;
END;
$$ LANGUAGE plpgsql;

-- Function to get proper nouns by text
CREATE OR REPLACE FUNCTION search_proper_noun_by_text(
    queries TEXT[],
    relevanceThreshold FLOAT DEFAULT 0.4,
    pageSize INT DEFAULT 50,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    dict_id VARCHAR(30),
    relevance_score FLOAT,
    primary_kanji JSON,
    primary_kana JSON,
    other_kanji_forms JSON,
    other_kana_forms JSON,
    translations JSON,
    total_count BIGINT
) AS $$
BEGIN
    -- We'll produce a ranked set by combining several indexed candidate sets.
    RETURN QUERY
    WITH search_matches AS (
			-- Search in kanji
       		SELECT DISTINCT
                pn.id as proper_noun_id,
                pnk.text as match_text,
                'kanji' as match_type,
                CASE
				    WHEN pnk.text = q THEN 1.0
				    WHEN pnk.text LIKE q || '%' THEN 0.95
				    WHEN pnk.text LIKE '%' || q THEN 0.90
				    ELSE similarity(pnk.text, q)
				END as relevance
	        FROM jlpt.proper_noun pn
	        JOIN jlpt.proper_noun_kanji pnk ON pn.id = pnk.proper_noun_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE (pnk.text % q AND similarity(pnk.text, q) >= relevanceThreshold)
			    OR pnk.text LIKE q || '%'
			    OR pnk.text LIKE '%' || q
            
            UNION ALL
            
            -- Search in kana
            SELECT DISTINCT
                pn.id as proper_noun_id,
                pnkn.text as match_text,
                'kana' as match_type,
                CASE
				    WHEN pnkn.text = q THEN 1.0
				    WHEN pnkn.text LIKE q || '%' THEN 0.95
				    WHEN pnkn.text LIKE '%' || q THEN 0.90
				    ELSE similarity(pnkn.text, q)
				END as relevance
	        FROM jlpt.proper_noun pn
	        JOIN jlpt.proper_noun_kana pnkn ON pn.id = pnkn.proper_noun_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE (pnkn.text % q AND similarity(pnkn.text, q) >= relevanceThreshold)
			    OR pnkn.text LIKE q || '%'
			    OR pnkn.text LIKE '%' || q
            
            UNION ALL
            
            -- Search in translations
            SELECT DISTINCT
                pn.id as proper_noun_id,
                pntt.text as match_text,
                'translation' as match_type,
                CASE
				    WHEN pntt.text = q THEN 1.0
				    WHEN pntt.text LIKE q || '%' THEN 0.95
				    WHEN pntt.text LIKE '%' || q THEN 0.90
				    ELSE similarity(pntt.text, q)
				END as relevance
	        FROM jlpt.proper_noun pn
	        JOIN jlpt.proper_noun_translation pnt ON pn.id = pnt.proper_noun_id
	        JOIN jlpt.proper_noun_translation_text pntt ON pnt.id = pntt.translation_id
			CROSS JOIN LATERAL unnest(queries) AS q
	        WHERE (pntt.text % q AND similarity(pntt.text, q) >= relevanceThreshold)
			    OR pntt.text LIKE q || '%'
			    OR pntt.text LIKE '%' || q
    ),
    ranked_matches AS (
        SELECT 
            proper_noun_id,
            MAX(relevance)::double precision as max_relevance
        FROM search_matches
        GROUP BY proper_noun_id
    ),
    proper_noun_data AS (
        SELECT
            pn.id,
            pn.jmnedict_id as dict_id,
            rm.max_relevance::double precision as relevance_score,

            
            -- Get primary kanji (first common one, or first one)
            (SELECT json_build_object(
                    'text', pnk.text,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.proper_noun_kanji_tag pnkt
                        JOIN jlpt.tag t ON pnkt.tag_code = t.code
                        WHERE pnkt.proper_noun_kanji_id = pnk.id),
                        '[]'::json
                    )
                )
                FROM jlpt.proper_noun_kanji pnk
                WHERE pnk.proper_noun_id = pn.id
                ORDER BY pnk.created_at ASC
                LIMIT 1
                ) as primary_kanji,
            
            -- Get primary kana (respecting applies_to_kanji)
            (SELECT json_build_object(
                    'text', pnkn.text,
                    'applies_to_kanji', pnkn.applies_to_kanji,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.proper_noun_kana_tag pnknt
                        JOIN jlpt.tag t ON pnknt.tag_code = t.code
                        WHERE pnknt.proper_noun_kana_id = pnkn.id),
                        '[]'::json
                    )
                )
                FROM jlpt.proper_noun_kana pnkn
                WHERE pnkn.proper_noun_id = pn.id
                ORDER BY pnkn.created_at ASC
                LIMIT 1
                ) as primary_kana,
            
            -- Get all other kanji forms
            (SELECT json_agg(json_build_object(
                    'text', pnk.text,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.proper_noun_kanji_tag pnkt
                        JOIN jlpt.tag t ON pnkt.tag_code = t.code
                        WHERE pnkt.proper_noun_kanji_id = pnk.id),
                        '[]'::json
                    )
                ) ORDER BY pnk.created_at ASC)
                FROM jlpt.proper_noun_kanji pnk
                WHERE pnk.proper_noun_id = pn.id
                OFFSET 1
                ) as other_kanji_forms,
            
            -- Get all other kana forms
            (SELECT json_agg(json_build_object(
                    'text', pnkn.text,
                    'applies_to_kanji', pnkn.applies_to_kanji,
                    'tags', COALESCE(
                        (SELECT json_agg(json_build_object(
                            'code', t.code,
                            'description', t.description,
                            'category', t.category
                        ))
                        FROM jlpt.proper_noun_kana_tag pnknt
                        JOIN jlpt.tag t ON pnknt.tag_code = t.code
                        WHERE pnknt.proper_noun_kana_id = pnkn.id),
                        '[]'::json
                    )
                ) ORDER BY pnkn.created_at ASC)
                FROM jlpt.proper_noun_kana pnkn
                WHERE pnkn.proper_noun_id = pn.id
                OFFSET 1
                ) as other_kana_forms,
            
            -- Get translations with their data
            (SELECT json_agg(translation_data ORDER BY translation_order)
                FROM (
                    SELECT 
                        pnt.id,
                        ROW_NUMBER() OVER (ORDER BY pnt.created_at) as translation_order,
                        json_build_object(
                            'types', (
                                SELECT json_agg(json_build_object(
                                    'code', t.code,
                                    'description', t.description,
                                    'category', t.category
                                ))
                                FROM jlpt.proper_noun_translation_type pntt
                                JOIN jlpt.tag t ON pntt.tag_code = t.code
                                WHERE pntt.translation_id = pnt.id
                            ),
                            'translations', (
                                SELECT json_agg(json_build_object(
                                    'language', pnttxt.lang,
                                    'text', pnttxt.text
                                ))
                                FROM jlpt.proper_noun_translation_text pnttxt
                                WHERE pnttxt.translation_id = pnt.id
                            )
                        ) as translation_data
                    FROM jlpt.proper_noun_translation pnt
                    WHERE pnt.proper_noun_id = pn.id
                    ORDER BY pnt.created_at
                ) translations
                ) as translations
            
        FROM jlpt.proper_noun pn
        JOIN ranked_matches rm ON pn.id = rm.proper_noun_id
    ),
    all_results AS (
        SELECT 
            pnd.id,
            pnd.dict_id,
            pnd.relevance_score,
            pnd.primary_kanji,
            pnd.primary_kana,
            pnd.other_kanji_forms,
            pnd.other_kana_forms,
            pnd.translations
        FROM proper_noun_data pnd
        WHERE pnd.relevance_score >= relevanceThreshold
        ORDER BY pnd.relevance_score DESC
    ),
    total_count AS (
        SELECT COUNT(*) as total_count FROM all_results
    )
    SELECT
        ar.*,
        tc.total_count
    FROM all_results ar
    CROSS JOIN total_count tc
    ORDER BY
        ar.relevance_score DESC
    LIMIT pageSize OFFSET page_offset;
END;
$$ LANGUAGE plpgsql;

-- Functions to search by complex user query
CREATE OR REPLACE FUNCTION jlpt.search_kanji_ranked(
    patterns TEXT[],                    -- Pre-built LIKE patterns (e.g., '食%')
    exact_terms TEXT[],                 -- Exact terms for ranking (without wildcards)
    has_user_wildcard BOOLEAN,          -- Whether user used wildcards
    jlpt_levels INT[],                  -- Filter: JLPT levels (empty = all)
    grades INT[],                       -- Filter: Grades (empty = all)
    stroke_min INT,                     -- Filter: Min stroke count (0 = no min)
    stroke_max INT,                     -- Filter: Max stroke count (0 = no max)
    page_size INT DEFAULT 20,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    literal VARCHAR(10),
    grade INT,
    stroke_count INT,
    frequency INT,
    jlpt_level INT,
    match_quality INT,                  -- 1000=exact, 500=prefix, 200=contains, 100=wildcard
    match_location INT,                 -- Bitmask: 16=literal, 32=reading, 64=meaning
    matched_text_length INT,
    all_readings TEXT[],
    all_meanings TEXT[],
    kunyomi JSON,
    onyomi JSON,
    meanings JSON,
    radicals JSON,
    total_count BIGINT
) AS $$
BEGIN
    -- If no patterns, return ordered by frequency/JLPT/grade
    IF patterns IS NULL OR array_length(patterns, 1) IS NULL THEN
        RETURN QUERY
        WITH base AS (
            SELECT k.id, k.literal, k.grade, k.stroke_count, k.frequency, k.jlpt_level_new
            FROM jlpt.kanji k
            WHERE (jlpt_levels IS NULL OR array_length(jlpt_levels, 1) IS NULL OR k.jlpt_level_new = ANY(jlpt_levels))
              AND (grades IS NULL OR array_length(grades, 1) IS NULL OR k.grade = ANY(grades))
              AND (stroke_min <= 0 OR k.stroke_count >= stroke_min)
              AND (stroke_max <= 0 OR k.stroke_count <= stroke_max)
        ),
        counted AS (SELECT COUNT(*) as cnt FROM base),
        paginated AS (
            SELECT b.* FROM base b
            ORDER BY b.frequency NULLS LAST, b.jlpt_level_new NULLS LAST, b.grade NULLS LAST, b.id
            LIMIT page_size OFFSET page_offset
        )
        SELECT 
            p.id, p.literal, p.grade, p.stroke_count, p.frequency, 
            p.jlpt_level_new AS jlpt_level,
            0::INT AS match_quality, 0::INT AS match_location, 0::INT AS matched_text_length,
            ARRAY(SELECT kr.value::TEXT FROM jlpt.kanji_reading kr WHERE kr.kanji_id = p.id) AS all_readings,
            ARRAY(SELECT km.value::TEXT FROM jlpt.kanji_meaning km WHERE km.kanji_id = p.id AND km.lang = 'en') AS all_meanings,
            (SELECT COALESCE(json_agg(json_build_object('id', kr.id, 'type', kr.type, 'value', kr.value, 'status', kr.status, 'onType', kr.on_type) ORDER BY kr.id), '[]'::json)
             FROM jlpt.kanji_reading kr WHERE kr.kanji_id = p.id AND kr.type = 'ja_kun') AS kunyomi,
            (SELECT COALESCE(json_agg(json_build_object('id', kr.id, 'type', kr.type, 'value', kr.value, 'status', kr.status, 'onType', kr.on_type) ORDER BY kr.id), '[]'::json)
             FROM jlpt.kanji_reading kr WHERE kr.kanji_id = p.id AND kr.type = 'ja_on') AS onyomi,
            (SELECT COALESCE(json_agg(json_build_object('id', km.id, 'language', km.lang, 'meaning', km.value) ORDER BY km.id), '[]'::json)
             FROM jlpt.kanji_meaning km WHERE km.kanji_id = p.id) AS meanings,
            (SELECT COALESCE(json_agg(json_build_object('id', kr.id, 'literal', r.literal) ORDER BY kr.id), '[]'::json)
             FROM jlpt.kanji_radical kr JOIN jlpt.radical r ON kr.radical_id = r.id WHERE kr.kanji_id = p.id) AS radicals,
            (SELECT cnt FROM counted) AS total_count
        FROM paginated p;
        RETURN;
    END IF;

    -- Main search with patterns
    RETURN QUERY
    WITH 
    -- Match on literal
    literal_matches AS (
        SELECT 
            k.id as kanji_id,
            k.literal as matched_text,
            16 as location_flag,
            CASE 
                WHEN NOT has_user_wildcard AND k.literal = ANY(exact_terms) THEN 1000
                WHEN NOT has_user_wildcard AND EXISTS (SELECT 1 FROM unnest(exact_terms) et WHERE k.literal LIKE et || '%') THEN 500
                ELSE CASE WHEN has_user_wildcard THEN 100 ELSE 200 END
            END as quality
        FROM jlpt.kanji k
        WHERE k.literal LIKE ANY(patterns)
    ),
    -- Match on readings
    reading_matches AS (
        SELECT DISTINCT ON (kr.kanji_id)
            kr.kanji_id,
            kr.value as matched_text,
            32 as location_flag,
            CASE 
                WHEN NOT has_user_wildcard AND kr.value = ANY(exact_terms) THEN 1000
                WHEN NOT has_user_wildcard AND EXISTS (SELECT 1 FROM unnest(exact_terms) et WHERE kr.value LIKE et || '%') THEN 500
                ELSE CASE WHEN has_user_wildcard THEN 100 ELSE 200 END
            END as quality
        FROM jlpt.kanji_reading kr
        WHERE kr.value LIKE ANY(patterns)
        ORDER BY kr.kanji_id,
            CASE WHEN NOT has_user_wildcard AND kr.value = ANY(exact_terms) THEN 0 ELSE 1 END,
            length(kr.value)
    ),
    -- Match on meanings
    meaning_matches AS (
        SELECT DISTINCT ON (km.kanji_id)
            km.kanji_id,
            km.value as matched_text,
            64 as location_flag,
            CASE 
                WHEN NOT has_user_wildcard AND lower(km.value) = ANY(SELECT lower(unnest(exact_terms))) THEN 1000
                WHEN NOT has_user_wildcard AND EXISTS (SELECT 1 FROM unnest(exact_terms) et WHERE km.value ILIKE et || '%') THEN 500
                ELSE CASE WHEN has_user_wildcard THEN 100 ELSE 200 END
            END as quality
        FROM jlpt.kanji_meaning km
        WHERE km.lang = 'en' AND km.value ILIKE ANY(patterns)
        ORDER BY km.kanji_id,
            CASE WHEN NOT has_user_wildcard AND lower(km.value) = ANY(SELECT lower(unnest(exact_terms))) THEN 0 ELSE 1 END,
            length(km.value)
    ),
    -- Combine matches
    match_info AS (
        SELECT 
            kanji_id,
            MAX(quality) as best_quality,
            BIT_OR(location_flag)::INT as locations,
            MIN(length(matched_text)) as shortest_match
        FROM (
            SELECT kanji_id, matched_text, location_flag, quality FROM literal_matches
            UNION ALL
            SELECT kanji_id, matched_text, location_flag, quality FROM reading_matches
            UNION ALL
            SELECT kanji_id, matched_text, location_flag, quality FROM meaning_matches
        ) all_matches
        WHERE quality > 0
        GROUP BY kanji_id
    ),
    -- Apply filters
    filtered AS (
        SELECT 
            k.id, k.literal, k.grade, k.stroke_count, k.frequency, k.jlpt_level_new,
            mi.best_quality, mi.locations, mi.shortest_match
        FROM match_info mi
        JOIN jlpt.kanji k ON k.id = mi.kanji_id
        WHERE (jlpt_levels IS NULL OR array_length(jlpt_levels, 1) IS NULL OR k.jlpt_level_new = ANY(jlpt_levels))
          AND (grades IS NULL OR array_length(grades, 1) IS NULL OR k.grade = ANY(grades))
          AND (stroke_min <= 0 OR k.stroke_count >= stroke_min)
          AND (stroke_max <= 0 OR k.stroke_count <= stroke_max)
    ),
    counted AS (SELECT COUNT(*) as cnt FROM filtered),
    paginated AS (
        SELECT f.* FROM filtered f
        ORDER BY f.best_quality DESC, f.frequency NULLS LAST, f.jlpt_level_new NULLS LAST, f.grade NULLS LAST, f.id
        LIMIT page_size OFFSET page_offset
    )
    SELECT 
        p.id, p.literal, p.grade, p.stroke_count, p.frequency, p.jlpt_level_new AS jlpt_level,
        p.best_quality AS match_quality, 
        p.locations AS match_location, 
        p.shortest_match AS matched_text_length,
        ARRAY(SELECT kr.value::TEXT FROM jlpt.kanji_reading kr WHERE kr.kanji_id = p.id) AS all_readings,
        ARRAY(SELECT km.value::TEXT FROM jlpt.kanji_meaning km WHERE km.kanji_id = p.id AND km.lang = 'en') AS all_meanings,
        (SELECT COALESCE(json_agg(json_build_object('id', kr.id, 'type', kr.type, 'value', kr.value, 'status', kr.status, 'onType', kr.on_type) ORDER BY kr.id), '[]'::json)
         FROM jlpt.kanji_reading kr WHERE kr.kanji_id = p.id AND kr.type = 'ja_kun') AS kunyomi,
        (SELECT COALESCE(json_agg(json_build_object('id', kr.id, 'type', kr.type, 'value', kr.value, 'status', kr.status, 'onType', kr.on_type) ORDER BY kr.id), '[]'::json)
         FROM jlpt.kanji_reading kr WHERE kr.kanji_id = p.id AND kr.type = 'ja_on') AS onyomi,
        (SELECT COALESCE(json_agg(json_build_object('id', km.id, 'language', km.lang, 'meaning', km.value) ORDER BY km.id), '[]'::json)
         FROM jlpt.kanji_meaning km WHERE km.kanji_id = p.id) AS meanings,
        (SELECT COALESCE(json_agg(json_build_object('id', kr.id, 'literal', r.literal) ORDER BY kr.id), '[]'::json)
         FROM jlpt.kanji_radical kr JOIN jlpt.radical r ON kr.radical_id = r.id WHERE kr.kanji_id = p.id) AS radicals,
        (SELECT cnt FROM counted) AS total_count
    FROM paginated p;
END;
$$ LANGUAGE plpgsql STABLE;

CREATE OR REPLACE FUNCTION jlpt.search_vocabulary_ranked(
    patterns TEXT[],                    -- Pre-built LIKE patterns (e.g., 'たべ%')
    exact_terms TEXT[],                 -- Exact terms for ranking (without wildcards)
    has_user_wildcard BOOLEAN,          -- Whether user used wildcards
    jlpt_levels INT[],                  -- Filter: JLPT levels (empty = all)
    pos_tags TEXT[],                    -- Filter: Part of speech tags
    common_only BOOLEAN,                -- Filter: common words only
    filter_tags TEXT[],                 -- Filter: general tags
    page_size INT DEFAULT 20,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    dict_id VARCHAR(30),
    jlpt_level INT,
    is_common BOOLEAN,
    match_quality INT,                  -- 1000=exact, 500=prefix, 200=contains, 100=wildcard
    match_location INT,                 -- Bitmask: 1=kana, 2=kanji, 4=gloss, 8=first_sense
    matched_text_length INT,
    sense_count INT,
    all_kana_texts TEXT[],
    all_kanji_texts TEXT[],
    first_sense_glosses TEXT[],
    all_glosses TEXT[],
    primary_kanji JSON,
    primary_kana JSON,
    other_kanji_forms JSON,
    other_kana_forms JSON,
    senses JSON,
    total_count BIGINT
) AS $$
BEGIN
    -- If no patterns, return ordered by common/JLPT
    IF patterns IS NULL OR array_length(patterns, 1) IS NULL THEN
        RETURN QUERY
        WITH base AS (
            SELECT 
                v.id,
                v.jmdict_id,
                v.jlpt_level_new,
                EXISTS (SELECT 1 FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = v.id AND vk.is_common) as is_common
            FROM jlpt.vocabulary v
            WHERE (jlpt_levels IS NULL OR array_length(jlpt_levels, 1) IS NULL OR v.jlpt_level_new = ANY(jlpt_levels))
              AND (NOT common_only OR EXISTS (SELECT 1 FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = v.id AND vk.is_common))
        ),
        counted AS (SELECT COUNT(*) as cnt FROM base),
        paginated AS (
            SELECT b.* FROM base b
            ORDER BY b.is_common DESC, b.jlpt_level_new NULLS LAST, b.id
            LIMIT page_size OFFSET page_offset
        )
        SELECT 
            p.id,
            p.jmdict_id,
            p.jlpt_level_new,
            p.is_common,
            0::INT as match_quality,
            0::INT as match_location,
            0::INT as matched_text_length,
            (SELECT COUNT(*)::INT FROM jlpt.vocabulary_sense vs WHERE vs.vocabulary_id = p.id),
            ARRAY(SELECT vk.text FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_common DESC, vk.created_at),
            ARRAY(SELECT vk.text FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_common DESC, vk.created_at),
            ARRAY[]::TEXT[],
            ARRAY[]::TEXT[],
            (SELECT row_to_json(x) FROM (
                SELECT vk.text, vk.is_common as "isCommon", 
                    COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                        FROM jlpt.vocabulary_kanji_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kanji_id = vk.id), '[]'::json) as tags
                FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_common DESC, vk.created_at LIMIT 1
            ) x),
            (SELECT row_to_json(x) FROM (
                SELECT vk.text, vk.is_common as "isCommon",
                    COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                        FROM jlpt.vocabulary_kana_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kana_id = vk.id), '[]'::json) as tags
                FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_common DESC, vk.created_at LIMIT 1
            ) x),
            (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
                SELECT vk.text, vk.is_common as "isCommon",
                    COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                        FROM jlpt.vocabulary_kanji_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kanji_id = vk.id), '[]'::json) as tags
                FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_common DESC, vk.created_at OFFSET 1
            ) x),
            (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
                SELECT vk.text, vk.is_common as "isCommon",
                    COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                        FROM jlpt.vocabulary_kana_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kana_id = vk.id), '[]'::json) as tags
                FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_common DESC, vk.created_at OFFSET 1
            ) x),
            (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
                SELECT vs.applies_to_kanji as "appliesToKanji", vs.applies_to_kana as "appliesToKana", vs.info,
                    COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category, 'type', vst.tag_type))
                        FROM jlpt.vocabulary_sense_tag vst JOIN jlpt.tag t ON vst.tag_code = t.code WHERE vst.sense_id = vs.id), '[]'::json) as tags,
                    COALESCE((SELECT json_agg(json_build_object('language', vsg.lang, 'text', vsg.text) ORDER BY vsg.id)
                        FROM jlpt.vocabulary_sense_gloss vsg WHERE vsg.sense_id = vs.id), '[]'::json) as glosses
                FROM jlpt.vocabulary_sense vs WHERE vs.vocabulary_id = p.id ORDER BY vs.id LIMIT 3
            ) x),
            (SELECT cnt FROM counted)
        FROM paginated p;
        RETURN;
    END IF;

    -- Main search query with patterns
    RETURN QUERY
    WITH 
    -- Find kana matches
    kana_matches AS (
        SELECT DISTINCT ON (vk.vocabulary_id)
            vk.vocabulary_id,
            vk.text as matched_text,
            1 as location_flag,
            CASE 
                WHEN NOT has_user_wildcard AND vk.text = ANY(exact_terms) THEN 1000
                WHEN NOT has_user_wildcard AND EXISTS (SELECT 1 FROM unnest(exact_terms) et WHERE vk.text LIKE et || '%') THEN 500
                ELSE CASE WHEN has_user_wildcard THEN 100 ELSE 200 END
            END as quality
        FROM jlpt.vocabulary_kana vk
        WHERE vk.text LIKE ANY(patterns)
        ORDER BY vk.vocabulary_id, 
            CASE WHEN NOT has_user_wildcard AND vk.text = ANY(exact_terms) THEN 0 ELSE 1 END,
            length(vk.text)
    ),
    -- Find kanji matches
    kanji_matches AS (
        SELECT DISTINCT ON (vk.vocabulary_id)
            vk.vocabulary_id,
            vk.text as matched_text,
            2 as location_flag,
            CASE 
                WHEN NOT has_user_wildcard AND vk.text = ANY(exact_terms) THEN 1000
                WHEN NOT has_user_wildcard AND EXISTS (SELECT 1 FROM unnest(exact_terms) et WHERE vk.text LIKE et || '%') THEN 500
                ELSE CASE WHEN has_user_wildcard THEN 100 ELSE 200 END
            END as quality
        FROM jlpt.vocabulary_kanji vk
        WHERE vk.text LIKE ANY(patterns)
        ORDER BY vk.vocabulary_id,
            CASE WHEN NOT has_user_wildcard AND vk.text = ANY(exact_terms) THEN 0 ELSE 1 END,
            length(vk.text)
    ),
    -- Find gloss matches (with sense order for first_sense detection)
    gloss_matches AS (
        SELECT DISTINCT ON (vs.vocabulary_id)
            vs.vocabulary_id,
            vsg.text as matched_text,
            4 as location_flag,
            CASE 
                WHEN NOT has_user_wildcard AND lower(vsg.text) = ANY(SELECT lower(unnest(exact_terms))) THEN 1000
                WHEN NOT has_user_wildcard AND EXISTS (SELECT 1 FROM unnest(exact_terms) et WHERE vsg.text ILIKE et || '%') THEN 500
                ELSE CASE WHEN has_user_wildcard THEN 100 ELSE 200 END
            END as quality,
            (ROW_NUMBER() OVER (PARTITION BY vs.vocabulary_id ORDER BY vs.id, vsg.id)) as sense_order
        FROM jlpt.vocabulary_sense vs
        JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
        WHERE vsg.lang = 'eng' AND vsg.text ILIKE ANY(patterns)
        ORDER BY vs.vocabulary_id,
            CASE WHEN NOT has_user_wildcard AND lower(vsg.text) = ANY(SELECT lower(unnest(exact_terms))) THEN 0 ELSE 1 END,
            length(vsg.text)
    ),
    -- Combine matches and compute aggregates per vocabulary
    match_info AS (
        SELECT 
            vocabulary_id,
            MAX(quality) as best_quality,
            BIT_OR(location_flag) | CASE WHEN BOOL_OR(location_flag = 4 AND sense_order = 1) THEN 8 ELSE 0 END as locations,
            MIN(length(matched_text)) as shortest_match
        FROM (
            SELECT vocabulary_id, matched_text, location_flag, quality, NULL::BIGINT as sense_order FROM kana_matches
            UNION ALL
            SELECT vocabulary_id, matched_text, location_flag, quality, NULL FROM kanji_matches
            UNION ALL
            SELECT vocabulary_id, matched_text, location_flag, quality, sense_order FROM gloss_matches
        ) all_matches
        WHERE quality > 0
        GROUP BY vocabulary_id
    ),
    -- Apply filters
    filtered AS (
        SELECT 
            v.id,
            v.jmdict_id,
            v.jlpt_level_new,
            mi.best_quality,
            mi.locations,
            mi.shortest_match,
            EXISTS (SELECT 1 FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = v.id AND vk.is_common) OR
            EXISTS (SELECT 1 FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = v.id AND vk.is_common) as is_common
        FROM match_info mi
        JOIN jlpt.vocabulary v ON v.id = mi.vocabulary_id
        WHERE 
            (jlpt_levels IS NULL OR array_length(jlpt_levels, 1) IS NULL OR v.jlpt_level_new = ANY(jlpt_levels))
            AND (NOT common_only OR EXISTS (SELECT 1 FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = v.id AND vk.is_common))
            AND (pos_tags IS NULL OR array_length(pos_tags, 1) IS NULL OR EXISTS (
                SELECT 1 FROM jlpt.vocabulary_sense vs
                JOIN jlpt.vocabulary_sense_tag vst ON vs.id = vst.sense_id
                WHERE vs.vocabulary_id = v.id AND vst.tag_code = ANY(pos_tags)
            ))
            AND (filter_tags IS NULL OR array_length(filter_tags, 1) IS NULL 
                OR EXISTS (SELECT 1 FROM jlpt.vocabulary_kana vk JOIN jlpt.vocabulary_kana_tag vkt ON vk.id = vkt.vocabulary_kana_id WHERE vk.vocabulary_id = v.id AND vkt.tag_code = ANY(filter_tags))
                OR EXISTS (SELECT 1 FROM jlpt.vocabulary_kanji vk JOIN jlpt.vocabulary_kanji_tag vkt ON vk.id = vkt.vocabulary_kanji_id WHERE vk.vocabulary_id = v.id AND vkt.tag_code = ANY(filter_tags))
                OR EXISTS (SELECT 1 FROM jlpt.vocabulary_sense vs JOIN jlpt.vocabulary_sense_tag vst ON vs.id = vst.sense_id WHERE vs.vocabulary_id = v.id AND vst.tag_code = ANY(filter_tags))
            )
    ),
    counted AS (SELECT COUNT(*) as cnt FROM filtered),
    paginated AS (
        SELECT f.* FROM filtered f
        ORDER BY f.best_quality DESC, f.is_common DESC, COALESCE(f.jlpt_level_new, 99), f.id
        LIMIT page_size OFFSET page_offset
    )
    -- Final output
    SELECT 
        p.id,
        p.jmdict_id,
        p.jlpt_level_new,
        p.is_common,
        p.best_quality,
        p.locations,
        p.shortest_match,
        (SELECT COUNT(*)::INT FROM jlpt.vocabulary_sense vs WHERE vs.vocabulary_id = p.id),
        ARRAY(SELECT vk.text FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_common DESC, vk.created_at),
        ARRAY(SELECT vk.text FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_common DESC, vk.created_at),
        ARRAY(SELECT vsg.text FROM jlpt.vocabulary_sense vs JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id 
              WHERE vs.vocabulary_id = p.id AND vsg.lang = 'eng' ORDER BY vs.id LIMIT 5),
        ARRAY(SELECT vsg.text FROM jlpt.vocabulary_sense vs JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id 
              WHERE vs.vocabulary_id = p.id AND vsg.lang = 'eng'),
        (SELECT row_to_json(x) FROM (
            SELECT vk.text, vk.is_common as "isCommon",
                COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.vocabulary_kanji_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kanji_id = vk.id), '[]'::json) as tags
            FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_common DESC, vk.created_at LIMIT 1
        ) x),
        (SELECT row_to_json(x) FROM (
            SELECT vk.text, vk.is_common as "isCommon",
                COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.vocabulary_kana_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kana_id = vk.id), '[]'::json) as tags
            FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_common DESC, vk.created_at LIMIT 1
        ) x),
        (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
            SELECT vk.text, vk.is_common as "isCommon",
                COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.vocabulary_kanji_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kanji_id = vk.id), '[]'::json) as tags
            FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_common DESC, vk.created_at OFFSET 1
        ) x),
        (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
            SELECT vk.text, vk.is_common as "isCommon",
                COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.vocabulary_kana_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kana_id = vk.id), '[]'::json) as tags
            FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_common DESC, vk.created_at OFFSET 1
        ) x),
        (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
            SELECT vs.applies_to_kanji as "appliesToKanji", vs.applies_to_kana as "appliesToKana", vs.info,
                COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category, 'type', vst.tag_type))
                    FROM jlpt.vocabulary_sense_tag vst JOIN jlpt.tag t ON vst.tag_code = t.code WHERE vst.sense_id = vs.id), '[]'::json) as tags,
                COALESCE((SELECT json_agg(json_build_object('language', vsg.lang, 'text', vsg.text) ORDER BY vsg.id)
                    FROM jlpt.vocabulary_sense_gloss vsg WHERE vsg.sense_id = vs.id), '[]'::json) as glosses
            FROM jlpt.vocabulary_sense vs WHERE vs.vocabulary_id = p.id ORDER BY vs.id LIMIT 3
        ) x),
        (SELECT cnt FROM counted)
    FROM paginated p;
END;
$$ LANGUAGE plpgsql STABLE;

CREATE OR REPLACE FUNCTION jlpt.search_proper_noun_ranked(
    patterns TEXT[],                    -- Pre-built LIKE patterns
    exact_terms TEXT[],                 -- Exact terms for ranking (without wildcards)
    has_user_wildcard BOOLEAN,          -- Whether user used wildcards
    filter_tags TEXT[],                 -- Filter: tags
    page_size INT DEFAULT 20,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    dict_id VARCHAR(30),
    match_quality INT,                  -- 1000=exact, 500=prefix, 200=contains, 100=wildcard
    match_location INT,                 -- Bitmask: 1=kana, 2=kanji, 128=translation
    matched_text_length INT,
    all_kanji_texts TEXT[],
    all_kana_texts TEXT[],
    all_translation_texts TEXT[],
    primary_kanji JSON,
    primary_kana JSON,
    other_kanji_forms JSON,
    other_kana_forms JSON,
    translations JSON,
    total_count BIGINT
) AS $$
BEGIN
    -- If no patterns, return ordered by id
    IF patterns IS NULL OR array_length(patterns, 1) IS NULL THEN
        RETURN QUERY
        WITH base AS (
            SELECT p.id, p.jmnedict_id
            FROM jlpt.proper_noun p
            WHERE (filter_tags IS NULL OR array_length(filter_tags, 1) IS NULL 
                OR EXISTS (SELECT 1 FROM jlpt.proper_noun_kanji pk JOIN jlpt.proper_noun_kanji_tag pkt ON pk.id = pkt.proper_noun_kanji_id WHERE pk.proper_noun_id = p.id AND pkt.tag_code = ANY(filter_tags))
                OR EXISTS (SELECT 1 FROM jlpt.proper_noun_kana pk JOIN jlpt.proper_noun_kana_tag pkt ON pk.id = pkt.proper_noun_kana_id WHERE pk.proper_noun_id = p.id AND pkt.tag_code = ANY(filter_tags))
                OR EXISTS (SELECT 1 FROM jlpt.proper_noun_translation pt JOIN jlpt.proper_noun_translation_type ptt ON pt.id = ptt.translation_id WHERE pt.proper_noun_id = p.id AND ptt.tag_code = ANY(filter_tags))
            )
        ),
        counted AS (SELECT COUNT(*) as cnt FROM base),
        paginated AS (
            SELECT b.* FROM base b ORDER BY b.id LIMIT page_size OFFSET page_offset
        )
        SELECT 
            p.id, p.jmnedict_id,
            0::INT, 0::INT, 0::INT,
            ARRAY(SELECT pk.text FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = p.id),
            ARRAY(SELECT pk.text FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = p.id),
            ARRAY(SELECT ptt.text FROM jlpt.proper_noun_translation pt JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id WHERE pt.proper_noun_id = p.id),
            NULL::JSON, NULL::JSON, NULL::JSON, NULL::JSON, NULL::JSON,
            (SELECT cnt FROM counted)
        FROM paginated p;
        RETURN;
    END IF;

    -- Main search with patterns
    RETURN QUERY
    WITH 
    kanji_matches AS (
        SELECT DISTINCT ON (pk.proper_noun_id)
            pk.proper_noun_id,
            pk.text as matched_text,
            2 as location_flag,
            CASE 
                WHEN NOT has_user_wildcard AND pk.text = ANY(exact_terms) THEN 1000
                WHEN NOT has_user_wildcard AND EXISTS (SELECT 1 FROM unnest(exact_terms) et WHERE pk.text LIKE et || '%') THEN 500
                ELSE CASE WHEN has_user_wildcard THEN 100 ELSE 200 END
            END as quality
        FROM jlpt.proper_noun_kanji pk
        WHERE pk.text LIKE ANY(patterns)
        ORDER BY pk.proper_noun_id,
            CASE WHEN NOT has_user_wildcard AND pk.text = ANY(exact_terms) THEN 0 ELSE 1 END,
            length(pk.text)
    ),
    kana_matches AS (
        SELECT DISTINCT ON (pk.proper_noun_id)
            pk.proper_noun_id,
            pk.text as matched_text,
            1 as location_flag,
            CASE 
                WHEN NOT has_user_wildcard AND pk.text = ANY(exact_terms) THEN 1000
                WHEN NOT has_user_wildcard AND EXISTS (SELECT 1 FROM unnest(exact_terms) et WHERE pk.text LIKE et || '%') THEN 500
                ELSE CASE WHEN has_user_wildcard THEN 100 ELSE 200 END
            END as quality
        FROM jlpt.proper_noun_kana pk
        WHERE pk.text LIKE ANY(patterns)
        ORDER BY pk.proper_noun_id,
            CASE WHEN NOT has_user_wildcard AND pk.text = ANY(exact_terms) THEN 0 ELSE 1 END,
            length(pk.text)
    ),
    translation_matches AS (
        SELECT DISTINCT ON (pt.proper_noun_id)
            pt.proper_noun_id,
            ptt.text as matched_text,
            128 as location_flag,
            CASE 
                WHEN NOT has_user_wildcard AND lower(ptt.text) = ANY(SELECT lower(unnest(exact_terms))) THEN 1000
                WHEN NOT has_user_wildcard AND EXISTS (SELECT 1 FROM unnest(exact_terms) et WHERE ptt.text ILIKE et || '%') THEN 500
                ELSE CASE WHEN has_user_wildcard THEN 100 ELSE 200 END
            END as quality
        FROM jlpt.proper_noun_translation pt
        JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id
        WHERE ptt.text ILIKE ANY(patterns)
        ORDER BY pt.proper_noun_id,
            CASE WHEN NOT has_user_wildcard AND lower(ptt.text) = ANY(SELECT lower(unnest(exact_terms))) THEN 0 ELSE 1 END,
            length(ptt.text)
    ),
    match_info AS (
        SELECT 
            proper_noun_id,
            MAX(quality) as best_quality,
            BIT_OR(location_flag) as locations,
            MIN(length(matched_text)) as shortest_match
        FROM (
            SELECT proper_noun_id, matched_text, location_flag, quality FROM kanji_matches
            UNION ALL
            SELECT proper_noun_id, matched_text, location_flag, quality FROM kana_matches
            UNION ALL
            SELECT proper_noun_id, matched_text, location_flag, quality FROM translation_matches
        ) all_matches
        WHERE quality > 0
        GROUP BY proper_noun_id
    ),
    filtered AS (
        SELECT p.id, p.jmnedict_id, mi.best_quality, mi.locations, mi.shortest_match
        FROM match_info mi
        JOIN jlpt.proper_noun p ON p.id = mi.proper_noun_id
        WHERE (filter_tags IS NULL OR array_length(filter_tags, 1) IS NULL 
            OR EXISTS (SELECT 1 FROM jlpt.proper_noun_kanji pk JOIN jlpt.proper_noun_kanji_tag pkt ON pk.id = pkt.proper_noun_kanji_id WHERE pk.proper_noun_id = p.id AND pkt.tag_code = ANY(filter_tags))
            OR EXISTS (SELECT 1 FROM jlpt.proper_noun_kana pk JOIN jlpt.proper_noun_kana_tag pkt ON pk.id = pkt.proper_noun_kana_id WHERE pk.proper_noun_id = p.id AND pkt.tag_code = ANY(filter_tags))
            OR EXISTS (SELECT 1 FROM jlpt.proper_noun_translation pt JOIN jlpt.proper_noun_translation_type ptt ON pt.id = ptt.translation_id WHERE pt.proper_noun_id = p.id AND ptt.tag_code = ANY(filter_tags))
        )
    ),
    counted AS (SELECT COUNT(*) as cnt FROM filtered),
    paginated AS (
        SELECT f.* FROM filtered f
        ORDER BY f.best_quality DESC, f.id
        LIMIT page_size OFFSET page_offset
    )
    SELECT 
        p.id, p.jmnedict_id,
        p.best_quality, p.locations, p.shortest_match,
        ARRAY(SELECT pk.text FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = p.id ORDER BY pk.created_at),
        ARRAY(SELECT pk.text FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = p.id ORDER BY pk.created_at),
        ARRAY(SELECT ptt.text FROM jlpt.proper_noun_translation pt JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id WHERE pt.proper_noun_id = p.id),
        -- Primary kanji
        (SELECT row_to_json(x) FROM (
            SELECT pk.text, COALESCE((
                SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                FROM jlpt.proper_noun_kanji_tag pkt JOIN jlpt.tag t ON pkt.tag_code = t.code WHERE pkt.proper_noun_kanji_id = pk.id
            ), '[]'::json) as tags
            FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = p.id ORDER BY pk.created_at, pk.id LIMIT 1
        ) x),
        -- Primary kana
        (SELECT row_to_json(x) FROM (
            SELECT pk.text, pk.applies_to_kanji as "appliesToKanji", COALESCE((
                SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                FROM jlpt.proper_noun_kana_tag pkt JOIN jlpt.tag t ON pkt.tag_code = t.code WHERE pkt.proper_noun_kana_id = pk.id
            ), '[]'::json) as tags
            FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = p.id ORDER BY pk.created_at, pk.id LIMIT 1
        ) x),
        -- Other kanji forms
        (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
            SELECT pk.text, COALESCE((
                SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                FROM jlpt.proper_noun_kanji_tag pkt JOIN jlpt.tag t ON pkt.tag_code = t.code WHERE pkt.proper_noun_kanji_id = pk.id
            ), '[]'::json) as tags
            FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = p.id ORDER BY pk.created_at, pk.id OFFSET 1
        ) x),
        -- Other kana forms
        (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
            SELECT pk.text, pk.applies_to_kanji as "appliesToKanji", COALESCE((
                SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                FROM jlpt.proper_noun_kana_tag pkt JOIN jlpt.tag t ON pkt.tag_code = t.code WHERE pkt.proper_noun_kana_id = pk.id
            ), '[]'::json) as tags
            FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = p.id ORDER BY pk.created_at, pk.id OFFSET 1
        ) x),
        -- Translations
        (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
            SELECT 
                COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.proper_noun_translation_type ptt JOIN jlpt.tag t ON ptt.tag_code = t.code WHERE ptt.translation_id = pt.id), '[]'::json) as types,
                COALESCE((SELECT json_agg(json_build_object('language', ptt.lang, 'text', ptt.text) ORDER BY ptt.id)
                    FROM jlpt.proper_noun_translation_text ptt WHERE ptt.translation_id = pt.id), '[]'::json) as translations
            FROM jlpt.proper_noun_translation pt WHERE pt.proper_noun_id = p.id ORDER BY pt.id
        ) x),
        (SELECT cnt FROM counted)
    FROM paginated p;
END;
$$ LANGUAGE plpgsql STABLE;

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