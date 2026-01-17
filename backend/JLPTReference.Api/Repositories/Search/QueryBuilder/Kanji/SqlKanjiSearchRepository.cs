using System.Text.Json;
using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Radical;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Repositories.Search.Ranking;
using Npgsql;
using NpgsqlTypes;

namespace JLPTReference.Api.Repositories.Search.QueryBuilder;

public class SqlKanjiSearchRepository : IKanjiSearchRepository
{
    private readonly string _connectionString;
    private readonly IKanjiRanker _ranker;
    private readonly KanjiRankingProfile _rankingProfile;
    private readonly JsonSerializerOptions _jsonOptions;

    public SqlKanjiSearchRepository(
        IConfiguration configuration,
        IKanjiRanker ranker,
        KanjiRankingProfile rankingProfile)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        _ranker = ranker;
        _rankingProfile = rankingProfile;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<SearchResultKanji> SearchAsync(SearchSpec spec, int pageSize, int page)
    {
        // Build per-token patterns for global AND matching across fields
        var patternsPerToken = SearchPatternUtils.GetPatternsPerToken(spec.Tokens);
        var tokenVariantCounts = SearchPatternUtils.GetTokenVariantCounts(spec.Tokens);
        var combinedPatterns = SearchPatternUtils.GetCombinedPatterns(spec.Tokens);

        var hasWildcard = spec.Tokens?.Any(t => t.HasWildcard) ?? false;

        // Flatten patterns for exact term extraction
        var allPatterns = patternsPerToken.SelectMany(p => p).ToList();
        var exactTerms = allPatterns
            .Select(p => p.TrimEnd('%'))
            .Where(p => !string.IsNullOrEmpty(p) && !hasWildcard)
            .Select(SearchPatternUtils.UnescapeLikePattern)
            .Distinct()
            .ToArray();

        var filters = spec.Filters ?? new SearchFilters();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
            SELECT * FROM jlpt.search_kanji_ranked(
                @patterns,
                @tokenVariantCounts,
                @combinedPatterns,
                @exactTerms,
                @hasWildcard,
                @jlptMin,
                @jlptMax,
                @gradeMin,
                @gradeMax,
                @strokeMin,
                @strokeMax,
                @freqMin,
                @freqMax,
                @langs,
                @pageSize,
                @pageOffset
            )", connection);

        // Add parameters
        cmd.Parameters.Add(new NpgsqlParameter("@patterns", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = allPatterns.Count > 0 ? allPatterns.ToArray() : DBNull.Value
        });
        cmd.Parameters.Add(new NpgsqlParameter("@tokenVariantCounts", NpgsqlDbType.Array | NpgsqlDbType.Integer)
        {
            Value = tokenVariantCounts.Length > 0 ? tokenVariantCounts : DBNull.Value
        });
        cmd.Parameters.Add(new NpgsqlParameter("@combinedPatterns", NpgsqlDbType.Array | NpgsqlDbType.Text) 
        { 
            Value = combinedPatterns.Count > 0 ? combinedPatterns.ToArray() : DBNull.Value 
        });
        cmd.Parameters.Add(new NpgsqlParameter("@exactTerms", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = exactTerms.Length > 0 ? exactTerms : DBNull.Value
        });
        cmd.Parameters.AddWithValue("@hasWildcard", hasWildcard);
        cmd.Parameters.AddWithValue("@jlptMin", filters.JlptLevels?.Min ?? 0);
        cmd.Parameters.AddWithValue("@jlptMax", filters.JlptLevels?.Max ?? 0);
        cmd.Parameters.AddWithValue("@gradeMin", filters.Grades?.Min ?? 0);
        cmd.Parameters.AddWithValue("@gradeMax", filters.Grades?.Max ?? 0);
        cmd.Parameters.AddWithValue("@strokeMin", filters.StrokeCount?.Min ?? 0);
        cmd.Parameters.AddWithValue("@strokeMax", filters.StrokeCount?.Max ?? 0);
        cmd.Parameters.AddWithValue("@freqMin", filters.Frequency?.Min ?? 0);
        cmd.Parameters.AddWithValue("@freqMax", filters.Frequency?.Max ?? 0);
        cmd.Parameters.Add(new NpgsqlParameter("@langs", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = filters.Languages?.Count > 0 ? filters.Languages.ToArray() : DBNull.Value
        });
        cmd.Parameters.AddWithValue("@pageSize", pageSize);
        cmd.Parameters.AddWithValue("@pageOffset", (page - 1) * pageSize);

        var results = new List<KanjiSummaryDto>();
        long totalCount = 0;

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var dto = new KanjiSummaryDto
            {
                Id = reader.GetGuid(0),
                Literal = reader.GetString(1),
                Grade = await reader.IsDBNullAsync(2) ? null : reader.GetInt32(2),
                StrokeCount = reader.GetInt32(3),
                Frequency = await reader.IsDBNullAsync(4) ? null : reader.GetInt32(4),
                JlptLevel = await reader.IsDBNullAsync(5) ? null : reader.GetInt32(5),
                RelevanceScore = 0
            };

            var matchQuality = reader.GetInt32(6);
            var matchLocation = reader.GetInt32(7);
            var matchedTextLength = reader.GetInt32(8);

            // Arrays for additional data
            var allReadings = await reader.IsDBNullAsync(9) ? Array.Empty<string>() : (string[])reader.GetValue(9);
            var allMeanings = await reader.IsDBNullAsync(10) ? Array.Empty<string>() : (string[])reader.GetValue(10);

            // JSON columns
            dto.KunyomiReadings = await ReadJsonColumn<List<KanjiReadingDto>>(reader, 11) ?? new List<KanjiReadingDto>();
            dto.OnyomiReadings = await ReadJsonColumn<List<KanjiReadingDto>>(reader, 12) ?? new List<KanjiReadingDto>();
            dto.Meanings = await ReadJsonColumn<List<KanjiMeaningDto>>(reader, 13) ?? new List<KanjiMeaningDto>();
            dto.Radicals = await ReadJsonColumn<List<RadicalSummaryDto>>(reader, 14) ?? new List<RadicalSummaryDto>();

            totalCount = reader.GetInt64(15);

            // Compute ranking score
            var matchInfo = new KanjiMatchInfo
            {
                KanjiId = dto.Id,
                BestMatchQuality = (MatchQuality)matchQuality,
                MatchLocations = (MatchLocation)matchLocation,
                MatchedTextLength = matchedTextLength,
                Frequency = dto.Frequency,
                JlptLevel = dto.JlptLevel,
                Grade = dto.Grade
            };

            _ranker.ComputeScores(new[] { matchInfo }, _rankingProfile);
            dto.RelevanceScore = matchInfo.RelevanceScore;

            results.Add(dto);
        }

        results = results.OrderByDescending(r => r.RelevanceScore).ToList();

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        return new SearchResultKanji
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

