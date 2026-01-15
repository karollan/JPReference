-- JLPT Reference Database Initialization Script
-- This script runs when the PostgreSQL container is first created

-- ============================================
-- SEARCH FUNCTIONS FOR DICTIONARY API
-- ============================================

SET search_path TO jlpt, public;

CREATE OR REPLACE FUNCTION jlpt.search_kanji_ranked(
    patterns TEXT[],                    -- Individual patterns - each must match somewhere in entry
    token_variant_counts INT[],         -- Number of variants per token (for OR within, AND across)
    combined_patterns TEXT[],           -- Combined phrase patterns (for OR against whole token logic)
    exact_terms TEXT[],                 -- Exact terms for ranking (without wildcards)
    has_user_wildcard BOOLEAN,          -- Whether user used wildcards
    jlpt_min INT,                       -- Filter: JLPT levels (0 = no min)
    jlpt_max INT,                       -- Filter: JLPT levels (0 = no max)
    grades_min INT,                     -- Filter: Grades (0 = no min)
    grades_max INT,                     -- Filter: Grades (0 = no max)
    stroke_min INT,                     -- Filter: Min stroke count (0 = no min)
    stroke_max INT,                     -- Filter: Max stroke count (0 = no max)
    freq_min INT,                       -- Filter: Min frequency (0 = no min)
    freq_max INT,                       -- Filter: Max frequency (0 = no max)
    langs TEXT[],                       -- Filter: Languages
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
            WHERE (jlpt_min <= 0 OR k.jlpt_level_new >= jlpt_min)
              AND (jlpt_max <= 0 OR k.jlpt_level_new <= jlpt_max)
              AND (grades_min <= 0 OR k.grade >= grades_min)
              AND (grades_max <= 0 OR k.grade <= grades_max)
              AND (stroke_min <= 0 OR k.stroke_count >= stroke_min)
              AND (stroke_max <= 0 OR k.stroke_count <= stroke_max)
              AND (freq_min <= 0 OR k.frequency >= freq_min)
              AND (freq_max <= 0 OR k.frequency <= freq_max)
              AND (langs IS NULL OR array_length(langs, 1) IS NULL OR EXISTS (SELECT 1 FROM jlpt.kanji_meaning km WHERE km.kanji_id = k.id AND km.lang = ANY(langs)))
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
            ARRAY(SELECT km.value::TEXT FROM jlpt.kanji_meaning km WHERE km.kanji_id = p.id) AS all_meanings,
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

    -- Main search with patterns - each pattern must match somewhere in the entry
    RETURN QUERY
    WITH 
    -- Step 1: Find candidate kanji IDs matching ANY pattern (uses indexes)
    candidate_ids AS (
        SELECT k.id as kanji_id FROM jlpt.kanji k WHERE k.literal LIKE ANY(patterns)
        UNION
        SELECT DISTINCT kr.kanji_id FROM jlpt.kanji_reading kr WHERE kr.value LIKE ANY(patterns)
        UNION
        SELECT DISTINCT km.kanji_id 
        FROM jlpt.kanji_meaning km 
        WHERE km.value ILIKE ANY(patterns)
            AND (langs IS NULL OR array_length(langs, 1) IS NULL OR km.lang = ANY(langs))
    ),
    -- Step 2: Filter candidates to those matching ALL patterns
    -- We need to handle variant groups: (P1 OR P2) AND (P3) AND (P4 OR P5 OR P6)
    -- OR if a combined pattern matches a single field (phrase search)
    matching_kanji_ids AS (
        SELECT c.kanji_id
        FROM candidate_ids c
        JOIN jlpt.kanji k ON k.id = c.kanji_id
        WHERE (
            -- Option A: Check that ALL tokens have at least one matching variant
            (SELECT bool_and(token_match)
            FROM (
                SELECT 
                    -- For each token, check if ANY of its variants match
                    bool_or(
                        k.literal LIKE pattern
                        OR EXISTS (SELECT 1 FROM jlpt.kanji_reading kr WHERE kr.kanji_id = k.id AND kr.value LIKE pattern)
                        OR EXISTS (
                            SELECT 1 FROM jlpt.kanji_meaning km 
                            WHERE km.kanji_id = k.id 
                            AND km.value ILIKE pattern
                            AND (langs IS NULL OR array_length(langs, 1) IS NULL OR km.lang = ANY(langs))
                        )
                    ) as token_match
                FROM (
                    -- Unnest patterns with their token index
                    SELECT 
                        p.pattern,
                        t.token_index
                    FROM (
                        -- Generate ranges for each token based on variant counts
                        -- token_index | start_idx | end_idx
                        SELECT 
                            ordinality as token_index,
                            sum(vc) OVER (ORDER BY ordinality) - vc + 1 as start_idx,
                            sum(vc) OVER (ORDER BY ordinality) as end_idx
                        FROM unnest(token_variant_counts) WITH ORDINALITY as t(vc, ordinality)
                    ) t
                    JOIN unnest(patterns) WITH ORDINALITY as p(pattern, ordinality) 
                    ON p.ordinality BETWEEN t.start_idx AND t.end_idx
                ) p_with_token
                GROUP BY token_index
            ) token_checks)

            OR

            -- Option B: Combined pattern matches a single field (Phrase Match)
            (combined_patterns IS NOT NULL AND EXISTS (
                SELECT 1
                WHERE 
                    k.literal LIKE ANY(combined_patterns)
                    OR EXISTS (SELECT 1 FROM jlpt.kanji_reading kr WHERE kr.kanji_id = k.id AND kr.value LIKE ANY(combined_patterns))
                    OR EXISTS (
                        SELECT 1 FROM jlpt.kanji_meaning km 
                        WHERE km.kanji_id = k.id 
                        AND km.value ILIKE ANY(combined_patterns)
                        AND (langs IS NULL OR array_length(langs, 1) IS NULL OR km.lang = ANY(langs))
                    )
            ))
        )
    ),
    -- For matched kanji, compute match quality and location
    match_details AS (
        SELECT 
            mk.kanji_id,
            -- Best quality across all patterns
            MAX(
                CASE 
                    -- Check literal exact match
                    WHEN EXISTS (
                        SELECT 1 FROM jlpt.kanji kx 
                        WHERE kx.id = mk.kanji_id 
                        AND kx.literal = ANY(exact_terms)
                    ) THEN 1000
                    -- Check reading exact match
                    WHEN EXISTS (
                        SELECT 1 FROM jlpt.kanji_reading kr 
                        WHERE kr.kanji_id = mk.kanji_id 
                        AND kr.value = ANY(exact_terms)
                    ) THEN 1000
                    -- Check meaning exact match
                    WHEN EXISTS (
                        SELECT 1 FROM jlpt.kanji_meaning km 
                        WHERE km.kanji_id = mk.kanji_id 
                        AND lower(km.value) = ANY(SELECT lower(unnest(exact_terms)))
                    ) THEN 1000
                    -- Check prefix matches
                    WHEN NOT has_user_wildcard AND EXISTS (
                        SELECT 1 FROM jlpt.kanji kx, unnest(exact_terms) et
                        WHERE kx.id = mk.kanji_id AND kx.literal LIKE et || '%'
                    ) THEN 500
                    WHEN NOT has_user_wildcard AND EXISTS (
                        SELECT 1 FROM jlpt.kanji_reading kr, unnest(exact_terms) et
                        WHERE kr.kanji_id = mk.kanji_id AND kr.value LIKE et || '%'
                    ) THEN 500
                    WHEN NOT has_user_wildcard AND EXISTS (
                        SELECT 1 FROM jlpt.kanji_meaning km, unnest(exact_terms) et
                        WHERE km.kanji_id = mk.kanji_id AND km.value ILIKE et || '%'
                    ) THEN 500
                    -- Wildcard or contains
                    WHEN has_user_wildcard THEN 100
                    ELSE 200
                END
            ) as best_quality,
            -- Compute match location bitmask
            (
                CASE WHEN EXISTS (SELECT 1 FROM jlpt.kanji kx WHERE kx.id = mk.kanji_id AND kx.literal LIKE ANY(patterns)) THEN 16 ELSE 0 END
                | CASE WHEN EXISTS (SELECT 1 FROM jlpt.kanji_reading kr WHERE kr.kanji_id = mk.kanji_id AND kr.value LIKE ANY(patterns)) THEN 32 ELSE 0 END
                | CASE WHEN EXISTS (
                    SELECT 1 FROM jlpt.kanji_meaning km 
                    WHERE km.kanji_id = mk.kanji_id AND km.value ILIKE ANY(patterns)
                ) THEN 64 ELSE 0 END
            )::INT as locations,
            -- Shortest matched text length
            COALESCE(
                (SELECT length(kx.literal) FROM jlpt.kanji kx WHERE kx.id = mk.kanji_id AND kx.literal LIKE ANY(patterns)),
                (SELECT MIN(length(kr.value)) FROM jlpt.kanji_reading kr WHERE kr.kanji_id = mk.kanji_id AND kr.value LIKE ANY(patterns)),
                (SELECT MIN(length(km.value)) FROM jlpt.kanji_meaning km WHERE km.kanji_id = mk.kanji_id AND km.value ILIKE ANY(patterns)),
                0
            ) as shortest_match
        FROM matching_kanji_ids mk
        GROUP BY mk.kanji_id
    ),
    -- Apply language filter
    language_filtered AS (
        SELECT md.*
        FROM match_details md
        WHERE (langs IS NULL OR array_length(langs, 1) IS NULL)
           OR (md.locations & 64 = 64)  -- Has meaning match (already language-filtered)
           OR (md.locations & (16 | 32)) != 0  -- Has literal or reading match
    ),
    -- Apply other filters
    filtered AS (
        SELECT 
            k.id, k.literal, k.grade, k.stroke_count, k.frequency, k.jlpt_level_new,
            lf.best_quality, lf.locations, lf.shortest_match
        FROM language_filtered lf
        JOIN jlpt.kanji k ON k.id = lf.kanji_id
        WHERE (jlpt_min <= 0 OR k.jlpt_level_new >= jlpt_min)
          AND (jlpt_max <= 0 OR k.jlpt_level_new <= jlpt_max)
          AND (grades_min <= 0 OR k.grade >= grades_min)
          AND (grades_max <= 0 OR k.grade <= grades_max)
          AND (stroke_min <= 0 OR k.stroke_count >= stroke_min)
          AND (stroke_max <= 0 OR k.stroke_count <= stroke_max)
          AND (freq_min <= 0 OR k.frequency >= freq_min)
          AND (freq_max <= 0 OR k.frequency <= freq_max)
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
        ARRAY(SELECT km.value::TEXT FROM jlpt.kanji_meaning km WHERE km.kanji_id = p.id) AS all_meanings,
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
    patterns TEXT[],                    -- Flat array of all patterns
    token_variant_counts INT[],         -- Number of variants per token (for OR within, AND across)
    combined_patterns TEXT[],           -- Combined phrase patterns (for OR against whole token logic)
    exact_terms TEXT[],                 -- Exact terms for ranking (without wildcards)
    has_user_wildcard BOOLEAN,          -- Whether user used wildcards
    jlpt_min INT,                       -- Filter: JLPT levels (0 = no min)
    jlpt_max INT,                       -- Filter: JLPT levels (0 = no max)
    common_only BOOLEAN,                -- Filter: common words only
    filter_tags TEXT[],                 -- Filter: general tags
    langs TEXT[],                       -- Filter: Languages
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
    furigana JSON,
    slug TEXT,                          -- URL-friendly identifier: "term" or "term(reading)"
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
            WHERE (jlpt_min <= 0 OR v.jlpt_level_new >= jlpt_min)
              AND (jlpt_max <= 0 OR v.jlpt_level_new <= jlpt_max)
              AND (NOT common_only OR EXISTS (SELECT 1 FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = v.id AND vk.is_common))
              AND (langs IS NULL OR array_length(langs, 1) IS NULL OR EXISTS (
                  SELECT 1 FROM jlpt.vocabulary_sense vs
                  JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id 
                  WHERE vs.vocabulary_id = v.id AND vsg.lang = ANY(langs)
              ))
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
            ARRAY(SELECT vk.text FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_primary DESC, vk.is_common DESC),
            ARRAY(SELECT vk.text FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_primary DESC, vk.is_common DESC),
            ARRAY[]::TEXT[],
            ARRAY[]::TEXT[],
            (SELECT row_to_json(x) FROM (
                SELECT vk.text, vk.is_common as "isCommon", 
                    COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                        FROM jlpt.vocabulary_kanji_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kanji_id = vk.id), '[]'::json) as tags
                FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = p.id AND vk.is_primary = true LIMIT 1
            ) x),
            (SELECT row_to_json(x) FROM (
                SELECT vk.text, vk.is_common as "isCommon", vk.applies_to_kanji as "appliesToKanji",
                    COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                        FROM jlpt.vocabulary_kana_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kana_id = vk.id), '[]'::json) as tags
                FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id AND vk.is_primary = true LIMIT 1
            ) x),
            (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
                SELECT vk.text, vk.is_common as "isCommon",
                    COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                        FROM jlpt.vocabulary_kanji_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kanji_id = vk.id), '[]'::json) as tags
                FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = p.id AND vk.is_primary = false
            ) x),
            (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
                SELECT vk.text, vk.is_common as "isCommon", vk.applies_to_kanji as "appliesToKanji",
                    COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                        FROM jlpt.vocabulary_kana_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kana_id = vk.id), '[]'::json) as tags
                FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id AND vk.is_primary = false
            ) x),
            (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
                WITH senses_with_langs AS (
                    SELECT DISTINCT vs.id as sense_id, vsg.lang
                    FROM jlpt.vocabulary_sense vs
                    JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
                    WHERE vs.vocabulary_id = p.id
                ),
                ranked_senses AS (
                    SELECT 
                        sense_id,
                        lang,
                        ROW_NUMBER() OVER (PARTITION BY lang ORDER BY sense_id) as sense_rank
                    FROM senses_with_langs
                ),
                top_senses AS (
                    SELECT DISTINCT sense_id
                    FROM ranked_senses
                    WHERE sense_rank <= 3
                )
                SELECT vs.applies_to_kanji as "appliesToKanji", vs.applies_to_kana as "appliesToKana", vs.info,
                    COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category, 'type', vst.tag_type))
                        FROM jlpt.vocabulary_sense_tag vst JOIN jlpt.tag t ON vst.tag_code = t.code WHERE vst.sense_id = vs.id), '[]'::json) as tags,
                    COALESCE((SELECT json_agg(json_build_object('language', vsg.lang, 'text', vsg.text) ORDER BY vsg.id)
                        FROM jlpt.vocabulary_sense_gloss vsg WHERE vsg.sense_id = vs.id), '[]'::json) as glosses
                FROM top_senses ts
                JOIN jlpt.vocabulary_sense vs ON vs.id = ts.sense_id
                ORDER BY vs.id
            ) x),
            -- Compute slug
            (SELECT 
                CASE 
                    WHEN pk.text IS NOT NULL THEN
                        CASE WHEN (SELECT COUNT(*) FROM jlpt.vocabulary_kanji vk2 WHERE vk2.text = pk.text AND vk2.is_primary = true) = 1 
                            THEN pk.text
                            ELSE pk.text || '(' || COALESCE(pka.text, '') || ')'
                        END
                    WHEN pka.text IS NOT NULL THEN pka.text
                    ELSE NULL
                END
            FROM (SELECT text FROM jlpt.vocabulary_kanji WHERE vocabulary_id = p.id AND is_primary = true LIMIT 1) pk
            FULL OUTER JOIN (SELECT text FROM jlpt.vocabulary_kana WHERE vocabulary_id = p.id AND is_primary = true LIMIT 1) pka ON true),
            (SELECT cnt FROM counted)
        FROM paginated p;
        RETURN;
    END IF;

    -- Main search query with patterns - each pattern must match somewhere in the entry
    RETURN QUERY
    WITH 
    -- Step 1: Find candidate vocabulary IDs matching ANY pattern (uses indexes)
    candidate_ids AS (
        SELECT DISTINCT vk.vocabulary_id FROM jlpt.vocabulary_kana vk WHERE vk.text LIKE ANY(patterns)
        UNION
        SELECT DISTINCT vj.vocabulary_id FROM jlpt.vocabulary_kanji vj WHERE vj.text LIKE ANY(patterns)
        UNION
        SELECT DISTINCT vs.vocabulary_id 
        FROM jlpt.vocabulary_sense vs 
        JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
        WHERE vsg.text ILIKE ANY(patterns)
            AND (langs IS NULL OR array_length(langs, 1) IS NULL OR vsg.lang = ANY(langs))
    ),
    -- Step 2: Filter candidates to those matching ALL patterns
    -- We need to handle variant groups: (P1 OR P2) AND (P3) AND (P4 OR P5 OR P6)
    -- OR if a combined pattern matches a single field (phrase search)
    matching_vocab_ids AS (
        SELECT c.vocabulary_id
        FROM candidate_ids c
        WHERE (
            -- Option A: Check that ALL tokens have at least one matching variant (AND across tokens)
            (SELECT bool_and(token_match)
            FROM (
                SELECT 
                    -- For each token, check if ANY of its variants match
                    bool_or(
                        EXISTS (SELECT 1 FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = c.vocabulary_id AND vk.text LIKE pattern)
                        OR EXISTS (SELECT 1 FROM jlpt.vocabulary_kanji vj WHERE vj.vocabulary_id = c.vocabulary_id AND vj.text LIKE pattern)
                        OR EXISTS (
                            SELECT 1 FROM jlpt.vocabulary_sense vs 
                            JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
                            WHERE vs.vocabulary_id = c.vocabulary_id 
                            AND vsg.text ILIKE pattern
                            AND (langs IS NULL OR array_length(langs, 1) IS NULL OR vsg.lang = ANY(langs))
                        )
                    ) as token_match
                FROM (
                    -- Unnest patterns with their token index
                    SELECT 
                        p.pattern,
                        t.token_index
                    FROM (
                        -- Generate ranges for each token based on variant counts
                        -- token_index | start_idx | end_idx
                        SELECT 
                            ordinality as token_index,
                            sum(vc) OVER (ORDER BY ordinality) - vc + 1 as start_idx,
                            sum(vc) OVER (ORDER BY ordinality) as end_idx
                        FROM unnest(token_variant_counts) WITH ORDINALITY as t(vc, ordinality)
                    ) t
                    JOIN unnest(patterns) WITH ORDINALITY as p(pattern, ordinality) 
                    ON p.ordinality BETWEEN t.start_idx AND t.end_idx
                ) p_with_token
                GROUP BY token_index
            ) token_checks)
            
            OR
            
            -- Option B: Combined pattern matches a single field (Phrase Match)
            (combined_patterns IS NOT NULL AND EXISTS (
                SELECT 1 
                WHERE 
                    EXISTS (SELECT 1 FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = c.vocabulary_id AND vk.text LIKE ANY(combined_patterns))
                    OR EXISTS (SELECT 1 FROM jlpt.vocabulary_kanji vj WHERE vj.vocabulary_id = c.vocabulary_id AND vj.text LIKE ANY(combined_patterns))
                    OR EXISTS (
                        SELECT 1 FROM jlpt.vocabulary_sense vs 
                        JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
                        WHERE vs.vocabulary_id = c.vocabulary_id 
                        AND vsg.text ILIKE ANY(combined_patterns)
                        AND (langs IS NULL OR array_length(langs, 1) IS NULL OR vsg.lang = ANY(langs))
                    )
            ))
        )
    ),
    -- For matched vocabularies, compute match quality and location per pattern
    match_details AS (
        SELECT 
            mv.vocabulary_id,
            -- Best quality across all patterns
            MAX(
                CASE 
                    -- Check kana exact match
                    WHEN EXISTS (
                        SELECT 1 FROM jlpt.vocabulary_kana vk 
                        WHERE vk.vocabulary_id = mv.vocabulary_id 
                        AND vk.text = ANY(exact_terms)
                    ) THEN 1000
                    -- Check kanji exact match
                    WHEN EXISTS (
                        SELECT 1 FROM jlpt.vocabulary_kanji vj 
                        WHERE vj.vocabulary_id = mv.vocabulary_id 
                        AND vj.text = ANY(exact_terms)
                    ) THEN 1000
                    -- Check gloss exact match
                    WHEN EXISTS (
                        SELECT 1 FROM jlpt.vocabulary_sense vs 
                        JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
                        WHERE vs.vocabulary_id = mv.vocabulary_id 
                        AND lower(vsg.text) = ANY(SELECT lower(unnest(exact_terms)))
                    ) THEN 1000
                    -- Check prefix matches
                    WHEN NOT has_user_wildcard AND EXISTS (
                        SELECT 1 FROM jlpt.vocabulary_kana vk, unnest(exact_terms) et
                        WHERE vk.vocabulary_id = mv.vocabulary_id AND vk.text LIKE et || '%'
                    ) THEN 500
                    WHEN NOT has_user_wildcard AND EXISTS (
                        SELECT 1 FROM jlpt.vocabulary_kanji vj, unnest(exact_terms) et
                        WHERE vj.vocabulary_id = mv.vocabulary_id AND vj.text LIKE et || '%'
                    ) THEN 500
                    WHEN NOT has_user_wildcard AND EXISTS (
                        SELECT 1 FROM jlpt.vocabulary_sense vs 
                        JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id,
                        unnest(exact_terms) et
                        WHERE vs.vocabulary_id = mv.vocabulary_id AND vsg.text ILIKE et || '%'
                    ) THEN 500
                    -- Wildcard or contains
                    WHEN has_user_wildcard THEN 100
                    ELSE 200
                END
            ) as best_quality,
            -- Compute match location bitmask
            (
                CASE WHEN EXISTS (SELECT 1 FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = mv.vocabulary_id AND vk.text LIKE ANY(patterns)) THEN 1 ELSE 0 END
                | CASE WHEN EXISTS (SELECT 1 FROM jlpt.vocabulary_kanji vj WHERE vj.vocabulary_id = mv.vocabulary_id AND vj.text LIKE ANY(patterns)) THEN 2 ELSE 0 END
                | CASE WHEN EXISTS (
                    SELECT 1 FROM jlpt.vocabulary_sense vs 
                    JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
                    WHERE vs.vocabulary_id = mv.vocabulary_id AND vsg.text ILIKE ANY(patterns)
                ) THEN 4 ELSE 0 END
                | CASE WHEN EXISTS (
                    SELECT 1 FROM jlpt.vocabulary_sense vs 
                    JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
                    WHERE vs.vocabulary_id = mv.vocabulary_id AND vsg.text ILIKE ANY(patterns)
                    AND vs.id = (SELECT vs2.id FROM jlpt.vocabulary_sense vs2 WHERE vs2.vocabulary_id = mv.vocabulary_id ORDER BY vs2.id LIMIT 1)
                ) THEN 8 ELSE 0 END
            ) as locations,
            -- Shortest matched text length
            COALESCE(
                (SELECT MIN(length(vk.text)) FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = mv.vocabulary_id AND vk.text LIKE ANY(patterns)),
                (SELECT MIN(length(vj.text)) FROM jlpt.vocabulary_kanji vj WHERE vj.vocabulary_id = mv.vocabulary_id AND vj.text LIKE ANY(patterns)),
                (SELECT MIN(length(vsg.text)) FROM jlpt.vocabulary_sense vs JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id WHERE vs.vocabulary_id = mv.vocabulary_id AND vsg.text ILIKE ANY(patterns)),
                0
            ) as shortest_match
        FROM matching_vocab_ids mv
        GROUP BY mv.vocabulary_id
    ),
    -- Apply language filter for non-gloss matches
    language_filtered AS (
        SELECT md.*
        FROM match_details md
        WHERE (langs IS NULL OR array_length(langs, 1) IS NULL)
           OR (md.locations & 4 = 4)  -- Has gloss match (already language-filtered)
           OR ((md.locations & (1 | 2)) != 0 AND EXISTS (
               SELECT 1 FROM jlpt.vocabulary_sense vs
               JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id 
               WHERE vs.vocabulary_id = md.vocabulary_id AND vsg.lang = ANY(langs)
           ))
    ),
    -- Apply other filters
    filtered AS (
        SELECT 
            v.id,
            v.jmdict_id,
            v.jlpt_level_new,
            lf.best_quality,
            lf.locations,
            lf.shortest_match,
            EXISTS (SELECT 1 FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = v.id AND vk.is_common) OR
            EXISTS (SELECT 1 FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = v.id AND vk.is_common) as is_common
        FROM language_filtered lf
        JOIN jlpt.vocabulary v ON v.id = lf.vocabulary_id
        WHERE 
            (jlpt_min <= 0 OR v.jlpt_level_new >= jlpt_min)
            AND (jlpt_max <= 0 OR v.jlpt_level_new <= jlpt_max)
            AND (NOT common_only OR EXISTS (SELECT 1 FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = v.id AND vk.is_common))
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
        ARRAY(SELECT vk.text FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_primary DESC, vk.is_common DESC),
        ARRAY(SELECT vk.text FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = p.id ORDER BY vk.is_primary DESC, vk.is_common DESC),
        ARRAY(SELECT vsg.text FROM jlpt.vocabulary_sense vs JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id 
              WHERE vs.vocabulary_id = p.id ORDER BY vs.id LIMIT 5),
        ARRAY(SELECT vsg.text FROM jlpt.vocabulary_sense vs JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id 
              WHERE vs.vocabulary_id = p.id),
        (SELECT row_to_json(x) FROM (
            SELECT vk.text, vk.is_common as "isCommon",
                COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.vocabulary_kanji_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kanji_id = vk.id), '[]'::json) as tags
            FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = p.id AND vk.is_primary = true LIMIT 1
        ) x),
        (SELECT row_to_json(x) FROM (
            SELECT vk.text, vk.is_common as "isCommon", vk.applies_to_kanji as "appliesToKanji",
                COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.vocabulary_kana_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kana_id = vk.id), '[]'::json) as tags
            FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id AND vk.is_primary = true LIMIT 1
        ) x),
        (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
            SELECT vk.text, vk.is_common as "isCommon",
                COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.vocabulary_kanji_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kanji_id = vk.id), '[]'::json) as tags
            FROM jlpt.vocabulary_kanji vk WHERE vk.vocabulary_id = p.id AND vk.is_primary = false
        ) x),
        (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
            SELECT vk.text, vk.is_common as "isCommon", vk.applies_to_kanji as "appliesToKanji",
                COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.vocabulary_kana_tag vkt JOIN jlpt.tag t ON vkt.tag_code = t.code WHERE vkt.vocabulary_kana_id = vk.id), '[]'::json) as tags
            FROM jlpt.vocabulary_kana vk WHERE vk.vocabulary_id = p.id AND vk.is_primary = false
        ) x),
        (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
            WITH senses_with_langs AS (
                SELECT DISTINCT vs.id as sense_id, vsg.lang
                FROM jlpt.vocabulary_sense vs
                JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
                WHERE vs.vocabulary_id = p.id
            ),
            ranked_senses AS (
                SELECT 
                    sense_id,
                    lang,
                    ROW_NUMBER() OVER (PARTITION BY lang ORDER BY sense_id) as sense_rank
                FROM senses_with_langs
            ),
            top_senses AS (
                SELECT DISTINCT sense_id
                FROM ranked_senses
                WHERE sense_rank <= 3
            )
            SELECT vs.applies_to_kanji as "appliesToKanji", vs.applies_to_kana as "appliesToKana", vs.info,
                COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category, 'type', vst.tag_type))
                    FROM jlpt.vocabulary_sense_tag vst JOIN jlpt.tag t ON vst.tag_code = t.code WHERE vst.sense_id = vs.id), '[]'::json) as tags,
                COALESCE((SELECT json_agg(json_build_object('language', vsg.lang, 'text', vsg.text) ORDER BY vsg.id)
                    FROM jlpt.vocabulary_sense_gloss vsg WHERE vsg.sense_id = vs.id), '[]'::json) as glosses
            FROM top_senses ts
            JOIN jlpt.vocabulary_sense vs ON vs.id = ts.sense_id
            ORDER BY vs.id
        ) x),
        (SELECT COALESCE(json_agg(json_build_object(
            'text', vf.text,
            'reading', vf.reading,
            'furigana', vf.furigana
        )), '[]'::json) FROM jlpt.vocabulary_furigana vf WHERE vf.vocabulary_id = p.id),
        -- Compute slug
        (SELECT 
            CASE 
                WHEN pk.text IS NOT NULL THEN
                    CASE WHEN (SELECT COUNT(*) FROM jlpt.vocabulary_kanji vk2 WHERE vk2.text = pk.text AND vk2.is_primary = true) = 1 
                        THEN pk.text
                        ELSE pk.text || '(' || COALESCE(pka.text, '') || ')'
                    END
                WHEN pka.text IS NOT NULL THEN pka.text
                ELSE NULL
            END
        FROM (SELECT text FROM jlpt.vocabulary_kanji WHERE vocabulary_id = p.id AND is_primary = true LIMIT 1) pk
        FULL OUTER JOIN (SELECT text FROM jlpt.vocabulary_kana WHERE vocabulary_id = p.id AND is_primary = true LIMIT 1) pka ON true),
        (SELECT cnt FROM counted)
    FROM paginated p;
END;
$$ LANGUAGE plpgsql STABLE;

CREATE OR REPLACE FUNCTION jlpt.search_proper_noun_ranked(
    patterns TEXT[],                    -- Individual patterns - each must match somewhere in entry
    token_variant_counts INT[],         -- Number of variants per token (for OR within, AND across)
    combined_patterns TEXT[],           -- Combined phrase patterns (for OR against whole token logic)
    exact_terms TEXT[],                 -- Exact terms for ranking (without wildcards)
    has_user_wildcard BOOLEAN,          -- Whether user used wildcards
    filter_tags TEXT[],                 -- Filter: tags
    langs TEXT[],                       -- Filter: Languages
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
    furigana JSON,
    slug TEXT,                          -- URL-friendly identifier: "term" or "term(reading)"
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
            AND (langs IS NULL OR array_length(langs, 1) IS NULL OR EXISTS (
                SELECT 1 FROM jlpt.proper_noun_translation pt 
                JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id 
                WHERE pt.proper_noun_id = p.id AND ptt.lang = ANY(langs)
            ))
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
            -- Primary kanji
            (SELECT row_to_json(x) FROM (
                SELECT pk.text, COALESCE((
                    SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.proper_noun_kanji_tag pkt JOIN jlpt.tag t ON pkt.tag_code = t.code WHERE pkt.proper_noun_kanji_id = pk.id
                ), '[]'::json) as tags
                FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = p.id AND pk.is_primary = true LIMIT 1
            ) x),
            -- Primary kana
            (SELECT row_to_json(x) FROM (
                SELECT pk.text, pk.applies_to_kanji as "appliesToKanji", COALESCE((
                    SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.proper_noun_kana_tag pkt JOIN jlpt.tag t ON pkt.tag_code = t.code WHERE pkt.proper_noun_kana_id = pk.id
                ), '[]'::json) as tags
                FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = p.id AND pk.is_primary = true LIMIT 1
            ) x),
            -- Other kanji forms
            (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
                SELECT pk.text, COALESCE((
                    SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.proper_noun_kanji_tag pkt JOIN jlpt.tag t ON pkt.tag_code = t.code WHERE pkt.proper_noun_kanji_id = pk.id
                ), '[]'::json) as tags
                FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = p.id AND pk.is_primary = false
            ) x),
            -- Other kana forms
            (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
                SELECT pk.text, pk.applies_to_kanji as "appliesToKanji", COALESCE((
                    SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.proper_noun_kana_tag pkt JOIN jlpt.tag t ON pkt.tag_code = t.code WHERE pkt.proper_noun_kana_id = pk.id
                ), '[]'::json) as tags
                FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = p.id AND pk.is_primary = false
            ) x),
            -- Translations (top 3 per language)
            (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
                WITH translations_with_langs AS (
                    SELECT DISTINCT pt.id as translation_id, ptt.lang
                    FROM jlpt.proper_noun_translation pt
                    JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id
                    WHERE pt.proper_noun_id = p.id
                ),
                ranked_translations AS (
                    SELECT 
                        translation_id,
                        lang,
                        ROW_NUMBER() OVER (PARTITION BY lang ORDER BY translation_id) as translation_rank
                    FROM translations_with_langs
                ),
                top_translations AS (
                    SELECT DISTINCT translation_id
                    FROM ranked_translations
                    WHERE translation_rank <= 3
                )
                SELECT 
                    COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                        FROM jlpt.proper_noun_translation_type ptt JOIN jlpt.tag t ON ptt.tag_code = t.code WHERE ptt.translation_id = pt.id), '[]'::json) as types,
                    COALESCE((SELECT json_agg(json_build_object('language', ptt.lang, 'text', ptt.text) ORDER BY ptt.id)
                        FROM jlpt.proper_noun_translation_text ptt WHERE ptt.translation_id = pt.id), '[]'::json) as translations
                FROM top_translations tt
                JOIN jlpt.proper_noun_translation pt ON pt.id = tt.translation_id
                ORDER BY pt.id
            ) x),
            -- Compute slug
            (SELECT 
                CASE 
                    WHEN pk.text IS NOT NULL THEN
                        CASE WHEN (SELECT COUNT(*) FROM jlpt.proper_noun_kanji pnk2 WHERE pnk2.text = pk.text AND pnk2.is_primary = true) = 1 
                            THEN pk.text
                            ELSE pk.text || '(' || COALESCE(pka.text, '') || ')'
                        END
                    WHEN pka.text IS NOT NULL THEN pka.text
                    ELSE NULL
                END
            FROM (SELECT text FROM jlpt.proper_noun_kanji WHERE proper_noun_id = p.id AND is_primary = true LIMIT 1) pk
            FULL OUTER JOIN (SELECT text FROM jlpt.proper_noun_kana WHERE proper_noun_id = p.id AND is_primary = true LIMIT 1) pka ON true),
            (SELECT cnt FROM counted)
        FROM paginated p;
        RETURN;
    END IF;

    -- Main search with patterns - each pattern must match somewhere in the entry
    RETURN QUERY
    WITH 
    -- Step 1: Find candidate proper noun IDs matching ANY pattern (uses indexes)
    candidate_ids AS (
        SELECT DISTINCT pk.proper_noun_id FROM jlpt.proper_noun_kanji pk WHERE pk.text LIKE ANY(patterns)
        UNION
        SELECT DISTINCT pk.proper_noun_id FROM jlpt.proper_noun_kana pk WHERE pk.text LIKE ANY(patterns)
        UNION
        SELECT DISTINCT pt.proper_noun_id 
        FROM jlpt.proper_noun_translation pt 
        JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id
        WHERE ptt.text ILIKE ANY(patterns)
            AND (langs IS NULL OR array_length(langs, 1) IS NULL OR ptt.lang = ANY(langs))
    ),
    -- Step 2: Filter candidates to those matching ALL patterns
    -- We need to handle variant groups: (P1 OR P2) AND (P3) AND (P4 OR P5 OR P6)
    -- OR if a combined pattern matches a single field (phrase search)
    matching_proper_noun_ids AS (
        SELECT c.proper_noun_id
        FROM candidate_ids c
        WHERE (
            -- Option A: Check that ALL tokens have at least one matching variant
            (SELECT bool_and(token_match)
            FROM (
                SELECT 
                    -- For each token, check if ANY of its variants match
                    bool_or(
                        EXISTS (SELECT 1 FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = c.proper_noun_id AND pk.text LIKE pattern)
                        OR EXISTS (SELECT 1 FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = c.proper_noun_id AND pk.text LIKE pattern)
                        OR EXISTS (
                            SELECT 1 FROM jlpt.proper_noun_translation pt 
                            JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id
                            WHERE pt.proper_noun_id = c.proper_noun_id 
                            AND ptt.text ILIKE pattern
                            AND (langs IS NULL OR array_length(langs, 1) IS NULL OR ptt.lang = ANY(langs))
                        )
                    ) as token_match
                FROM (
                    -- Unnest patterns with their token index
                    SELECT 
                        p.pattern,
                        t.token_index
                    FROM (
                        -- Generate ranges for each token based on variant counts
                        -- token_index | start_idx | end_idx
                        SELECT 
                            ordinality as token_index,
                            sum(vc) OVER (ORDER BY ordinality) - vc + 1 as start_idx,
                            sum(vc) OVER (ORDER BY ordinality) as end_idx
                        FROM unnest(token_variant_counts) WITH ORDINALITY as t(vc, ordinality)
                    ) t
                    JOIN unnest(patterns) WITH ORDINALITY as p(pattern, ordinality) 
                    ON p.ordinality BETWEEN t.start_idx AND t.end_idx
                ) p_with_token
                GROUP BY token_index
            ) token_checks)

            OR

            -- Option B: Combined pattern matches a single field (Phrase Match)
            (combined_patterns IS NOT NULL AND EXISTS (
                SELECT 1
                WHERE 
                    EXISTS (SELECT 1 FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = c.proper_noun_id AND pk.text LIKE ANY(combined_patterns))
                    OR EXISTS (SELECT 1 FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = c.proper_noun_id AND pk.text LIKE ANY(combined_patterns))
                    OR EXISTS (
                        SELECT 1 FROM jlpt.proper_noun_translation pt 
                        JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id
                        WHERE pt.proper_noun_id = c.proper_noun_id 
                        AND ptt.text ILIKE ANY(combined_patterns)
                        AND (langs IS NULL OR array_length(langs, 1) IS NULL OR ptt.lang = ANY(langs))
                    )
            ))
        )
    ),
    -- For matched proper nouns, compute match quality and location
    match_details AS (
        SELECT 
            mp.proper_noun_id,
            -- Best quality across all patterns
            MAX(
                CASE 
                    -- Check kanji exact match
                    WHEN EXISTS (
                        SELECT 1 FROM jlpt.proper_noun_kanji pk 
                        WHERE pk.proper_noun_id = mp.proper_noun_id 
                        AND pk.text = ANY(exact_terms)
                    ) THEN 1000
                    -- Check kana exact match
                    WHEN EXISTS (
                        SELECT 1 FROM jlpt.proper_noun_kana pk 
                        WHERE pk.proper_noun_id = mp.proper_noun_id 
                        AND pk.text = ANY(exact_terms)
                    ) THEN 1000
                    -- Check translation exact match
                    WHEN EXISTS (
                        SELECT 1 FROM jlpt.proper_noun_translation pt 
                        JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id
                        WHERE pt.proper_noun_id = mp.proper_noun_id 
                        AND lower(ptt.text) = ANY(SELECT lower(unnest(exact_terms)))
                    ) THEN 1000
                    -- Check prefix matches
                    WHEN NOT has_user_wildcard AND EXISTS (
                        SELECT 1 FROM jlpt.proper_noun_kanji pk, unnest(exact_terms) et
                        WHERE pk.proper_noun_id = mp.proper_noun_id AND pk.text LIKE et || '%'
                    ) THEN 500
                    WHEN NOT has_user_wildcard AND EXISTS (
                        SELECT 1 FROM jlpt.proper_noun_kana pk, unnest(exact_terms) et
                        WHERE pk.proper_noun_id = mp.proper_noun_id AND pk.text LIKE et || '%'
                    ) THEN 500
                    WHEN NOT has_user_wildcard AND EXISTS (
                        SELECT 1 FROM jlpt.proper_noun_translation pt 
                        JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id,
                        unnest(exact_terms) et
                        WHERE pt.proper_noun_id = mp.proper_noun_id AND ptt.text ILIKE et || '%'
                    ) THEN 500
                    -- Wildcard or contains
                    WHEN has_user_wildcard THEN 100
                    ELSE 200
                END
            ) as best_quality,
            -- Compute match location bitmask
            (
                CASE WHEN EXISTS (SELECT 1 FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = mp.proper_noun_id AND pk.text LIKE ANY(patterns)) THEN 1 ELSE 0 END
                | CASE WHEN EXISTS (SELECT 1 FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = mp.proper_noun_id AND pk.text LIKE ANY(patterns)) THEN 2 ELSE 0 END
                | CASE WHEN EXISTS (
                    SELECT 1 FROM jlpt.proper_noun_translation pt 
                    JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id
                    WHERE pt.proper_noun_id = mp.proper_noun_id AND ptt.text ILIKE ANY(patterns)
                ) THEN 128 ELSE 0 END
            ) as locations,
            -- Shortest matched text length
            COALESCE(
                (SELECT MIN(length(pk.text)) FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = mp.proper_noun_id AND pk.text LIKE ANY(patterns)),
                (SELECT MIN(length(pk.text)) FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = mp.proper_noun_id AND pk.text LIKE ANY(patterns)),
                (SELECT MIN(length(ptt.text)) FROM jlpt.proper_noun_translation pt JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id WHERE pt.proper_noun_id = mp.proper_noun_id AND ptt.text ILIKE ANY(patterns)),
                0
            ) as shortest_match
        FROM matching_proper_noun_ids mp
        GROUP BY mp.proper_noun_id
    ),
    -- Apply language filter
    language_filtered AS (
        SELECT md.*
        FROM match_details md
        WHERE (langs IS NULL OR array_length(langs, 1) IS NULL)
           OR (md.locations & 128 = 128)  -- Has translation match (already language-filtered)
           OR ((md.locations & (1 | 2)) != 0 AND EXISTS (
               SELECT 1 FROM jlpt.proper_noun_translation pt 
               JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id 
               WHERE pt.proper_noun_id = md.proper_noun_id AND ptt.lang = ANY(langs)
           ))
    ),
    filtered AS (
        SELECT p.id, p.jmnedict_id, lf.best_quality, lf.locations, lf.shortest_match
        FROM language_filtered lf
        JOIN jlpt.proper_noun p ON p.id = lf.proper_noun_id
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
        ARRAY(SELECT pk.text FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = p.id ORDER BY pk.is_primary DESC),
        ARRAY(SELECT pk.text FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = p.id ORDER BY pk.is_primary DESC),
        ARRAY(SELECT ptt.text FROM jlpt.proper_noun_translation pt JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id WHERE pt.proper_noun_id = p.id),
        -- Primary kanji
        (SELECT row_to_json(x) FROM (
            SELECT pk.text, COALESCE((
                SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                FROM jlpt.proper_noun_kanji_tag pkt JOIN jlpt.tag t ON pkt.tag_code = t.code WHERE pkt.proper_noun_kanji_id = pk.id
            ), '[]'::json) as tags
            FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = p.id AND pk.is_primary = true LIMIT 1
        ) x),
        -- Primary kana
        (SELECT row_to_json(x) FROM (
            SELECT pk.text, pk.applies_to_kanji as "appliesToKanji", COALESCE((
                SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                FROM jlpt.proper_noun_kana_tag pkt JOIN jlpt.tag t ON pkt.tag_code = t.code WHERE pkt.proper_noun_kana_id = pk.id
            ), '[]'::json) as tags
            FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = p.id AND pk.is_primary = true LIMIT 1
        ) x),
        -- Other kanji forms
        (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
            SELECT pk.text, COALESCE((
                SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                FROM jlpt.proper_noun_kanji_tag pkt JOIN jlpt.tag t ON pkt.tag_code = t.code WHERE pkt.proper_noun_kanji_id = pk.id
            ), '[]'::json) as tags
            FROM jlpt.proper_noun_kanji pk WHERE pk.proper_noun_id = p.id AND pk.is_primary = false
        ) x),
        -- Other kana forms
        (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
            SELECT pk.text, pk.applies_to_kanji as "appliesToKanji", COALESCE((
                SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                FROM jlpt.proper_noun_kana_tag pkt JOIN jlpt.tag t ON pkt.tag_code = t.code WHERE pkt.proper_noun_kana_id = pk.id
            ), '[]'::json) as tags
            FROM jlpt.proper_noun_kana pk WHERE pk.proper_noun_id = p.id AND pk.is_primary = false
        ) x),
        -- Translations (top 3 per language)
        (SELECT COALESCE(json_agg(row_to_json(x)), '[]'::json) FROM (
            WITH translations_with_langs AS (
                SELECT DISTINCT pt.id as translation_id, ptt.lang
                FROM jlpt.proper_noun_translation pt
                JOIN jlpt.proper_noun_translation_text ptt ON pt.id = ptt.translation_id
                WHERE pt.proper_noun_id = p.id
            ),
            ranked_translations AS (
                SELECT 
                    translation_id,
                    lang,
                    ROW_NUMBER() OVER (PARTITION BY lang ORDER BY translation_id) as translation_rank
                FROM translations_with_langs
            ),
            top_translations AS (
                SELECT DISTINCT translation_id
                FROM ranked_translations
                WHERE translation_rank <= 3
            )
            SELECT 
                COALESCE((SELECT json_agg(json_build_object('code', t.code, 'description', t.description, 'category', t.category))
                    FROM jlpt.proper_noun_translation_type ptt JOIN jlpt.tag t ON ptt.tag_code = t.code WHERE ptt.translation_id = pt.id), '[]'::json) as types,
                COALESCE((SELECT json_agg(json_build_object('language', ptt.lang, 'text', ptt.text) ORDER BY ptt.id)
                    FROM jlpt.proper_noun_translation_text ptt WHERE ptt.translation_id = pt.id), '[]'::json) as translations
            FROM top_translations tt
            JOIN jlpt.proper_noun_translation pt ON pt.id = tt.translation_id
            ORDER BY pt.id
        ) x),
        (SELECT COALESCE(json_agg(json_build_object(
            'text', pnf.text,
            'reading', pnf.reading,
            'furigana', pnf.furigana
        )), '[]'::json) FROM jlpt.proper_noun_furigana pnf WHERE pnf.proper_noun_id = p.id),
        -- Compute slug
        (SELECT 
            CASE 
                WHEN pk.text IS NOT NULL THEN
                    CASE WHEN (SELECT COUNT(*) FROM jlpt.proper_noun_kanji pnk2 WHERE pnk2.text = pk.text AND pnk2.is_primary = true) = 1 
                        THEN pk.text
                        ELSE pk.text || '(' || COALESCE(pka.text, '') || ')'
                    END
                WHEN pka.text IS NOT NULL THEN pka.text
                ELSE NULL
            END
        FROM (SELECT text FROM jlpt.proper_noun_kanji WHERE proper_noun_id = p.id AND is_primary = true LIMIT 1) pk
        FULL OUTER JOIN (SELECT text FROM jlpt.proper_noun_kana WHERE proper_noun_id = p.id AND is_primary = true LIMIT 1) pka ON true),
        (SELECT cnt FROM counted)
    FROM paginated p;
END;
$$ LANGUAGE plpgsql STABLE;

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