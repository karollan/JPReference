using System.Text.Json;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.Services.Search.Ranking;
using Npgsql;
using NpgsqlTypes;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

/// <summary>
/// SQL-based vocabulary search service using the optimized search_vocabulary_ranked function.
/// This is significantly faster than the EF Core implementation for large datasets.
/// </summary>
public class SqlVocabularySearchService : IVocabularySearchService
{
    private readonly string _connectionString;
    private readonly IVocabularyRanker _ranker;
    private readonly VocabularyRankingProfile _rankingProfile;
    private readonly JsonSerializerOptions _jsonOptions;

    public SqlVocabularySearchService(
        IConfiguration configuration,
        IVocabularyRanker ranker,
        VocabularyRankingProfile rankingProfile)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        _ranker = ranker;
        _rankingProfile = rankingProfile;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<VocabularySearchResult> SearchAsync(SearchSpec spec, int pageSize, int page)
    {
        // Build parameters from SearchSpec
        var patterns = SearchPatternUtils.GetPatterns(spec.Tokens);
        var hasWildcard = spec.Tokens?.Any(t => t.HasWildcard) ?? false;
        
        var exactTerms = patterns
            .Select(p => p.TrimEnd('%'))
            .Where(p => !string.IsNullOrEmpty(p) && !hasWildcard)
            .Select(SearchPatternUtils.UnescapeLikePattern)
            .ToArray();

        var filters = spec.Filters ?? new SearchFilters();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM jlpt.search_vocabulary_ranked(
                @patterns,
                @exactTerms,
                @hasWildcard,
                @jlptMin,
                @jlptMax,
                @commonOnly,
                @filterTags,
                @langs,
                @pageSize,
                @pageOffset
            )", connection);

        // Add parameters
        cmd.Parameters.Add(new NpgsqlParameter("@patterns", NpgsqlDbType.Array | NpgsqlDbType.Text) 
        { 
            Value = patterns.Count > 0 ? patterns.ToArray() : DBNull.Value 
        });
        cmd.Parameters.Add(new NpgsqlParameter("@exactTerms", NpgsqlDbType.Array | NpgsqlDbType.Text) 
        { 
            Value = exactTerms.Length > 0 ? exactTerms : DBNull.Value 
        });
        cmd.Parameters.AddWithValue("@hasWildcard", hasWildcard);
        cmd.Parameters.AddWithValue("@jlptMin", filters.JlptLevels?.Min ?? 0);
        cmd.Parameters.AddWithValue("@jlptMax", filters.JlptLevels?.Max ?? 0);
        cmd.Parameters.AddWithValue("@commonOnly", filters.CommonOnly ?? false);
        cmd.Parameters.Add(new NpgsqlParameter("@filterTags", NpgsqlDbType.Array | NpgsqlDbType.Text) 
        { 
            Value = filters.Tags?.Count > 0 ? filters.Tags.ToArray() : DBNull.Value 
        });
        cmd.Parameters.Add(new NpgsqlParameter("@langs", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = filters.Languages?.Count > 0 ? filters.Languages.ToArray() : DBNull.Value
        });
        cmd.Parameters.AddWithValue("@pageSize", pageSize);
        cmd.Parameters.AddWithValue("@pageOffset", (page - 1) * pageSize);

        var results = new List<VocabularySummaryDto>();
        long totalCount = 0;

        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var dto = new VocabularySummaryDto
            {
                Id = reader.GetGuid(0),
                DictionaryId = reader.GetString(1),
                JlptLevel = await reader.IsDBNullAsync(2) ? null : reader.GetInt32(2),
                IsCommon = reader.GetBoolean(3),
                RelevanceScore = 0 // Will be computed below
            };

            // Get match info for ranking
            var matchQuality = reader.GetInt32(4);
            var matchLocation = reader.GetInt32(5);
            var matchedTextLength = reader.GetInt32(6);
            var senseCount = reader.GetInt32(7);

            // Arrays for additional ranking (if needed)
            var allKanaTexts = await reader.IsDBNullAsync(8) ? Array.Empty<string>() : (string[])reader.GetValue(8);
            var allKanjiTexts = await reader.IsDBNullAsync(9) ? Array.Empty<string>() : (string[])reader.GetValue(9);
            var firstSenseGlosses = await reader.IsDBNullAsync(10) ? Array.Empty<string>() : (string[])reader.GetValue(10);
            var allGlosses = await reader.IsDBNullAsync(11) ? Array.Empty<string>() : (string[])reader.GetValue(11);

            // JSON columns for display
            dto.PrimaryKanji = await ReadJsonColumn<KanjiFormDto>(reader, 12);
            dto.PrimaryKana = await ReadJsonColumn<KanaFormDto>(reader, 13);
            dto.OtherKanjiForms = await ReadJsonColumn<List<KanjiFormDto>>(reader, 14) ?? new List<KanjiFormDto>();
            dto.OtherKanaForms = await ReadJsonColumn<List<KanaFormDto>>(reader, 15) ?? new List<KanaFormDto>();
            dto.Senses = await ReadJsonColumn<List<SenseSummaryDto>>(reader, 16) ?? new List<SenseSummaryDto>();
            dto.Slug = await reader.IsDBNullAsync(17) ? null : reader.GetString(17);

            totalCount = reader.GetInt64(18);

            // Compute final ranking score using the ranker
            var matchInfo = new VocabularyMatchInfo
            {
                VocabularyId = dto.Id,
                BestMatchQuality = (MatchQuality)matchQuality,
                MatchLocations = (MatchLocation)matchLocation,
                MatchedTextLength = matchedTextLength,
                IsCommon = dto.IsCommon,
                JlptLevel = dto.JlptLevel,
                SenseCount = senseCount
            };

            _ranker.ComputeScores(new[] { matchInfo }, _rankingProfile);
            dto.RelevanceScore = matchInfo.RelevanceScore;

            results.Add(dto);
        }

        // Sort by computed score (maintains order if scores are equal due to SQL ordering)
        results = results.OrderByDescending(r => r.RelevanceScore).ToList();

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new VocabularySearchResult
        {
            Data = results,
            Pagination = new PaginationMetadata
            {
                TotalCount = (int)totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            }
        };
    }

    private async Task<T?> ReadJsonColumn<T>(NpgsqlDataReader reader, int ordinal) where T : class
    {
        if (await reader.IsDBNullAsync(ordinal))
            return null;
        
        var json = reader.GetString(ordinal);
        if (string.IsNullOrEmpty(json))
            return null;
            
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }
}

