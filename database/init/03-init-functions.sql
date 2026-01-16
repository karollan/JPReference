-- JLPT Reference Database Initialization Script
-- This script runs when the PostgreSQL container is first created

-- ============================================
-- SEARCH FUNCTIONS FOR DICTIONARY API
-- ============================================

SET search_path TO jlpt, public;

-- ============================================
-- KANJI HELPER FUNCTIONS
-- ============================================

CREATE OR REPLACE FUNCTION jlpt.kanji_build_kunyomi(p_kanji_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT COALESCE(
    json_agg(
        json_build_object(
            'id', kr.id,
            'type', kr.type,
            'value', kr.value,
            'status', kr.status,
            'onType', kr.on_type
        ) ORDER BY kr.id
    ),
    '[]'::json
)
FROM jlpt.kanji_reading kr
WHERE kr.kanji_id = p_kanji_id AND kr.type = 'ja_kun';
$$;

CREATE OR REPLACE FUNCTION jlpt.kanji_build_onyomi(p_kanji_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT COALESCE(
    json_agg(
        json_build_object(
            'id', kr.id,
            'type', kr.type,
            'value', kr.value,
            'status', kr.status,
            'onType', kr.on_type
        ) ORDER BY kr.id
    ),
    '[]'::json
)
FROM jlpt.kanji_reading kr
WHERE kr.kanji_id = p_kanji_id AND kr.type = 'ja_on';
$$;

CREATE OR REPLACE FUNCTION jlpt.kanji_build_meanings(p_kanji_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT COALESCE(
    json_agg(
        json_build_object(
            'id', km.id,
            'language', km.lang,
            'meaning', km.value
        ) ORDER BY km.id
    ),
    '[]'::json
)
FROM jlpt.kanji_meaning km
WHERE km.kanji_id = p_kanji_id;
$$;

CREATE OR REPLACE FUNCTION jlpt.kanji_build_radicals(p_kanji_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT COALESCE(
    json_agg(
        json_build_object(
            'id', kr.id,
            'literal', r.literal
        ) ORDER BY kr.id
    ),
    '[]'::json
)
FROM jlpt.kanji_radical kr
JOIN jlpt.radical r ON kr.radical_id = r.id
WHERE kr.kanji_id = p_kanji_id;
$$;

-- ============================================
-- NO-PATTERNS VARIANT
-- ============================================

CREATE OR REPLACE FUNCTION jlpt.search_kanji_ranked_no_patterns(
    jlpt_min INT,
    jlpt_max INT,
    grades_min INT,
    grades_max INT,
    stroke_min INT,
    stroke_max INT,
    freq_min INT,
    freq_max INT,
    langs TEXT[],
    page_size INT DEFAULT 20,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    out_kanji_id UUID,
    out_literal VARCHAR(10),
    out_grade INT,
    out_stroke_count INT,
    out_frequency INT,
    out_jlpt_level INT,
    out_match_quality INT,
    out_match_location INT,
    out_matched_text_length INT,
    out_all_readings TEXT[],
    out_all_meanings TEXT[],
    out_kunyomi JSON,
    out_onyomi JSON,
    out_meanings JSON,
    out_radicals JSON,
    out_total_count BIGINT
)
LANGUAGE sql
STABLE
AS $$
WITH
filtered AS (
    SELECT k.id, k.literal, k.grade, k.stroke_count, k.frequency, k.jlpt_level_new
    FROM jlpt.kanji k
    WHERE (jlpt_min <= 0 OR k.jlpt_level_new >= jlpt_min)
      AND (jlpt_max <= 0 OR k.jlpt_level_new <= jlpt_max)
      AND (grades_min <= 0 OR k.grade >= grades_min)
      AND (grades_max <= 0 OR k.grade <= grades_max)
      AND (stroke_min <= 0 OR k.stroke_count >= stroke_min)
      AND (stroke_max <= 0 OR k.stroke_count <= stroke_max)
      AND (freq_min <= 0 OR k.frequency >= freq_min)
      AND (freq_max <= 0 OR k.frequency <= freq_max)
      AND (langs IS NULL OR array_length(langs, 1) IS NULL OR EXISTS (
          SELECT 1 FROM jlpt.kanji_meaning km WHERE km.kanji_id = k.id AND km.lang = ANY(langs)
      ))
),
paginated AS (
    SELECT f.*,
           COUNT(*) OVER () AS total_count
    FROM filtered f
    ORDER BY f.frequency NULLS LAST, f.jlpt_level_new NULLS LAST, f.grade NULLS LAST, f.id
    LIMIT page_size OFFSET page_offset
)
SELECT
    p.id AS out_kanji_id,
    p.literal AS out_literal,
    p.grade AS out_grade,
    p.stroke_count AS out_stroke_count,
    p.frequency AS out_frequency,
    p.jlpt_level_new AS out_jlpt_level,
    0 AS out_match_quality,
    0 AS out_match_location,
    NULL::INT AS out_matched_text_length,
    ARRAY(SELECT kr.value::TEXT FROM jlpt.kanji_reading kr WHERE kr.kanji_id = p.id) AS out_all_readings,
    ARRAY(SELECT km.value::TEXT FROM jlpt.kanji_meaning km WHERE km.kanji_id = p.id) AS out_all_meanings,
    jlpt.kanji_build_kunyomi(p.id) AS out_kunyomi,
    jlpt.kanji_build_onyomi(p.id) AS out_onyomi,
    jlpt.kanji_build_meanings(p.id) AS out_meanings,
    jlpt.kanji_build_radicals(p.id) AS out_radicals,
    p.total_count AS out_total_count
FROM paginated p;
$$;

CREATE OR REPLACE FUNCTION jlpt.search_kanji_ranked(
    patterns TEXT[],
    token_variant_counts INT[],
    combined_patterns TEXT[],
    exact_terms TEXT[],
    has_user_wildcard BOOLEAN,
    jlpt_min INT,
    jlpt_max INT,
    grades_min INT,
    grades_max INT,
    stroke_min INT,
    stroke_max INT,
    freq_min INT,
    freq_max INT,
    langs TEXT[],
    page_size INT DEFAULT 20,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    out_kanji_id UUID,
    out_literal VARCHAR(10),
    out_grade INT,
    out_stroke_count INT,
    out_frequency INT,
    out_jlpt_level INT,
    out_match_quality INT,
    out_match_location INT,
    out_matched_text_length INT,
    out_all_readings TEXT[],
    out_all_meanings TEXT[],
    out_kunyomi JSON,
    out_onyomi JSON,
    out_meanings JSON,
    out_radicals JSON,
    out_total_count BIGINT
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    IF patterns IS NULL OR array_length(patterns, 1) IS NULL THEN
        RETURN QUERY
        SELECT * FROM jlpt.search_kanji_ranked_no_patterns(
            jlpt_min, jlpt_max, grades_min, grades_max,
            stroke_min, stroke_max, freq_min, freq_max,
            langs, page_size, page_offset
        );
        RETURN;
    END IF;

    RETURN QUERY
    WITH
    /* ---------------------------------------------------------
       1. Token expansion
    --------------------------------------------------------- */
    token_ranges AS (
        SELECT
            ordinality AS token_index,
            SUM(vc) OVER (ORDER BY ordinality) - vc + 1 AS start_idx,
            SUM(vc) OVER (ORDER BY ordinality) AS end_idx
        FROM unnest(token_variant_counts) WITH ORDINALITY t(vc, ordinality)
    ),
    expanded_patterns AS (
        SELECT p.pattern, r.token_index
        FROM unnest(patterns) WITH ORDINALITY p(pattern, ordinality)
        JOIN token_ranges r
          ON p.ordinality BETWEEN r.start_idx AND r.end_idx
    ),

    /* ---------------------------------------------------------
       2. SINGLE PASS text matching (ALL flags computed here)
    --------------------------------------------------------- */
    matched_texts AS (
        SELECT
            k.id AS kanji_id,
            'literal'::text AS src,
            k.literal AS text,
            length(k.literal) AS match_len,
            k.literal = ANY(exact_terms) AS is_exact,
            (NOT has_user_wildcard AND k.literal LIKE ANY(
                SELECT et || '%' FROM unnest(exact_terms) et
            )) AS is_prefix
        FROM jlpt.kanji k
        WHERE k.literal LIKE ANY(patterns)

        UNION ALL

        SELECT
            kr.kanji_id,
            'reading',
            kr.value,
            length(kr.value),
            kr.value = ANY(exact_terms),
            (NOT has_user_wildcard AND kr.value LIKE ANY(
                SELECT et || '%' FROM unnest(exact_terms) et
            ))
        FROM jlpt.kanji_reading kr
        WHERE kr.value LIKE ANY(patterns)

        UNION ALL

        SELECT
            km.kanji_id,
            'meaning',
            km.value,
            length(km.value),
            lower(km.value) = ANY(SELECT lower(unnest(exact_terms))),
            (NOT has_user_wildcard AND km.value ILIKE ANY(
                SELECT et || '%' FROM unnest(exact_terms) et
            ))
        FROM jlpt.kanji_meaning km
        WHERE km.value ILIKE ANY(patterns)
          AND (langs IS NULL OR km.lang = ANY(langs))
    ),

    /* ---------------------------------------------------------
       3. Token AND logic
    --------------------------------------------------------- */
    token_matches AS (
        SELECT
            mt.kanji_id,
            ep.token_index,
            COUNT(*) > 0 AS token_matched
        FROM expanded_patterns ep
        JOIN matched_texts mt
          ON mt.text LIKE ep.pattern OR mt.text ILIKE ep.pattern
        GROUP BY mt.kanji_id, ep.token_index
    ),
    and_matched AS (
        SELECT kanji_id
        FROM token_matches
        GROUP BY kanji_id
        HAVING bool_and(token_matched)
    ),

    /* ---------------------------------------------------------
       4. Combined phrase matches
    --------------------------------------------------------- */
    combined_matched AS (
        SELECT DISTINCT kanji_id
        FROM (
            SELECT id AS kanji_id FROM jlpt.kanji
            WHERE combined_patterns IS NOT NULL
              AND literal LIKE ANY(combined_patterns)
            UNION ALL
            SELECT kanji_id FROM jlpt.kanji_reading
            WHERE combined_patterns IS NOT NULL
              AND value LIKE ANY(combined_patterns)
            UNION ALL
            SELECT kanji_id FROM jlpt.kanji_meaning
            WHERE combined_patterns IS NOT NULL
              AND value ILIKE ANY(combined_patterns)
              AND (langs IS NULL OR lang = ANY(langs))
        ) x
    ),

    matched_k AS (
        SELECT kanji_id FROM and_matched
        UNION
        SELECT kanji_id FROM combined_matched
    ),

    /* ---------------------------------------------------------
       5. Aggregate EVERYTHING (no EXISTS remain)
    --------------------------------------------------------- */
    match_agg AS (
        SELECT
            mk.kanji_id,
            bool_or(src = 'literal') AS has_literal,
            bool_or(src = 'reading') AS has_reading,
            bool_or(src = 'meaning') AS has_meaning,
            bool_or(is_exact) AS has_exact,
            bool_or(is_prefix) AS has_prefix,
            MIN(match_len) AS shortest_match
        FROM matched_k mk
        JOIN matched_texts mt USING (kanji_id)
        GROUP BY mk.kanji_id
    ),

    /* ---------------------------------------------------------
       6. Rank + filter
    --------------------------------------------------------- */
    filtered AS (
        SELECT
            k.id,
            k.literal,
            k.grade,
            k.stroke_count,
            k.frequency,
            k.jlpt_level_new,
            ma.shortest_match,
            (
                CASE
                    WHEN ma.has_exact THEN 1000
                    WHEN ma.has_prefix THEN 500
                    WHEN has_user_wildcard THEN 100
                    ELSE 200
                END
            ) AS match_quality,
            (
                (CASE WHEN has_literal THEN 16 ELSE 0 END) |
                (CASE WHEN has_reading THEN 32 ELSE 0 END) |
                (CASE WHEN has_meaning THEN 64 ELSE 0 END)
            ) AS match_location
        FROM match_agg ma
        JOIN jlpt.kanji k ON k.id = ma.kanji_id
        WHERE (jlpt_min <= 0 OR k.jlpt_level_new >= jlpt_min)
          AND (jlpt_max <= 0 OR k.jlpt_level_new <= jlpt_max)
          AND (grades_min <= 0 OR k.grade >= grades_min)
          AND (grades_max <= 0 OR k.grade <= grades_max)
          AND (stroke_min <= 0 OR k.stroke_count >= stroke_min)
          AND (stroke_max <= 0 OR k.stroke_count <= stroke_max)
          AND (freq_min <= 0 OR k.frequency >= freq_min)
          AND (freq_max <= 0 OR k.frequency <= freq_max)
    ),

    /* ---------------------------------------------------------
       7. Pagination + COUNT(*) OVER ()
    --------------------------------------------------------- */
    paginated AS (
        SELECT f.*,
               COUNT(*) OVER () AS total_count
        FROM filtered f
        ORDER BY f.match_quality DESC, f.frequency NULLS LAST, f.jlpt_level_new NULLS LAST, f.grade NULLS LAST, f.id
        LIMIT page_size OFFSET page_offset
    )

    /* ---------------------------------------------------------
       8. Final projection
    --------------------------------------------------------- */
    SELECT
        p.id as out_kanji_id,
        p.literal as out_literal,
        p.grade as out_grade,
        p.stroke_count as out_stroke_count,
        p.frequency as out_frequency,
        p.jlpt_level_new as out_jlpt_level,
        p.match_quality as out_match_quality,
        p.match_location as out_match_location,
        p.shortest_match as out_matched_text_length,
        ARRAY(SELECT kr.value::TEXT FROM jlpt.kanji_reading kr WHERE kr.kanji_id = p.id) as out_all_readings,
        ARRAY(SELECT km.value::TEXT FROM jlpt.kanji_meaning km WHERE km.kanji_id = p.id) as out_all_meanings,
        jlpt.kanji_build_kunyomi(p.id) as out_kunyomi,
        jlpt.kanji_build_onyomi(p.id) as out_onyomi,
        jlpt.kanji_build_meanings(p.id) as out_meanings,
        jlpt.kanji_build_radicals(p.id) as out_radicals,
        p.total_count as out_total_count
    FROM paginated p;
END;
$$;

-- ============================================
-- VOCABULARY HELPER FUNCTIONS
-- ============================================

CREATE OR REPLACE FUNCTION jlpt.vocab_build_primary_kanji(p_vocab_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT row_to_json(x)
FROM (
    SELECT
        vk.text,
        vk.is_common AS "isCommon",
        COALESCE((
            SELECT json_agg(
                json_build_object(
                    'code', t.code,
                    'description', t.description,
                    'category', t.category
                )
            )
            FROM jlpt.vocabulary_kanji_tag vkt
            JOIN jlpt.tag t ON vkt.tag_code = t.code
            WHERE vkt.vocabulary_kanji_id = vk.id
        ), '[]'::json) AS tags
    FROM jlpt.vocabulary_kanji vk
    WHERE vk.vocabulary_id = p_vocab_id
      AND vk.is_primary = true
    LIMIT 1
) x;
$$;

CREATE OR REPLACE FUNCTION jlpt.vocab_build_primary_kana(p_vocab_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT row_to_json(x)
FROM (
    SELECT
        vk.text,
        vk.is_common AS "isCommon",
        vk.applies_to_kanji AS "appliesToKanji",
        COALESCE((
            SELECT json_agg(
                json_build_object(
                    'code', t.code,
                    'description', t.description,
                    'category', t.category
                )
            )
            FROM jlpt.vocabulary_kana_tag vkt
            JOIN jlpt.tag t ON vkt.tag_code = t.code
            WHERE vkt.vocabulary_kana_id = vk.id
        ), '[]'::json) AS tags
    FROM jlpt.vocabulary_kana vk
    WHERE vk.vocabulary_id = p_vocab_id
      AND vk.is_primary = true
    LIMIT 1
) x;
$$;

CREATE OR REPLACE FUNCTION jlpt.vocab_build_other_kanji(p_vocab_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json)
FROM (
    SELECT
        vk.text,
        vk.is_common AS "isCommon",
        COALESCE((
            SELECT json_agg(
                json_build_object(
                    'code', t.code,
                    'description', t.description,
                    'category', t.category
                )
            )
            FROM jlpt.vocabulary_kanji_tag vkt
            JOIN jlpt.tag t ON vkt.tag_code = t.code
            WHERE vkt.vocabulary_kanji_id = vk.id
        ), '[]'::json) AS tags
    FROM jlpt.vocabulary_kanji vk
    WHERE vk.vocabulary_id = p_vocab_id
      AND vk.is_primary = false
) x;
$$;

CREATE OR REPLACE FUNCTION jlpt.vocab_build_other_kana(p_vocab_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json)
FROM (
    SELECT
        vk.text,
        vk.is_common AS "isCommon",
        vk.applies_to_kanji AS "appliesToKanji",
        COALESCE((
            SELECT json_agg(
                json_build_object(
                    'code', t.code,
                    'description', t.description,
                    'category', t.category
                )
            )
            FROM jlpt.vocabulary_kana_tag vkt
            JOIN jlpt.tag t ON vkt.tag_code = t.code
            WHERE vkt.vocabulary_kana_id = vk.id
        ), '[]'::json) AS tags
    FROM jlpt.vocabulary_kana vk
    WHERE vk.vocabulary_id = p_vocab_id
      AND vk.is_primary = false
) x;
$$;

CREATE OR REPLACE FUNCTION jlpt.vocab_build_senses(p_vocab_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
WITH senses_with_langs AS (
    SELECT DISTINCT
        vs.id AS sense_id,
        vsg.lang
    FROM jlpt.vocabulary_sense vs
    JOIN jlpt.vocabulary_sense_gloss vsg ON vsg.sense_id = vs.id
    WHERE vs.vocabulary_id = p_vocab_id
),
ranked_senses AS (
    SELECT
        sense_id,
        lang,
        ROW_NUMBER() OVER (PARTITION BY lang ORDER BY sense_id) AS sense_rank
    FROM senses_with_langs
),
top_senses AS (
    SELECT DISTINCT sense_id
    FROM ranked_senses
    WHERE sense_rank <= 3
)
SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json)
FROM (
    SELECT
        vs.applies_to_kanji AS "appliesToKanji",
        vs.applies_to_kana AS "appliesToKana",
        vs.info,
        COALESCE((
            SELECT json_agg(
                json_build_object(
                    'code', t.code,
                    'description', t.description,
                    'category', t.category,
                    'type', vst.tag_type
                )
            )
            FROM jlpt.vocabulary_sense_tag vst
            JOIN jlpt.tag t ON vst.tag_code = t.code
            WHERE vst.sense_id = vs.id
        ), '[]'::json) AS tags,
        COALESCE((
            SELECT json_agg(
                json_build_object(
                    'language', vsg.lang,
                    'text', vsg.text
                )
                ORDER BY vsg.id
            )
            FROM jlpt.vocabulary_sense_gloss vsg
            WHERE vsg.sense_id = vs.id
        ), '[]'::json) AS glosses
    FROM top_senses ts
    JOIN jlpt.vocabulary_sense vs ON vs.id = ts.sense_id
    ORDER BY vs.id
) x;
$$;

CREATE OR REPLACE FUNCTION jlpt.vocab_build_furigana(p_vocab_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT COALESCE(
    json_agg(
        json_build_object(
            'text', vf.text,
            'reading', vf.reading,
            'furigana', vf.furigana
        )
    ),
    '[]'::json
)
FROM jlpt.vocabulary_furigana vf
WHERE vf.vocabulary_id = p_vocab_id;
$$;

CREATE OR REPLACE FUNCTION jlpt.search_vocabulary_ranked_no_patterns(
    jlpt_min INT,
    jlpt_max INT,
    common_only BOOLEAN,
    langs TEXT[],
    page_size INT DEFAULT 20,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    out_vocab_id UUID,
    out_dict_id VARCHAR(30),
    out_jlpt_level INT,
    out_is_common BOOLEAN,
    out_match_quality INT,
    out_match_location INT,
    out_matched_text_length INT,
    out_sense_count INT,
    out_all_kana_texts TEXT[],
    out_all_kanji_texts TEXT[],
    out_first_sense_glosses TEXT[],
    out_all_glosses TEXT[],
    out_primary_kanji JSON,
    out_primary_kana JSON,
    out_other_kanji_forms JSON,
    out_other_kana_forms JSON,
    out_senses JSON,
    out_furigana JSON,
    out_slug TEXT,
    out_total_count BIGINT
)
LANGUAGE sql
STABLE
AS $$
WITH
/* ---------------------------------------------------------
   1. Precompute is_common ONCE
--------------------------------------------------------- */
common_flags AS (
    SELECT v.id AS vocabulary_id,
           EXISTS (SELECT 1 FROM jlpt.vocabulary_kana k WHERE k.vocabulary_id = v.id AND k.is_common) AS is_common
    FROM jlpt.vocabulary v
),
/* ---------------------------------------------------------
   2. Filter vocabulary
--------------------------------------------------------- */
filtered AS (
    SELECT
        v.id,
        v.jmdict_id,
        v.jlpt_level_new,
        v.slug,
        cf.is_common
    FROM jlpt.vocabulary v
    JOIN common_flags cf ON cf.vocabulary_id = v.id
    WHERE
        (jlpt_min <= 0 OR v.jlpt_level_new >= jlpt_min)
        AND (jlpt_max <= 0 OR v.jlpt_level_new <= jlpt_max)
        AND (NOT common_only OR cf.is_common)
),

/* ---------------------------------------------------------
   3. Pagination + total count
--------------------------------------------------------- */
paginated AS (
    SELECT
        f.*,
        COUNT(*) OVER () AS total_count
    FROM filtered f
    ORDER BY
        f.is_common DESC,
        COALESCE(f.jlpt_level_new, 99),
        f.id
    LIMIT page_size OFFSET page_offset
)

/* ---------------------------------------------------------
   4. Final projection (same shape as ranked search)
--------------------------------------------------------- */
SELECT
    p.id AS out_vocab_id,
    p.jmdict_id AS out_dict_id,
    p.jlpt_level_new AS out_jlpt_level,
    p.is_common AS out_is_common,
    0 AS out_match_quality,
    0 AS out_match_location,
    NULL::INT AS out_matched_text_length,
    (SELECT COUNT(*)::INT
     FROM jlpt.vocabulary_sense vs
     WHERE vs.vocabulary_id = p.id) AS out_sense_count,
    ARRAY(
        SELECT text
        FROM jlpt.vocabulary_kana vk
        WHERE vk.vocabulary_id = p.id
        ORDER BY vk.is_primary DESC, vk.is_common DESC
    ) AS out_all_kana_texts,
    ARRAY(
        SELECT text
        FROM jlpt.vocabulary_kanji vkj
        WHERE vkj.vocabulary_id = p.id
        ORDER BY vkj.is_primary DESC, vkj.is_common DESC
    ) AS out_all_kanji_texts,
    ARRAY(
        SELECT vsg.text
        FROM jlpt.vocabulary_sense vs
        JOIN jlpt.vocabulary_sense_gloss vsg ON vsg.sense_id = vs.id
        WHERE vs.vocabulary_id = p.id
        ORDER BY vs.id
        LIMIT 5
    ) AS out_first_sense_glosses,
    ARRAY(
        SELECT vsg.text
        FROM jlpt.vocabulary_sense vs
        JOIN jlpt.vocabulary_sense_gloss vsg ON vsg.sense_id = vs.id
        WHERE vs.vocabulary_id = p.id
    ) AS out_all_glosses,
    jlpt.vocab_build_primary_kanji(p.id) AS out_primary_kanji,
    jlpt.vocab_build_primary_kana(p.id) AS out_primary_kana,
    jlpt.vocab_build_other_kanji(p.id) AS out_other_kanji_forms,
    jlpt.vocab_build_other_kana(p.id) AS out_other_kana_forms,
    jlpt.vocab_build_senses(p.id) AS out_senses,
    jlpt.vocab_build_furigana(p.id) AS out_furigana,
    p.slug AS out_slug,
    p.total_count AS out_total_count
FROM paginated p;
$$;

CREATE OR REPLACE FUNCTION jlpt.search_vocabulary_ranked(
    patterns TEXT[],
    token_variant_counts INT[],
    combined_patterns TEXT[],
    exact_terms TEXT[],
    has_user_wildcard BOOLEAN,
    jlpt_min INT,
    jlpt_max INT,
    common_only BOOLEAN,
    filter_tags TEXT[],
    langs TEXT[],
    page_size INT DEFAULT 20,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    out_vocab_id UUID,
    out_dict_id VARCHAR(30),
    out_jlpt_level INT,
    out_is_common BOOLEAN,
    out_match_quality INT,
    out_match_location INT,
    out_matched_text_length INT,
    out_sense_count INT,
    out_all_kana_texts TEXT[],
    out_all_kanji_texts TEXT[],
    out_first_sense_glosses TEXT[],
    out_all_glosses TEXT[],
    out_primary_kanji JSON,
    out_primary_kana JSON,
    out_other_kanji_forms JSON,
    out_other_kana_forms JSON,
    out_senses JSON,
    out_furigana JSON,
    out_slug TEXT,
    out_total_count BIGINT
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    IF patterns IS NULL OR array_length(patterns, 1) IS NULL THEN
        RETURN QUERY
        SELECT * FROM jlpt.search_vocabulary_ranked_no_patterns(
            jlpt_min, jlpt_max, common_only, langs, page_size, page_offset
        );
        RETURN;
    END IF;

    RETURN QUERY
    WITH
    /* ---------------------------------------------------------
       1. Token expansion
    --------------------------------------------------------- */
    token_ranges AS (
        SELECT
            ordinality AS token_index,
            SUM(vc) OVER (ORDER BY ordinality) - vc + 1 AS start_idx,
            SUM(vc) OVER (ORDER BY ordinality) AS end_idx
        FROM unnest(token_variant_counts) WITH ORDINALITY t(vc, ordinality)
    ),
    expanded_patterns AS (
        SELECT p.pattern, r.token_index
        FROM unnest(patterns) WITH ORDINALITY p(pattern, ordinality)
        JOIN token_ranges r
          ON p.ordinality BETWEEN r.start_idx AND r.end_idx
    ),

    /* ---------------------------------------------------------
       1b. Pre-compute tag-matched IDs (faster than 3 EXISTS)
    --------------------------------------------------------- */
    tag_matched_ids AS (
        SELECT DISTINCT vk.vocabulary_id
        FROM jlpt.vocabulary_kana vk
        JOIN jlpt.vocabulary_kana_tag vkt ON vk.id = vkt.vocabulary_kana_id
        WHERE filter_tags IS NOT NULL 
          AND array_length(filter_tags, 1) IS NOT NULL
          AND vkt.tag_code = ANY(filter_tags)
        
        UNION
        
        SELECT DISTINCT vk.vocabulary_id
        FROM jlpt.vocabulary_kanji vk
        JOIN jlpt.vocabulary_kanji_tag vkt ON vk.id = vkt.vocabulary_kanji_id
        WHERE filter_tags IS NOT NULL 
          AND array_length(filter_tags, 1) IS NOT NULL
          AND vkt.tag_code = ANY(filter_tags)
        
        UNION
        
        SELECT DISTINCT vs.vocabulary_id
        FROM jlpt.vocabulary_sense vs
        JOIN jlpt.vocabulary_sense_tag vst ON vs.id = vst.sense_id
        WHERE filter_tags IS NOT NULL 
          AND array_length(filter_tags, 1) IS NOT NULL
          AND vst.tag_code = ANY(filter_tags)
    ),

    /* ---------------------------------------------------------
       2. SINGLE PASS text matching (ALL flags computed here)
    --------------------------------------------------------- */
    matched_texts AS (
        SELECT
            vk.vocabulary_id,
            'kana'::text AS src,
            vk.text,
            length(vk.text) AS match_len,
            vk.is_common,
            vk.text = ANY(exact_terms) AS is_exact,
            (NOT has_user_wildcard AND vk.text LIKE ANY(
			    SELECT et || '%' FROM unnest(exact_terms) et
			)) AS is_prefix
        FROM jlpt.vocabulary_kana vk
        WHERE vk.text LIKE ANY(patterns)

        UNION ALL

        SELECT
            vj.vocabulary_id,
            'kanji',
            vj.text,
            length(vj.text),
            vj.is_common,
            vj.text = ANY(exact_terms),
			(NOT has_user_wildcard AND vj.text LIKE ANY(
				SELECT et || '%' FROM unnest(exact_terms) et
			))
        FROM jlpt.vocabulary_kanji vj
        WHERE vj.text LIKE ANY(patterns)

        UNION ALL

        SELECT
            vs.vocabulary_id,
            'gloss',
            vsg.text,
            length(vsg.text),
            FALSE,
            lower(vsg.text) = ANY(SELECT lower(unnest(exact_terms))),
            FALSE
        FROM jlpt.vocabulary_sense vs
        JOIN jlpt.vocabulary_sense_gloss vsg ON vsg.sense_id = vs.id
        WHERE vsg.text ILIKE ANY(patterns)
          AND (langs IS NULL OR vsg.lang = ANY(langs))
    ),

    /* ---------------------------------------------------------
       3. Token AND logic
    --------------------------------------------------------- */
    token_matches AS (
        SELECT
            mt.vocabulary_id,
            ep.token_index,
            COUNT(*) > 0 AS token_matched
        FROM expanded_patterns ep
        JOIN matched_texts mt
          ON mt.text LIKE ep.pattern
        GROUP BY mt.vocabulary_id, ep.token_index
    ),
    and_matched AS (
        SELECT vocabulary_id
        FROM token_matches
        GROUP BY vocabulary_id
        HAVING bool_and(token_matched)
    ),

    /* ---------------------------------------------------------
       4. Combined phrase matches
    --------------------------------------------------------- */
    combined_matched AS (
        SELECT DISTINCT vocabulary_id
        FROM (
            SELECT vocabulary_id FROM jlpt.vocabulary_kana
            WHERE combined_patterns IS NOT NULL
              AND text LIKE ANY(combined_patterns)
            UNION ALL
            SELECT vocabulary_id FROM jlpt.vocabulary_kanji
            WHERE combined_patterns IS NOT NULL
              AND text LIKE ANY(combined_patterns)
            UNION ALL
            SELECT vs.vocabulary_id
            FROM jlpt.vocabulary_sense vs
            JOIN jlpt.vocabulary_sense_gloss vsg ON vsg.sense_id = vs.id
            WHERE combined_patterns IS NOT NULL
              AND vsg.text ILIKE ANY(combined_patterns)
              AND (langs IS NULL OR vsg.lang = ANY(langs))
        ) x
    ),

    matched_vocab AS (
        SELECT vocabulary_id FROM and_matched
        UNION
        SELECT vocabulary_id FROM combined_matched
    ),

    /* ---------------------------------------------------------
       5. Aggregate EVERYTHING (no EXISTS remain)
    --------------------------------------------------------- */
    match_agg AS (
        SELECT
            mv.vocabulary_id,
            bool_or(src = 'kana') AS has_kana,
            bool_or(src = 'kanji') AS has_kanji,
            bool_or(src = 'gloss') AS has_gloss,
            bool_or(mt.is_common) AS is_common,
            bool_or(is_exact) AS has_exact,
            bool_or(is_prefix) AS has_prefix,
            MIN(match_len) AS shortest_match
        FROM matched_vocab mv
        JOIN matched_texts mt USING (vocabulary_id)
        GROUP BY mv.vocabulary_id
    ),

    /* ---------------------------------------------------------
       6. Rank + filter
    --------------------------------------------------------- */
    filtered AS (
        SELECT
            v.id,
            v.jmdict_id,
            v.jlpt_level_new,
            v.slug,
            ma.is_common,
            ma.shortest_match,
            (
                CASE
                    WHEN ma.has_exact THEN 1000
                    WHEN ma.has_prefix THEN 500
                    WHEN has_user_wildcard THEN 100
                    ELSE 200
                END
            ) AS match_quality,
            (
                (CASE WHEN has_kana THEN 1 ELSE 0 END) |
                (CASE WHEN has_kanji THEN 2 ELSE 0 END) |
                (CASE WHEN has_gloss THEN 4 ELSE 0 END)
            ) AS match_location
        FROM match_agg ma
        JOIN jlpt.vocabulary v ON v.id = ma.vocabulary_id
        WHERE
            (jlpt_min <= 0 OR v.jlpt_level_new >= jlpt_min)
            AND (jlpt_max <= 0 OR v.jlpt_level_new <= jlpt_max)
            AND (NOT common_only OR ma.is_common)
            AND (filter_tags IS NULL OR array_length(filter_tags, 1) IS NULL
                OR v.id IN (SELECT vocabulary_id FROM tag_matched_ids))
    ),

    /* ---------------------------------------------------------
       7. Pagination + COUNT(*) OVER ()
    --------------------------------------------------------- */
    paginated AS (
        SELECT f.*,
               COUNT(*) OVER () AS total_count
        FROM filtered f
        ORDER BY f.match_quality DESC, f.is_common DESC, COALESCE(f.jlpt_level_new, 99), f.id
        LIMIT page_size OFFSET page_offset
    )

    /* ---------------------------------------------------------
       8. Final projection
    --------------------------------------------------------- */
    SELECT
        p.id AS out_vocab_id,
        p.jmdict_id AS out_dict_id,
        p.jlpt_level_new AS out_jlpt_level,
        p.is_common AS out_is_common,
        p.match_quality AS out_match_quality,
        p.match_location AS out_match_location,
        p.shortest_match AS out_matched_text_length,
        (SELECT COUNT(*)::INT FROM jlpt.vocabulary_sense vs WHERE vs.vocabulary_id = p.id) AS out_sense_count,
        ARRAY(SELECT text FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_primary DESC, vk.is_common DESC) AS out_all_kana_texts,
        ARRAY(SELECT text FROM jlpt.vocabulary_kanji vkj WHERE vkj.vocabulary_id = p.id ORDER BY vkj.is_primary DESC, vkj.is_common DESC) AS out_all_kanji_texts,
        ARRAY(SELECT vsg.text FROM jlpt.vocabulary_sense vs JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
              WHERE vs.vocabulary_id = p.id ORDER BY vs.id LIMIT 5) AS out_first_sense_glosses,
        ARRAY(SELECT vsg.text FROM jlpt.vocabulary_sense vs JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
              WHERE vs.vocabulary_id = p.id) AS out_all_glosses,
        jlpt.vocab_build_primary_kanji(p.id) AS out_primary_kanji,
        jlpt.vocab_build_primary_kana(p.id) AS out_primary_kana,
        jlpt.vocab_build_other_kanji(p.id) AS out_other_kanji_forms,
        jlpt.vocab_build_other_kana(p.id) AS out_other_kana_forms,
        jlpt.vocab_build_senses(p.id) AS out_senses,
        jlpt.vocab_build_furigana(p.id) AS out_furigana,
        p.slug AS out_slug,
        p.total_count AS out_total_count
    FROM paginated p;
END;
$$;

-- ============================================
-- HELPER FUNCTIONS
-- ============================================

CREATE OR REPLACE FUNCTION jlpt.pn_build_primary_kanji(p_pn_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT row_to_json(x)
FROM (
    SELECT
        pk.text,
        COALESCE((
            SELECT json_agg(
                json_build_object('code', t.code, 'description', t.description, 'category', t.category)
            )
            FROM jlpt.proper_noun_kanji_tag pkt
            JOIN jlpt.tag t ON pkt.tag_code = t.code
            WHERE pkt.proper_noun_kanji_id = pk.id
        ), '[]'::json) AS tags
    FROM jlpt.proper_noun_kanji pk
    WHERE pk.proper_noun_id = p_pn_id
      AND pk.is_primary = true
    LIMIT 1
) x;
$$;

CREATE OR REPLACE FUNCTION jlpt.pn_build_primary_kana(p_pn_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT row_to_json(x)
FROM (
    SELECT
        pk.text,
        pk.applies_to_kanji AS "appliesToKanji",
        COALESCE((
            SELECT json_agg(
                json_build_object('code', t.code, 'description', t.description, 'category', t.category)
            )
            FROM jlpt.proper_noun_kana_tag pkt
            JOIN jlpt.tag t ON pkt.tag_code = t.code
            WHERE pkt.proper_noun_kana_id = pk.id
        ), '[]'::json) AS tags
    FROM jlpt.proper_noun_kana pk
    WHERE pk.proper_noun_id = p_pn_id
      AND pk.is_primary = true
    LIMIT 1
) x;
$$;

CREATE OR REPLACE FUNCTION jlpt.pn_build_other_kanji(p_pn_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json)
FROM (
    SELECT
        pk.text,
        COALESCE((
            SELECT json_agg(
                json_build_object('code', t.code, 'description', t.description, 'category', t.category)
            )
            FROM jlpt.proper_noun_kanji_tag pkt
            JOIN jlpt.tag t ON pkt.tag_code = t.code
            WHERE pkt.proper_noun_kanji_id = pk.id
        ), '[]'::json) AS tags
    FROM jlpt.proper_noun_kanji pk
    WHERE pk.proper_noun_id = p_pn_id
      AND pk.is_primary = false
) x;
$$;

CREATE OR REPLACE FUNCTION jlpt.pn_build_other_kana(p_pn_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json)
FROM (
    SELECT
        pk.text,
        pk.applies_to_kanji AS "appliesToKanji",
        COALESCE((
            SELECT json_agg(
                json_build_object('code', t.code, 'description', t.description, 'category', t.category)
            )
            FROM jlpt.proper_noun_kana_tag pkt
            JOIN jlpt.tag t ON pkt.tag_code = t.code
            WHERE pkt.proper_noun_kana_id = pk.id
        ), '[]'::json) AS tags
    FROM jlpt.proper_noun_kana pk
    WHERE pk.proper_noun_id = p_pn_id
      AND pk.is_primary = false
) x;
$$;

CREATE OR REPLACE FUNCTION jlpt.pn_build_translations(p_pn_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
WITH translations_with_langs AS (
    SELECT DISTINCT
        pt.id AS translation_id,
        ptt.lang
    FROM jlpt.proper_noun_translation pt
    JOIN jlpt.proper_noun_translation_text ptt ON ptt.translation_id = pt.id
    WHERE pt.proper_noun_id = p_pn_id
),
ranked_translations AS (
    SELECT
        translation_id,
        lang,
        ROW_NUMBER() OVER (PARTITION BY lang ORDER BY translation_id) AS translation_rank
    FROM translations_with_langs
),
top_translations AS (
    SELECT DISTINCT translation_id
    FROM ranked_translations
    WHERE translation_rank <= 3
)
SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json)
FROM (
    SELECT
        COALESCE((
            SELECT json_agg(
                json_build_object('code', t.code, 'description', t.description, 'category', t.category)
            )
            FROM jlpt.proper_noun_translation_type ptt
            JOIN jlpt.tag t ON ptt.tag_code = t.code
            WHERE ptt.translation_id = pt.id
        ), '[]'::json) AS types,
        COALESCE((
            SELECT json_agg(
                json_build_object('language', ptt.lang, 'text', ptt.text)
                ORDER BY ptt.id
            )
            FROM jlpt.proper_noun_translation_text ptt
            WHERE ptt.translation_id = pt.id
        ), '[]'::json) AS translations
    FROM top_translations tt
    JOIN jlpt.proper_noun_translation pt ON pt.id = tt.translation_id
    ORDER BY pt.id
) x;
$$;

CREATE OR REPLACE FUNCTION jlpt.pn_build_furigana(p_pn_id UUID)
RETURNS JSON
LANGUAGE sql
STABLE
AS $$
SELECT COALESCE(
    json_agg(
        json_build_object(
            'text', pnf.text,
            'reading', pnf.reading,
            'furigana', pnf.furigana
        )
    ),
    '[]'::json
)
FROM jlpt.proper_noun_furigana pnf
WHERE pnf.proper_noun_id = p_pn_id;
$$;

-- ============================================
-- NO-PATTERNS VARIANT
-- ============================================

CREATE OR REPLACE FUNCTION jlpt.search_proper_noun_ranked_no_patterns(
    filter_tags TEXT[],
    langs TEXT[],
    page_size INT DEFAULT 20,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    out_pn_id UUID,
    out_dict_id VARCHAR(30),
    out_match_quality INT,
    out_match_location INT,
    out_matched_text_length INT,
    out_all_kanji_texts TEXT[],
    out_all_kana_texts TEXT[],
    out_all_translation_texts TEXT[],
    out_primary_kanji JSON,
    out_primary_kana JSON,
    out_other_kanji_forms JSON,
    out_other_kana_forms JSON,
    out_translations JSON,
    out_furigana JSON,
    out_slug TEXT,
    out_total_count BIGINT
)
LANGUAGE sql
STABLE
AS $$
WITH
/* ---------------------------------------------------------
   1. Pre-compute proper_noun_ids matching filter_tags
--------------------------------------------------------- */
tag_matched_ids AS (
    SELECT DISTINCT pk.proper_noun_id
    FROM jlpt.proper_noun_kanji pk
    JOIN jlpt.proper_noun_kanji_tag pkt ON pk.id = pkt.proper_noun_kanji_id
    WHERE filter_tags IS NOT NULL 
      AND array_length(filter_tags, 1) IS NOT NULL
      AND pkt.tag_code = ANY(filter_tags)
    
    UNION
    
    SELECT DISTINCT pk.proper_noun_id
    FROM jlpt.proper_noun_kana pk
    JOIN jlpt.proper_noun_kana_tag pkt ON pk.id = pkt.proper_noun_kana_id
    WHERE filter_tags IS NOT NULL 
      AND array_length(filter_tags, 1) IS NOT NULL
      AND pkt.tag_code = ANY(filter_tags)
    
    UNION
    
    SELECT DISTINCT pt.proper_noun_id
    FROM jlpt.proper_noun_translation pt
    JOIN jlpt.proper_noun_translation_type ptt ON pt.id = ptt.translation_id
    WHERE filter_tags IS NOT NULL 
      AND array_length(filter_tags, 1) IS NOT NULL
      AND ptt.tag_code = ANY(filter_tags)
),

/* ---------------------------------------------------------
   2. Filter proper nouns (use JOIN for tag filter, EXISTS for langs)
--------------------------------------------------------- */
filtered AS (
    SELECT p.id, p.jmnedict_id, p.slug
    FROM jlpt.proper_noun p
    WHERE 
        -- Tag filter: either no tags specified OR id in tag_matched_ids
        (filter_tags IS NULL OR array_length(filter_tags, 1) IS NULL 
            OR p.id IN (SELECT proper_noun_id FROM tag_matched_ids))
        -- Language filter
        AND (langs IS NULL OR array_length(langs, 1) IS NULL OR EXISTS (
            SELECT 1 FROM jlpt.proper_noun_translation pt
            JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id
            WHERE pt.proper_noun_id = p.id AND ptt.lang = ANY(langs)
        ))
),

/* ---------------------------------------------------------
   3. Pagination + total count
--------------------------------------------------------- */
paginated AS (
    SELECT f.*,
           COUNT(*) OVER () AS total_count
    FROM filtered f
    ORDER BY f.id
    LIMIT page_size OFFSET page_offset
)

/* ---------------------------------------------------------
   4. Final projection
--------------------------------------------------------- */
SELECT
    p.id AS out_pn_id,
    p.jmnedict_id AS out_dict_id,
    0 AS out_match_quality,
    0 AS out_match_location,
    NULL::INT AS out_matched_text_length,
    ARRAY(SELECT pk.text FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = p.id ORDER BY pk.is_primary DESC) AS out_all_kanji_texts,
    ARRAY(SELECT pk.text FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = p.id ORDER BY pk.is_primary DESC) AS out_all_kana_texts,
    ARRAY(SELECT ptt.text FROM jlpt.proper_noun_translation pt JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id WHERE pt.proper_noun_id = p.id) AS out_all_translation_texts,
    jlpt.pn_build_primary_kanji(p.id) AS out_primary_kanji,
    jlpt.pn_build_primary_kana(p.id) AS out_primary_kana,
    jlpt.pn_build_other_kanji(p.id) AS out_other_kanji_forms,
    jlpt.pn_build_other_kana(p.id) AS out_other_kana_forms,
    jlpt.pn_build_translations(p.id) AS out_translations,
    jlpt.pn_build_furigana(p.id) AS out_furigana,
    p.slug AS out_slug,
    p.total_count AS out_total_count
FROM paginated p;
$$;

-- Proper noun search function
CREATE OR REPLACE FUNCTION jlpt.search_proper_noun_ranked(
    patterns TEXT[],
    token_variant_counts INT[],
    combined_patterns TEXT[],
    exact_terms TEXT[],
    has_user_wildcard BOOLEAN,
    filter_tags TEXT[],
    langs TEXT[],
    page_size INT DEFAULT 20,
    page_offset INT DEFAULT 0
)
RETURNS TABLE (
    out_pn_id UUID,
    out_dict_id VARCHAR(30),
    out_match_quality INT,
    out_match_location INT,
    out_matched_text_length INT,
    out_all_kanji_texts TEXT[],
    out_all_kana_texts TEXT[],
    out_all_translation_texts TEXT[],
    out_primary_kanji JSON,
    out_primary_kana JSON,
    out_other_kanji_forms JSON,
    out_other_kana_forms JSON,
    out_translations JSON,
    out_furigana JSON,
    out_slug TEXT,
    out_total_count BIGINT
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    IF patterns IS NULL OR array_length(patterns, 1) IS NULL THEN
        RETURN QUERY
        SELECT * FROM jlpt.search_proper_noun_ranked_no_patterns(
            filter_tags, langs, page_size, page_offset
        );
        RETURN;
    END IF;

    RETURN QUERY
    WITH
    /* ---------------------------------------------------------
       1. Token expansion
    --------------------------------------------------------- */
    token_ranges AS (
        SELECT
            ordinality AS token_index,
            SUM(vc) OVER (ORDER BY ordinality) - vc + 1 AS start_idx,
            SUM(vc) OVER (ORDER BY ordinality) AS end_idx
        FROM unnest(token_variant_counts) WITH ORDINALITY t(vc, ordinality)
    ),
    expanded_patterns AS (
        SELECT p.pattern, r.token_index
        FROM unnest(patterns) WITH ORDINALITY p(pattern, ordinality)
        JOIN token_ranges r
          ON p.ordinality BETWEEN r.start_idx AND r.end_idx
    ),

    /* ---------------------------------------------------------
       1b. Pre-compute tag-matched IDs
    --------------------------------------------------------- */
    tag_matched_ids AS (
        SELECT DISTINCT pk.proper_noun_id
        FROM jlpt.proper_noun_kanji pk
        JOIN jlpt.proper_noun_kanji_tag pkt ON pk.id = pkt.proper_noun_kanji_id
        WHERE filter_tags IS NOT NULL 
          AND array_length(filter_tags, 1) IS NOT NULL
          AND pkt.tag_code = ANY(filter_tags)
        
        UNION
        
        SELECT DISTINCT pk.proper_noun_id
        FROM jlpt.proper_noun_kana pk
        JOIN jlpt.proper_noun_kana_tag pkt ON pk.id = pkt.proper_noun_kana_id
        WHERE filter_tags IS NOT NULL 
          AND array_length(filter_tags, 1) IS NOT NULL
          AND pkt.tag_code = ANY(filter_tags)
        
        UNION
        
        SELECT DISTINCT pt.proper_noun_id
        FROM jlpt.proper_noun_translation pt
        JOIN jlpt.proper_noun_translation_type ptt ON pt.id = ptt.translation_id
        WHERE filter_tags IS NOT NULL 
          AND array_length(filter_tags, 1) IS NOT NULL
          AND ptt.tag_code = ANY(filter_tags)
    ),

    /* ---------------------------------------------------------
       2. SINGLE PASS text matching (ALL flags computed here)
    --------------------------------------------------------- */
    matched_texts AS (
        SELECT
            pk.proper_noun_id,
            'kana'::text AS src,
            pk.text,
            length(pk.text) AS match_len,
            pk.text = ANY(exact_terms) AS is_exact,
            (NOT has_user_wildcard AND pk.text LIKE ANY(
                SELECT et || '%' FROM unnest(exact_terms) et
            )) AS is_prefix
        FROM jlpt.proper_noun_kana pk
        WHERE pk.text LIKE ANY(patterns)

        UNION ALL

        SELECT
            pk.proper_noun_id,
            'kanji',
            pk.text,
            length(pk.text),
            pk.text = ANY(exact_terms),
            (NOT has_user_wildcard AND pk.text LIKE ANY(
                SELECT et || '%' FROM unnest(exact_terms) et
            ))
        FROM jlpt.proper_noun_kanji pk
        WHERE pk.text LIKE ANY(patterns)

        UNION ALL

        SELECT
            pt.proper_noun_id,
            'translation',
            ptt.text,
            length(ptt.text),
            lower(ptt.text) = ANY(SELECT lower(unnest(exact_terms))),
            FALSE
        FROM jlpt.proper_noun_translation pt
        JOIN jlpt.proper_noun_translation_text ptt ON ptt.translation_id = pt.id
        WHERE ptt.text ILIKE ANY(patterns)
          AND (langs IS NULL OR ptt.lang = ANY(langs))
    ),

    /* ---------------------------------------------------------
       3. Token AND logic
    --------------------------------------------------------- */
    token_matches AS (
        SELECT
            mt.proper_noun_id,
            ep.token_index,
            COUNT(*) > 0 AS token_matched
        FROM expanded_patterns ep
        JOIN matched_texts mt
          ON mt.text LIKE ep.pattern
        GROUP BY mt.proper_noun_id, ep.token_index
    ),
    and_matched AS (
        SELECT proper_noun_id
        FROM token_matches
        GROUP BY proper_noun_id
        HAVING bool_and(token_matched)
    ),

    /* ---------------------------------------------------------
       4. Combined phrase matches
    --------------------------------------------------------- */
    combined_matched AS (
        SELECT DISTINCT proper_noun_id
        FROM (
            SELECT proper_noun_id FROM jlpt.proper_noun_kana
            WHERE combined_patterns IS NOT NULL
              AND text LIKE ANY(combined_patterns)
            UNION ALL
            SELECT proper_noun_id FROM jlpt.proper_noun_kanji
            WHERE combined_patterns IS NOT NULL
              AND text LIKE ANY(combined_patterns)
            UNION ALL
            SELECT pt.proper_noun_id
            FROM jlpt.proper_noun_translation pt
            JOIN jlpt.proper_noun_translation_text ptt ON ptt.translation_id = pt.id
            WHERE combined_patterns IS NOT NULL
              AND ptt.text ILIKE ANY(combined_patterns)
              AND (langs IS NULL OR ptt.lang = ANY(langs))
        ) x
    ),

    matched_pn AS (
        SELECT proper_noun_id FROM and_matched
        UNION
        SELECT proper_noun_id FROM combined_matched
    ),

    /* ---------------------------------------------------------
       5. Aggregate EVERYTHING
    --------------------------------------------------------- */
    match_agg AS (
        SELECT
            mp.proper_noun_id,
            bool_or(src = 'kana') AS has_kana,
            bool_or(src = 'kanji') AS has_kanji,
            bool_or(src = 'translation') AS has_translation,
            bool_or(is_exact) AS has_exact,
            bool_or(is_prefix) AS has_prefix,
            MIN(match_len) AS shortest_match
        FROM matched_pn mp
        JOIN matched_texts mt USING (proper_noun_id)
        GROUP BY mp.proper_noun_id
    ),

    /* ---------------------------------------------------------
       6. Rank + filter
    --------------------------------------------------------- */
    filtered AS (
        SELECT
            p.id,
            p.jmnedict_id,
            p.slug,
            ma.shortest_match,
            (
                CASE
                    WHEN ma.has_exact THEN 1000
                    WHEN ma.has_prefix THEN 500
                    WHEN has_user_wildcard THEN 100
                    ELSE 200
                END
            ) AS match_quality,
            (
                (CASE WHEN has_kana THEN 1 ELSE 0 END) |
                (CASE WHEN has_kanji THEN 2 ELSE 0 END) |
                (CASE WHEN has_translation THEN 128 ELSE 0 END)
            ) AS match_location
        FROM match_agg ma
        JOIN jlpt.proper_noun p ON p.id = ma.proper_noun_id
        WHERE (filter_tags IS NULL OR array_length(filter_tags, 1) IS NULL
            OR p.id IN (SELECT proper_noun_id FROM tag_matched_ids))
    ),

    /* ---------------------------------------------------------
       7. Pagination + COUNT(*) OVER ()
    --------------------------------------------------------- */
    paginated AS (
        SELECT f.*,
               COUNT(*) OVER () AS total_count
        FROM filtered f
        ORDER BY f.match_quality DESC, f.id
        LIMIT page_size OFFSET page_offset
    )

    /* ---------------------------------------------------------
       8. Final projection
    --------------------------------------------------------- */
    SELECT
        p.id AS out_pn_id,
        p.jmnedict_id AS out_dict_id,
        p.match_quality AS out_match_quality,
        p.match_location AS out_match_location,
        p.shortest_match AS out_matched_text_length,
        ARRAY(SELECT pk.text FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = p.id ORDER BY pk.is_primary DESC) AS out_all_kanji_texts,
        ARRAY(SELECT pk.text FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = p.id ORDER BY pk.is_primary DESC) AS out_all_kana_texts,
        ARRAY(SELECT ptt.text FROM jlpt.proper_noun_translation pt JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id WHERE pt.proper_noun_id = p.id) AS out_all_translation_texts,
        jlpt.pn_build_primary_kanji(p.id) AS out_primary_kanji,
        jlpt.pn_build_primary_kana(p.id) AS out_primary_kana,
        jlpt.pn_build_other_kanji(p.id) AS out_other_kanji_forms,
        jlpt.pn_build_other_kana(p.id) AS out_other_kana_forms,
        jlpt.pn_build_translations(p.id) AS out_translations,
        jlpt.pn_build_furigana(p.id) AS out_furigana,
        p.slug AS out_slug,
        p.total_count AS out_total_count
    FROM paginated p;
END;
$$;

-- Helper function to get kanji summaries for a set of IDs
CREATE OR REPLACE FUNCTION jlpt.get_kanji_summaries(kanji_ids UUID[])
RETURNS TABLE (
    id UUID,
    literal VARCHAR(10),
    grade INT,
    stroke_count INT,
    frequency INT,
    jlpt_level INT,
    kunyomi_readings JSON,
    onyomi_readings JSON,
    meanings JSON,
    radicals JSON
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        k.id, k.literal, k.grade, k.stroke_count, k.frequency, k.jlpt_level_new as jlpt_level,
        (SELECT COALESCE(json_agg(json_build_object('type', kr.type, 'value', kr.value, 'status', kr.status, 'onType', kr.on_type) ORDER BY kr.id), '[]'::json)
         FROM jlpt.kanji_reading kr WHERE kr.kanji_id = k.id AND kr.type = 'ja_kun') AS kunyomi_readings,
        (SELECT COALESCE(json_agg(json_build_object('type', kr.type, 'value', kr.value, 'status', kr.status, 'onType', kr.on_type) ORDER BY kr.id), '[]'::json)
         FROM jlpt.kanji_reading kr WHERE kr.kanji_id = k.id AND kr.type = 'ja_on') AS onyomi_readings,
        (SELECT COALESCE(json_agg(json_build_object('language', km.lang, 'meaning', km.value) ORDER BY km.id), '[]'::json)
         FROM jlpt.kanji_meaning km WHERE km.kanji_id = k.id) AS meanings,
        (SELECT COALESCE(json_agg(json_build_object('id', r.id, 'literal', r.literal) ORDER BY kr.id), '[]'::json)
         FROM jlpt.kanji_radical kr JOIN jlpt.radical r ON kr.radical_id = r.id WHERE kr.kanji_id = k.id) AS radicals
    FROM jlpt.kanji k
    WHERE k.id = ANY(kanji_ids)
    ORDER BY k.frequency NULLS LAST, k.jlpt_level_new NULLS LAST, k.grade NULLS LAST, k.id;
END;
$$ LANGUAGE plpgsql STABLE;

-- Get kanjis that contain a specific radical literal
CREATE OR REPLACE FUNCTION jlpt.get_kanjis_for_radical_literal(radical_literal TEXT)
RETURNS TABLE (
    id UUID,
    literal VARCHAR(10),
    grade INT,
    stroke_count INT,
    frequency INT,
    jlpt_level INT,
    kunyomi_readings JSON,
    onyomi_readings JSON,
    meanings JSON,
    radicals JSON
) AS $$
BEGIN
    RETURN QUERY
    SELECT * FROM jlpt.get_kanji_summaries(
        ARRAY(SELECT kr.kanji_id FROM jlpt.kanji_radical kr JOIN jlpt.radical r ON kr.radical_id = r.id WHERE r.literal = radical_literal)
    );
END;
$$ LANGUAGE plpgsql STABLE;

-- Find vocabulary ID by term with prioritized fallback chain
-- Replaces 6 sequential C# queries with a single SQL call
CREATE OR REPLACE FUNCTION jlpt.find_vocabulary_id_by_term(
    search_term TEXT,
    search_reading TEXT DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
    result_id UUID;
BEGIN
    -- Priority 1: If reading provided, find vocabulary with matching primary kanji AND primary kana
    IF search_reading IS NOT NULL THEN
        SELECT vk.vocabulary_id INTO result_id
        FROM jlpt.vocabulary_kanji vk
        WHERE vk.text = search_term AND vk.is_primary = true
          AND EXISTS (
              SELECT 1 FROM jlpt.vocabulary_kana vka 
              WHERE vka.vocabulary_id = vk.vocabulary_id 
                AND vka.text = search_reading 
                AND vka.is_primary = true
          )
        LIMIT 1;
        
        IF result_id IS NOT NULL THEN RETURN result_id; END IF;
    END IF;
    
    -- Priority 2: Primary kanji only
    SELECT vocabulary_id INTO result_id
    FROM jlpt.vocabulary_kanji
    WHERE text = search_term AND is_primary = true
    LIMIT 1;
    IF result_id IS NOT NULL THEN RETURN result_id; END IF;
    
    -- Priority 3: KANA-ONLY vocabulary (no primary kanji) where term is PRIMARY kana
    SELECT vka.vocabulary_id INTO result_id
    FROM jlpt.vocabulary_kana vka
    WHERE vka.text = search_term AND vka.is_primary = true
      AND NOT EXISTS (
          SELECT 1 FROM jlpt.vocabulary_kanji vk 
          WHERE vk.vocabulary_id = vka.vocabulary_id AND vk.is_primary = true
      )
    LIMIT 1;
    IF result_id IS NOT NULL THEN RETURN result_id; END IF;
    
    -- Priority 4: Primary kana (including entries with kanji)
    SELECT vocabulary_id INTO result_id
    FROM jlpt.vocabulary_kana
    WHERE text = search_term AND is_primary = true
    LIMIT 1;
    IF result_id IS NOT NULL THEN RETURN result_id; END IF;
    
    -- Priority 5: Any vocabulary with term as kanji
    SELECT vocabulary_id INTO result_id
    FROM jlpt.vocabulary_kanji
    WHERE text = search_term
    LIMIT 1;
    IF result_id IS NOT NULL THEN RETURN result_id; END IF;
    
    -- Priority 6: Any vocabulary with term as kana
    SELECT vocabulary_id INTO result_id
    FROM jlpt.vocabulary_kana
    WHERE text = search_term
    LIMIT 1;
    
    RETURN result_id;
END;
$$ LANGUAGE plpgsql STABLE;

-- Find proper noun ID by term with prioritized fallback chain
CREATE OR REPLACE FUNCTION jlpt.find_proper_noun_id_by_term(
    search_term TEXT,
    search_reading TEXT DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
    result_id UUID;
BEGIN
    -- Priority 1: If reading provided, find with matching primary kanji AND primary kana
    IF search_reading IS NOT NULL THEN
        SELECT pk.proper_noun_id INTO result_id
        FROM jlpt.proper_noun_kanji pk
        WHERE pk.text = search_term AND pk.is_primary = true
          AND EXISTS (
              SELECT 1 FROM jlpt.proper_noun_kana pka 
              WHERE pka.proper_noun_id = pk.proper_noun_id 
                AND pka.text = search_reading 
                AND pka.is_primary = true
          )
        LIMIT 1;
        
        IF result_id IS NOT NULL THEN RETURN result_id; END IF;
    END IF;
    
    -- Priority 2: Primary kanji only
    SELECT proper_noun_id INTO result_id
    FROM jlpt.proper_noun_kanji
    WHERE text = search_term AND is_primary = true
    LIMIT 1;
    IF result_id IS NOT NULL THEN RETURN result_id; END IF;
    
    -- Priority 3: KANA-ONLY (no primary kanji) where term is PRIMARY kana
    SELECT pka.proper_noun_id INTO result_id
    FROM jlpt.proper_noun_kana pka
    WHERE pka.text = search_term AND pka.is_primary = true
      AND NOT EXISTS (
          SELECT 1 FROM jlpt.proper_noun_kanji pk 
          WHERE pk.proper_noun_id = pka.proper_noun_id AND pk.is_primary = true
      )
    LIMIT 1;
    IF result_id IS NOT NULL THEN RETURN result_id; END IF;
    
    -- Priority 4: Primary kana (including entries with kanji)
    SELECT proper_noun_id INTO result_id
    FROM jlpt.proper_noun_kana
    WHERE text = search_term AND is_primary = true
    LIMIT 1;
    IF result_id IS NOT NULL THEN RETURN result_id; END IF;
    
    -- Priority 5: Any proper noun with term as kanji
    SELECT proper_noun_id INTO result_id
    FROM jlpt.proper_noun_kanji
    WHERE text = search_term
    LIMIT 1;
    IF result_id IS NOT NULL THEN RETURN result_id; END IF;
    
    -- Priority 6: Any proper noun with term as kana
    SELECT proper_noun_id INTO result_id
    FROM jlpt.proper_noun_kana
    WHERE text = search_term
    LIMIT 1;
    
    RETURN result_id;
END;
$$ LANGUAGE plpgsql STABLE;

-- Batch lookup: returns all kanji for an array of radical literals
CREATE OR REPLACE FUNCTION jlpt.get_kanjis_for_multiple_literals(literals TEXT[])
RETURNS TABLE (
    literal_key TEXT,
    id UUID,
    literal VARCHAR(10),
    grade INT,
    stroke_count INT,
    frequency INT,
    jlpt_level INT,
    kunyomi_readings JSON,
    onyomi_readings JSON,
    meanings JSON,
    radicals JSON
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        r.literal as literal_key,
        ks.*
    FROM unnest(literals) as lit(l)
    JOIN jlpt.radical r ON r.literal = lit.l
    JOIN jlpt.kanji_radical kr ON kr.radical_id = r.id
    JOIN LATERAL (
        SELECT * FROM jlpt.get_kanji_summaries(ARRAY[kr.kanji_id])
    ) ks ON true;
END;
$$ LANGUAGE plpgsql STABLE;