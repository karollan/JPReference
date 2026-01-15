-- JLPT Reference Database Initialization Script
-- This script runs when the PostgreSQL container is first created

-- ============================================
-- TRIGGERS FOR UPDATED_AT COLUMNS
-- ============================================
-- Create triggers for all tables with updated_at columns

SET search_path TO jlpt, public;

-- Create a function to update the updated_at timestamp -> only used for triggers
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

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

CREATE TRIGGER trigger_radical_group_updated_at
    BEFORE UPDATE ON radical_group
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_radical_group_member_updated_at
    BEFORE UPDATE ON radical_group_member
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