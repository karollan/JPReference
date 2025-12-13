using System.Text.Json;
using JLPTReference.Api.DTOs.ProperNoun;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.Services.Search.Ranking;
using Npgsql;
using NpgsqlTypes;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

public class SqlProperNounSearchService : IProperNounSearchService
{
    private readonly string _connectionString;
    private readonly IProperNounRanker _ranker;
    private readonly ProperNounRankingProfile _rankingProfile;
    private readonly JsonSerializerOptions _jsonOptions;

    public SqlProperNounSearchService(
        IConfiguration configuration,
        IProperNounRanker ranker,
        ProperNounRankingProfile rankingProfile)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        _ranker = ranker;
        _rankingProfile = rankingProfile;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<SearchResultProperNoun> SearchAsync(SearchSpec spec, int pageSize, int page)
    {
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
            SELECT * FROM jlpt.search_proper_noun_ranked(
                @patterns,
                @exactTerms,
                @hasWildcard,
                @filterTags,
                @pageSize,
                @pageOffset
            )", connection);

        cmd.Parameters.Add(new NpgsqlParameter("@patterns", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = patterns.Count > 0 ? patterns.ToArray() : DBNull.Value
        });
        cmd.Parameters.Add(new NpgsqlParameter("@exactTerms", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = exactTerms.Length > 0 ? exactTerms : DBNull.Value
        });
        cmd.Parameters.AddWithValue("@hasWildcard", hasWildcard);
        cmd.Parameters.Add(new NpgsqlParameter("@filterTags", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = filters.Tags?.Count > 0 ? filters.Tags.ToArray() : DBNull.Value
        });
        cmd.Parameters.AddWithValue("@pageSize", pageSize);
        cmd.Parameters.AddWithValue("@pageOffset", (page - 1) * pageSize);

        var results = new List<ProperNounSummaryDto>();
        long totalCount = 0;

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var dto = new ProperNounSummaryDto
            {
                Id = reader.GetGuid(0),
                DictionaryId = reader.GetString(1),
                RelevanceScore = 0
            };

            var matchQuality = reader.GetInt32(2);
            var matchLocation = reader.GetInt32(3);
            var matchedTextLength = reader.GetInt32(4);

            // Arrays for display
            var allKanjiTexts = await reader.IsDBNullAsync(5) ? Array.Empty<string>() : (string[])reader.GetValue(5);
            var allKanaTexts = await reader.IsDBNullAsync(6) ? Array.Empty<string>() : (string[])reader.GetValue(6);
            var allTranslationTexts = await reader.IsDBNullAsync(7) ? Array.Empty<string>() : (string[])reader.GetValue(7);

            // JSON columns
            dto.PrimaryKanji = await ReadJsonColumn<DTOs.ProperNoun.KanjiFormDto>(reader, 8);
            dto.PrimaryKana = await ReadJsonColumn<DTOs.ProperNoun.KanaFormDto>(reader, 9);
            dto.OtherKanjiForms = await ReadJsonColumn<List<DTOs.ProperNoun.KanjiFormDto>>(reader, 10) ?? new List<DTOs.ProperNoun.KanjiFormDto>();
            dto.OtherKanaForms = await ReadJsonColumn<List<DTOs.ProperNoun.KanaFormDto>>(reader, 11) ?? new List<DTOs.ProperNoun.KanaFormDto>();
            dto.Translations = await ReadJsonColumn<List<TranslationSummaryDto>>(reader, 12) ?? new List<TranslationSummaryDto>();

            totalCount = reader.GetInt64(13);

            // Compute ranking score
            var matchInfo = new ProperNounMatchInfo
            {
                ProperNounId = dto.Id,
                BestMatchQuality = (MatchQuality)matchQuality,
                MatchLocations = (MatchLocation)matchLocation,
                MatchedTextLength = matchedTextLength
            };

            _ranker.ComputeScores(new[] { matchInfo }, _rankingProfile);
            dto.RelevanceScore = matchInfo.RelevanceScore;

            results.Add(dto);
        }

        results = results.OrderByDescending(r => r.RelevanceScore).ToList();

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultProperNoun
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
