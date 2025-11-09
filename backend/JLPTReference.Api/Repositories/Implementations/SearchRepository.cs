using System.Text.Json;
using JLPTReference.Api.Data;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.DTOs.ProperNoun;
using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Radical;
using JLPTReference.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace JLPTReference.Api.Repositories.Implementations;

public class SearchRepository : ISearchRepository
{
    private readonly ApplicationDBContext _context;

    public SearchRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<GlobalSearchResponse> SearchAllAsync(GlobalSearchRequest request)
    {
        var sql = """
            -- UNIFIED SEARCH QUERY
            WITH 
            -- ============ VOCABULARY SEARCH ============
            vocab_search_matches AS (
                SELECT DISTINCT
                    v.id as vocabulary_id,
                    vk.text as match_text,
                    'kanji' as match_type,
                    similarity(vk.text, {1}) as relevance
                FROM vocabulary v
                JOIN vocabulary_kanji vk ON v.id = vk.vocabulary_id
                WHERE vk.text % {1}
                
                UNION ALL
                
                SELECT DISTINCT
                    v.id as vocabulary_id,
                    vkn.text as match_text,
                    'kana' as match_type,
                    similarity(vkn.text, {1}) as relevance
                FROM vocabulary v
                JOIN vocabulary_kana vkn ON v.id = vkn.vocabulary_id
                WHERE vkn.text % {1}
                
                UNION ALL
                
                SELECT DISTINCT
                    v.id as vocabulary_id,
                    vsg.text as match_text,
                    'gloss' as match_type,
                    similarity(vsg.text, {1}) as relevance
                FROM vocabulary v
                JOIN vocabulary_sense vs ON v.id = vs.vocabulary_id
                JOIN vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
                WHERE vsg.text % {1}
            ),
            vocab_ranked_matches AS (
                SELECT 
                    vocabulary_id,
                    MAX(relevance) as max_relevance
                FROM vocab_search_matches
                GROUP BY vocabulary_id
            ),
            vocab_results AS (
                SELECT
                    v.id,
                    'vocabulary' as entry_type,
                    2 as entry_type_order,
                    v.jmdict_id as dict_id,
                    rm.max_relevance as relevance_score,
                    
                    -- Primary kanji
                    (SELECT json_build_object(
                        'text', vk.text,
                        'is_common', vk.is_common,
                        'tags', COALESCE(
                            (SELECT json_agg(json_build_object(
                                'code', t.code,
                                'description', t.description,
                                'category', t.category
                            ))
                            FROM vocabulary_kanji_tag vkt
                            JOIN tag t ON vkt.tag_code = t.code
                            WHERE vkt.vocabulary_kanji_id = vk.id),
                            '[]'::json
                        )
                    )
                    FROM vocabulary_kanji vk
                    WHERE vk.vocabulary_id = v.id
                    ORDER BY vk.is_common DESC, vk.created_at ASC
                    LIMIT 1
                    ) as primary_kanji,
                    
                    -- Primary kana
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
                            FROM vocabulary_kana_tag vknt
                            JOIN tag t ON vknt.tag_code = t.code
                            WHERE vknt.vocabulary_kana_id = vkn.id),
                            '[]'::json
                        )
                    )
                    FROM vocabulary_kana vkn
                    WHERE vkn.vocabulary_id = v.id
                    ORDER BY vkn.is_common DESC, vkn.created_at ASC
                    LIMIT 1
                    ) as primary_kana,
                    
                    -- Other kanji forms
                    (SELECT json_agg(json_build_object(
                        'text', vk.text,
                        'is_common', vk.is_common,
                        'tags', COALESCE(
                            (SELECT json_agg(json_build_object(
                                'code', t.code,
                                'description', t.description,
                                'category', t.category
                            ))
                            FROM vocabulary_kanji_tag vkt
                            JOIN tag t ON vkt.tag_code = t.code
                            WHERE vkt.vocabulary_kanji_id = vk.id),
                            '[]'::json
                        )
                    ) ORDER BY vk.is_common DESC, vk.created_at ASC)
                    FROM vocabulary_kanji vk
                    WHERE vk.vocabulary_id = v.id
                    OFFSET 1
                    ) as other_kanji_forms,
                    
                    -- Other kana forms
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
                            FROM vocabulary_kana_tag vknt
                            JOIN tag t ON vknt.tag_code = t.code
                            WHERE vknt.vocabulary_kana_id = vkn.id),
                            '[]'::json
                        )
                    ) ORDER BY vkn.is_common DESC, vkn.created_at ASC)
                    FROM vocabulary_kana vkn
                    WHERE vkn.vocabulary_id = v.id
                    OFFSET 1
                    ) as other_kana_forms,
                    
                    -- First 3 senses
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
                                        'lang', vsg.lang,
                                        'text', vsg.text,
                                        'gender', vsg.gender,
                                        'type', vsg.type
                                    ))
                                    FROM vocabulary_sense_gloss vsg
                                    WHERE vsg.sense_id = vs.id
                                ),
                                'tags', (
                                    SELECT json_agg(json_build_object(
                                        'code', t.code,
                                        'description', t.description,
                                        'category', t.category,
                                        'type', vst.tag_type
                                    ))
                                    FROM vocabulary_sense_tag vst
                                    JOIN tag t ON vst.tag_code = t.code
                                    WHERE vst.sense_id = vs.id
                                )
                            ) as sense_data
                        FROM vocabulary_sense vs
                        WHERE vs.vocabulary_id = v.id
                        ORDER BY vs.created_at
                        LIMIT 3
                    ) senses
                    ) as senses,
                    
                    -- Vocabulary specific fields
                    v.jlpt_level_new,
                    CASE 
                        WHEN (
                            SELECT vk.is_common 
                            FROM vocabulary_kanji vk 
                            WHERE vk.vocabulary_id = v.id 
                            ORDER BY vk.is_common DESC, vk.created_at ASC 
                            LIMIT 1
                        ) = true 
                        OR (
                            SELECT vkn.is_common 
                            FROM vocabulary_kana vkn 
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
                    
                FROM vocabulary v
                JOIN vocab_ranked_matches rm ON v.id = rm.vocabulary_id
            ),

            -- ============ PROPER NOUN SEARCH ============
            proper_noun_search_matches AS (
                SELECT DISTINCT
                    pn.id as proper_noun_id,
                    pnk.text as match_text,
                    'kanji' as match_type,
                    similarity(pnk.text, {1}) as relevance
                FROM proper_noun pn
                JOIN proper_noun_kanji pnk ON pn.id = pnk.proper_noun_id
                WHERE pnk.text % {1}
                
                UNION ALL
                
                SELECT DISTINCT
                    pn.id as proper_noun_id,
                    pnkn.text as match_text,
                    'kana' as match_type,
                    similarity(pnkn.text, {1}) as relevance
                FROM proper_noun pn
                JOIN proper_noun_kana pnkn ON pn.id = pnkn.proper_noun_id
                WHERE pnkn.text % {1}
                
                UNION ALL
                
                SELECT DISTINCT
                    pn.id as proper_noun_id,
                    pntt.text as match_text,
                    'translation' as match_type,
                    similarity(pntt.text, {1}) as relevance
                FROM proper_noun pn
                JOIN proper_noun_translation pnt ON pn.id = pnt.proper_noun_id
                JOIN proper_noun_translation_text pntt ON pnt.id = pntt.translation_id
                WHERE pntt.text % {1}
            ),
            proper_noun_ranked_matches AS (
                SELECT 
                    proper_noun_id,
                    MAX(relevance) as max_relevance
                FROM proper_noun_search_matches
                GROUP BY proper_noun_id
            ),
            proper_noun_results AS (
                SELECT
                    pn.id,
                    'proper_noun' as entry_type,
                    3 as entry_type_order,
                    pn.jmnedict_id as dict_id,
                    rm.max_relevance as relevance_score,
                    
                    -- Primary kanji
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
                    
                    -- Primary kana
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
                    
                    -- Other kanji forms
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
                    
                    -- Other kana forms
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
                    NULL::integer as jlpt_level_new,
                    false as is_common,
                    
                    -- Translations
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
                                        'lang', pnttxt.lang,
                                        'text', pnttxt.text
                                    ))
                                    FROM jlpt.proper_noun_translation_text pnttxt
                                    WHERE pnttxt.translation_id = pnt.id
                                ),
                                'related_terms', (
                                    SELECT json_agg(json_build_object(
                                        'term', pntr.related_term,
                                        'reading', pntr.related_reading,
                                        'reference_proper_noun_id', pntr.reference_proper_noun_id,
                                        'reference_translation_id', pntr.reference_proper_noun_translation_id
                                    ))
                                    FROM jlpt.proper_noun_translation_related pntr
                                    WHERE pntr.translation_id = pnt.id
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
            ),

            -- ============ KANJI SEARCH ============
            kanji_search_matches AS (
                SELECT DISTINCT
                    k.id as kanji_id,
                    k.literal as match_text,
                    'literal' as match_type,
                    CASE 
                        WHEN k.literal = {1} THEN 1.0
                        ELSE similarity(k.literal, {1})
                    END as relevance
                FROM kanji k
                WHERE k.literal = {1} OR k.literal % {1}
                
                UNION ALL
                
                SELECT DISTINCT
                    k.id as kanji_id,
                    kr.value as match_text,
                    'reading' as match_type,
                    similarity(kr.value, {1}) as relevance
                FROM kanji k
                JOIN kanji_reading kr ON k.id = kr.kanji_id
                WHERE kr.value % {1}
                
                UNION ALL
                
                SELECT DISTINCT
                    k.id as kanji_id,
                    km.value as match_text,
                    'meaning' as match_type,
                    similarity(km.value, {1}) as relevance
                FROM kanji k
                JOIN kanji_meaning km ON k.id = km.kanji_id
                WHERE km.value % {1}
            ),
            kanji_ranked_matches AS (
                SELECT 
                    kanji_id,
                    MAX(relevance) as max_relevance
                FROM kanji_search_matches
                GROUP BY kanji_id
            ),
            kanji_results AS (
                SELECT
                    k.id,
                    'kanji' as entry_type,
                    1 as entry_type_order,
                    k.literal as dict_id,
                    rm.max_relevance as relevance_score,
                    
                    NULL::json as primary_kanji,
                    NULL::json as primary_kana,
                    NULL::json as other_kanji_forms,
                    NULL::json as other_kana_forms,
                    NULL::json as senses,
                    NULL::integer as jlpt_level_new,
                    false as is_common,
                    NULL::json as translations,
                    
                    -- Kanji specific fields
                    k.literal,
                    k.grade,
                    k.stroke_count,
                    k.frequency,
                    
                    -- Kunyomi readings
                    (SELECT json_agg(json_build_object(
                        'value', kr.value,
                        'status', kr.status
                    ) ORDER BY kr.created_at)
                    FROM kanji_reading kr
                    WHERE kr.kanji_id = k.id 
                    AND kr.type = 'ja_kun'
                    ) as kunyomi,
                    
                    -- Onyomi readings
                    (SELECT json_agg(json_build_object(
                        'value', kr.value,
                        'status', kr.status,
                        'on_type', kr.on_type
                    ) ORDER BY kr.created_at)
                    FROM kanji_reading kr
                    WHERE kr.kanji_id = k.id 
                    AND kr.type = 'ja_on'
                    ) as onyomi,
                    
                    -- Meanings
                    (SELECT json_agg(json_build_object(
                        'lang', km.lang,
                        'value', km.value
                    ) ORDER BY km.created_at)
                    FROM kanji_meaning km
                    WHERE km.kanji_id = k.id
                    ) as meanings_kanji,
                    
                    -- Radicals
                    (SELECT json_agg(json_build_object(
                        'literal', r.literal,
                        'stroke_count', r.stroke_count,
                        'code', r.code
                    ))
                    FROM kanji_radical kr
                    JOIN radical r ON kr.radical_id = r.id
                    WHERE kr.kanji_id = k.id
                    ) as radicals
                    
                FROM kanji k
                JOIN kanji_ranked_matches rm ON k.id = rm.kanji_id
            )

            -- ============ COMBINE ALL RESULTS ============
            SELECT 
                id,
                entry_type,
                entry_type_order,
                dict_id,
                relevance_score,
                primary_kanji,
                primary_kana,
                other_kanji_forms,
                other_kana_forms,
                senses,
                jlpt_level_new,
                is_common,
                translations,
                literal,
                grade,
                stroke_count,
                frequency,
                kunyomi,
                onyomi,
                meanings_kanji,
                radicals
            FROM vocab_results

            UNION ALL

            SELECT 
                id,
                entry_type,
                entry_type_order,
                dict_id,
                relevance_score,
                primary_kanji,
                primary_kana,
                other_kanji_forms,
                other_kana_forms,
                senses,
                jlpt_level_new,
                is_common,
                translations,
                literal,
                grade,
                stroke_count,
                frequency,
                kunyomi,
                onyomi,
                meanings_kanji,
                radicals
            FROM proper_noun_results

            UNION ALL

            SELECT 
                id,
                entry_type,
                entry_type_order,
                dict_id,
                relevance_score,
                primary_kanji,
                primary_kana,
                other_kanji_forms,
                other_kana_forms,
                senses,
                jlpt_level_new,
                is_common,
                translations,
                literal,
                grade,
                stroke_count,
                frequency,
                kunyomi,
                onyomi,
                meanings_kanji,
                radicals
            FROM kanji_results

            ORDER BY 
                relevance_score DESC,
                entry_type_order ASC,
                frequency ASC NULLS LAST
            LIMIT {2} OFFSET {3};
        """;
        var response = new GlobalSearchResponse();
        response.KanjiResults = new();
        response.VocabularyResults = new();
        response.ProperNounResults = new();
        return response;
    }

    public async Task<SearchResultKanji> SearchKanjiAsync(string query, int page, int pageSize, double relevanceThreshold = ISearchRepository.MIN_RELEVANCE_SCORE)
    {
        var sql = @"
          WITH search_matches AS (
            -- Search by literal (exact match has highest priority)
            SELECT DISTINCT
                k.id as kanji_id,
                k.literal as match_text,
                'literal' as match_type,
                CASE 
                    WHEN k.literal = @query THEN 1.0
                    ELSE similarity(k.literal, @query)
                END as relevance
            FROM jlpt.kanji k
            WHERE k.literal = @query OR k.literal % @query
            
            UNION ALL
            
            -- Search in readings (kunyomi and onyomi)
            SELECT DISTINCT
                k.id as kanji_id,
                kr.value as match_text,
                'reading' as match_type,
                similarity(kr.value, @query) as relevance
            FROM jlpt.kanji k
            JOIN jlpt.kanji_reading kr ON k.id = kr.kanji_id
            WHERE kr.value % @query
            
            UNION ALL
            
            -- Search in meanings
            SELECT DISTINCT
                k.id as kanji_id,
                km.value as match_text,
                'meaning' as match_type,
                similarity(km.value, @query) as relevance
            FROM jlpt.kanji k
            JOIN jlpt.kanji_meaning km ON k.id = km.kanji_id
            WHERE km.value % @query
        ),
        ranked_matches AS (
            SELECT 
                kanji_id,
                MAX(relevance) as max_relevance
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
                rm.max_relevance as relevance_score,
                
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
            id,
            literal,
            grade,
            stroke_count,
            frequency,
            jlpt_level_new,
            relevance_score,
            kunyomi,
            onyomi,
            meanings,
            radicals
        FROM kanji_data
        WHERE relevance_score >= @relevanceThreshold
        ORDER BY relevance_score DESC, frequency ASC NULLS LAST
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
        LIMIT @pageSize OFFSET @offset;
        ";
        
        var results = new List<KanjiSummaryDto>();
        var totalCount = 0;

        await using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter("@query", query));
        command.Parameters.Add(new NpgsqlParameter("@pageSize", pageSize));
        command.Parameters.Add(new NpgsqlParameter("@offset", (page - 1) * pageSize));
        command.Parameters.Add(new NpgsqlParameter("@relevanceThreshold", relevanceThreshold));

        await using var reader = await command.ExecuteReaderAsync();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        while (await reader.ReadAsync())
        {
            totalCount = reader.GetInt32(11);
            var result = new KanjiSummaryDto
            {
                Id = reader.GetGuid(0),
                Literal = reader.GetString(1),
                Grade = await reader.IsDBNullAsync(2) ? null : reader.GetInt32(2),
                StrokeCount = reader.GetInt32(3),
                Frequency = await reader.IsDBNullAsync(4) ? null : reader.GetInt32(4),
                JlptLevel = await reader.IsDBNullAsync(5) ? null : reader.GetInt32(5),
                RelevanceScore = reader.GetDouble(6),
                KunyomiReadings = await reader.IsDBNullAsync(7) ? null :
                    JsonSerializer.Deserialize<List<KanjiReadingDto>>(reader.GetString(7), jsonOptions),
                OnyomiReadings = await reader.IsDBNullAsync(8) ? null :
                    JsonSerializer.Deserialize<List<KanjiReadingDto>>(reader.GetString(8), jsonOptions),
                Meanings = await reader.IsDBNullAsync(9) ? null :
                    JsonSerializer.Deserialize<List<KanjiMeaningDto>>(reader.GetString(9), jsonOptions),
                Radicals = await reader.IsDBNullAsync(10) ? null :
                    JsonSerializer.Deserialize<List<RadicalSummaryDto>>(reader.GetString(10), jsonOptions),
            };
            results.Add(result);
        }

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultKanji
        {
            Data = results,
            Pagination = new PaginationMetadata
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
            }
        };

    }

    public async Task<SearchResultProperNoun> SearchProperNounAsync(string query, int page, int pageSize, double relevanceThreshold = ISearchRepository.MIN_RELEVANCE_SCORE)
    {
        var sql = @"
            WITH search_matches AS (
            -- Search in kanji
            SELECT DISTINCT
                pn.id as proper_noun_id,
                pnk.text as match_text,
                'kanji' as match_type,
                similarity(pnk.text, @query) as relevance
            FROM jlpt.proper_noun pn
            JOIN jlpt.proper_noun_kanji pnk ON pn.id = pnk.proper_noun_id
            WHERE pnk.text % @query
            
            UNION ALL
            
            -- Search in kana
            SELECT DISTINCT
                pn.id as proper_noun_id,
                pnkn.text as match_text,
                'kana' as match_type,
                similarity(pnkn.text, @query) as relevance
            FROM jlpt.proper_noun pn
            JOIN jlpt.proper_noun_kana pnkn ON pn.id = pnkn.proper_noun_id
            WHERE pnkn.text % @query
            
            UNION ALL
            
            -- Search in translations
            SELECT DISTINCT
                pn.id as proper_noun_id,
                pntt.text as match_text,
                'translation' as match_type,
                similarity(pntt.text, @query) as relevance
            FROM jlpt.proper_noun pn
            JOIN jlpt.proper_noun_translation pnt ON pn.id = pnt.proper_noun_id
            JOIN jlpt.proper_noun_translation_text pntt ON pnt.id = pntt.translation_id
            WHERE pntt.text % @query
        ),
        ranked_matches AS (
            SELECT 
                proper_noun_id,
                MAX(relevance) as max_relevance
            FROM search_matches
            GROUP BY proper_noun_id
        ),
        proper_noun_data AS (
            SELECT
                pn.id,
                pn.jmnedict_id as dict_id,
                rm.max_relevance as relevance_score,
                
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
                                    'lang', pnttxt.lang,
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
                id,
                dict_id,
                relevance_score,
                primary_kanji,
                primary_kana,
                other_kanji_forms,
                other_kana_forms,
                translations
            FROM proper_noun_data
            WHERE relevance_score >= @relevanceThreshold
            ORDER BY relevance_score DESC
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
        LIMIT @pageSize OFFSET @offset;
        ";
        
        var results = new List<ProperNounSummaryDto>();
        var totalCount = 0;

        await using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter("@query", query));
        command.Parameters.Add(new NpgsqlParameter("@pageSize", pageSize));
        command.Parameters.Add(new NpgsqlParameter("@offset", (page - 1) * pageSize));
        command.Parameters.Add(new NpgsqlParameter("@relevanceThreshold", relevanceThreshold));

        await using var reader = await command.ExecuteReaderAsync();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        while (await reader.ReadAsync())
        {
            totalCount = reader.GetInt32(8);
            var result = new ProperNounSummaryDto
            {
                Id = reader.GetGuid(0),
                DictionaryId = reader.GetString(1),
                RelevanceScore = reader.GetDouble(2),
                PrimaryKanji = await reader.IsDBNullAsync(3) ? null :
                    JsonSerializer.Deserialize<DTOs.ProperNoun.KanjiFormDto>(reader.GetString(3), jsonOptions),
                PrimaryKana = await reader.IsDBNullAsync(4) ? null :
                    JsonSerializer.Deserialize<DTOs.ProperNoun.KanaFormDto>(reader.GetString(4), jsonOptions),
                OtherKanjiForms = await reader.IsDBNullAsync(5) ? null :
                    JsonSerializer.Deserialize<List<DTOs.ProperNoun.KanjiFormDto>>(reader.GetString(5), jsonOptions),
                OtherKanaForms = await reader.IsDBNullAsync(6) ? null :
                    JsonSerializer.Deserialize<List<DTOs.ProperNoun.KanaFormDto>>(reader.GetString(6), jsonOptions),
                Translations = await reader.IsDBNullAsync(7) ? null :
                    JsonSerializer.Deserialize<List<TranslationSummaryDto>>(reader.GetString(7), jsonOptions),
            };
            results.Add(result);
        }

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultProperNoun
        {
            Data = results,
            Pagination = new PaginationMetadata
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
            }
        };
    }

    public async Task<SearchResultVocabulary> SearchVocabularyAsync(
        string query, 
        int page, 
        int pageSize, 
        double relevanceThreshold = ISearchRepository.MIN_RELEVANCE_SCORE
    )
    {
        var sql = @"
            WITH search_matches AS (
                -- Search in kanji
                SELECT DISTINCT
                    v.id as vocabulary_id,
                    vk.text as match_text,
                    'kanji' as match_type,
                    similarity(vk.text, @query) as relevance
                FROM jlpt.vocabulary v
                JOIN jlpt.vocabulary_kanji vk ON v.id = vk.vocabulary_id
                WHERE vk.text % @query  -- % is the similarity operator
                
                UNION ALL
                
                -- Search in kana
                SELECT DISTINCT
                    v.id as vocabulary_id,
                    vkn.text as match_text,
                    'kana' as match_type,
                    similarity(vkn.text, @query) as relevance
                FROM jlpt.vocabulary v
                JOIN jlpt.vocabulary_kana vkn ON v.id = vkn.vocabulary_id
                WHERE vkn.text % @query
                
                UNION ALL
                
                -- Search in glosses
                SELECT DISTINCT
                    v.id as vocabulary_id,
                    vsg.text as match_text,
                    'gloss' as match_type,
                    similarity(vsg.text, @query) as relevance
                FROM jlpt.vocabulary v
                JOIN jlpt.vocabulary_sense vs ON v.id = vs.vocabulary_id
                JOIN jlpt.vocabulary_sense_gloss vsg ON vs.id = vsg.sense_id
                WHERE vsg.text % @query
            ),
            ranked_matches AS (
                SELECT 
                    vocabulary_id,
                    MAX(relevance) as max_relevance
                FROM search_matches
                GROUP BY vocabulary_id
            ),
            vocabulary_data AS (
                SELECT
                    v.id,
                    v.jmdict_id as dict_id,
                    v.jlpt_level_new as jlpt_level,
                    rm.max_relevance as relevance_score,
                    
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
                    ) as senses
                    
                FROM jlpt.vocabulary v
                JOIN ranked_matches rm ON v.id = rm.vocabulary_id
            ),
            all_results AS (
                SELECT 
                    id,
                    dict_id,
                    jlpt_level,
                    relevance_score,
                    primary_kanji,
                    primary_kana,
                    other_kanji_forms,
                    other_kana_forms,
                    senses,
                    CASE 
                        WHEN (primary_kanji->>'is_common')::boolean = true 
                            OR (primary_kana->>'is_common')::boolean = true 
                        THEN true 
                        ELSE false 
                    END as is_common
                FROM vocabulary_data
                WHERE relevance_score >= @relevanceThreshold
                ORDER BY relevance_score DESC
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
            LIMIT @pageSize OFFSET @offset";

        var results = new List<VocabularySummaryDto>();
        var totalCount = 0;

        await using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter("@query", query));
        command.Parameters.Add(new NpgsqlParameter("@pageSize", pageSize));
        command.Parameters.Add(new NpgsqlParameter("@offset", (page - 1) * pageSize));
        command.Parameters.Add(new NpgsqlParameter("@relevanceThreshold", relevanceThreshold));

        await using var reader = await command.ExecuteReaderAsync();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        while (await reader.ReadAsync())
        {
            totalCount = reader.GetInt32(10);
            var result = new VocabularySummaryDto
            {
                Id = reader.GetGuid(0),
                DictionaryId = reader.GetString(1),
                JlptLevel = await reader.IsDBNullAsync(2) ? null : reader.GetInt32(2),
                RelevanceScore = reader.GetDouble(3),
                PrimaryKanji = await reader.IsDBNullAsync(4) ? null :
                    JsonSerializer.Deserialize<DTOs.Vocabulary.KanjiFormDto>(reader.GetString(4), jsonOptions),
                PrimaryKana = await reader.IsDBNullAsync(5) ? null :
                    JsonSerializer.Deserialize<DTOs.Vocabulary.KanaFormDto>(reader.GetString(5), jsonOptions),
                OtherKanjiForms = await reader.IsDBNullAsync(6) ? null :
                    JsonSerializer.Deserialize<List<DTOs.Vocabulary.KanjiFormDto>>(reader.GetString(6), jsonOptions),
                OtherKanaForms = await reader.IsDBNullAsync(7) ? null :
                    JsonSerializer.Deserialize<List<DTOs.Vocabulary.KanaFormDto>>(reader.GetString(7), jsonOptions),
                Senses = await reader.IsDBNullAsync(8) ? null :
                    JsonSerializer.Deserialize<List<SenseSummaryDto>>(reader.GetString(8), jsonOptions),
                IsCommon = reader.GetBoolean(9)
            };
            results.Add(result);
        }

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultVocabulary
        {
            Data = results,
            Pagination = new PaginationMetadata
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
            }
        };
    }
}