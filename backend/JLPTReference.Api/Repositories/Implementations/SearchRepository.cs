using System.Text.Json;

using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Entities.Kanji;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.DTOs.ProperNoun;
using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Radical;
using JLPTReference.Api.Repositories.Interfaces;
using JLPTReference.Api.Services.Search.QueryBuilder;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using JLPTReference.Api.Data;
using Microsoft.EntityFrameworkCore.Storage.Internal;
namespace JLPTReference.Api.Repositories.Implementations;

public class SearchRepository : ISearchRepository
{
    private readonly string _connectionString;
    private readonly ISearchQueryBuilder<Kanji> _kanjiQueryBuilder;
    private readonly ApplicationDBContext _context;
    public SearchRepository(IConfiguration configuration, ISearchQueryBuilder<Kanji> kanjiQueryBuilder, ApplicationDBContext context)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        _kanjiQueryBuilder = kanjiQueryBuilder;
        _context = context;
    }

    public async Task<GlobalSearchResponse> SearchAllAsync(GlobalSearchRequest request)
    {        
        var kanjiTask = SearchKanjiAsync(request);
        var vocabTask = SearchVocabularyAsync(request);
        var properNounTask = SearchProperNounAsync(request);

        await Task.WhenAll(kanjiTask, vocabTask, properNounTask);

        return new GlobalSearchResponse
        {
            SearchedTerms = request.Queries,
            KanjiResults = kanjiTask.Result,
            VocabularyResults = vocabTask.Result,
            ProperNounResults = properNounTask.Result
        };
    }

    public async Task<SearchResultKanji> SearchKanjiAsync(SearchSpec spec, int pageSize, int page)
    {
        var baseQuery = _context.Kanji
            .Include(k => k.Readings)
            .Include(k => k.Meanings)
            .Include(k => k.Radicals)
            .ThenInclude(r => r.Radical)
            .AsQueryable();
        var query = _kanjiQueryBuilder.BuildQuery(
            baseQuery,
            spec
        );
        query = query.Skip((page - 1) * pageSize).Take(pageSize);

        var results = await query.ToListAsync();
        var totalCount = results.Count;
        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultKanji
        {
            Data = results.Select(r => new KanjiSummaryDto
            {
                Id = r.Id,
                Literal = r.Literal,
                Grade = r.Grade ?? null,
                StrokeCount = r.StrokeCount,
                Frequency = r.Frequency ?? null,
                JlptLevel = r.JlptLevelNew ?? null,
                RelevanceScore = 0,
                KunyomiReadings = r.Readings
                    .Where(k => r.Id == k.KanjiId && k.Type == "ja_kun")
                    .Select(k => new KanjiReadingDto {
                        Type = k.Type,
                        Value = k.Value,
                        Status = k.Status
                     }).ToList(),
                OnyomiReadings = r.Readings
                    .Where(k => r.Id == k.KanjiId && k.Type == "ja_on")
                    .Select(k => new KanjiReadingDto {
                        Type = k.Type,
                        Value = k.Value,
                        Status = k.Status,
                        OnType = k.OnType
                     }).ToList(),
                Meanings = r.Meanings
                    .Select(m => new KanjiMeaningDto { Meaning = m.Value, Language = m.Lang }).ToList(),
                Radicals = r.Radicals
                    .Select(r => new RadicalSummaryDto { Id = r.Id, Literal = r.Radical.Literal }).ToList(),
            }).ToList(),
            Pagination = new PaginationMetadata
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
            }
        };
    }

    public async Task<SearchResultKanji> SearchKanjiAsync(GlobalSearchRequest request)
    {
        
        var sql = @"
          SELECT * FROM jlpt.search_kanji_by_text(@queries, @relevanceThreshold, @pageSize, @offset);
        ";
        
        var results = new List<KanjiSummaryDto>();
        var totalCount = 0;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter("@queries", request.Queries.ToArray()));
        command.Parameters.Add(new NpgsqlParameter("@pageSize", request.PageSize));
        command.Parameters.Add(new NpgsqlParameter("@offset", (request.Page - 1) * request.PageSize));
        command.Parameters.Add(new NpgsqlParameter("@relevanceThreshold", request.RelevanceThreshold));

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

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return new SearchResultKanji
        {
            Data = results,
            Pagination = new PaginationMetadata
            {
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
            }
        };

    }

    public async Task<SearchResultProperNoun> SearchProperNounAsync(GlobalSearchRequest request)
    {
        var sql = @"
            SELECT * FROM jlpt.search_proper_noun_by_text(@queries, @relevanceThreshold, @pageSize, @offset);
        ";
        
        var results = new List<ProperNounSummaryDto>();
        var totalCount = 0;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter("@queries", request.Queries.ToArray()));
        command.Parameters.Add(new NpgsqlParameter("@pageSize", request.PageSize));
        command.Parameters.Add(new NpgsqlParameter("@offset", (request.Page - 1) * request.PageSize));
        command.Parameters.Add(new NpgsqlParameter("@relevanceThreshold", request.RelevanceThreshold));

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

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return new SearchResultProperNoun
        {
            Data = results,
            Pagination = new PaginationMetadata
            {
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
            }
        };
    }

    public async Task<SearchResultVocabulary> SearchVocabularyAsync(
        GlobalSearchRequest request
    )
    {
        var sql = @"
            SELECT * FROM jlpt.search_vocabulary_by_text(@queries, @relevanceThreshold, @pageSize, @offset);
        ";

        var results = new List<VocabularySummaryDto>();
        var totalCount = 0;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter("@queries", request.Queries.ToArray()));
        command.Parameters.Add(new NpgsqlParameter("@pageSize", request.PageSize));
        command.Parameters.Add(new NpgsqlParameter("@offset", (request.Page - 1) * request.PageSize));
        command.Parameters.Add(new NpgsqlParameter("@relevanceThreshold", request.RelevanceThreshold));

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

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return new SearchResultVocabulary
        {
            Data = results,
            Pagination = new PaginationMetadata
            {
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
            }
        };
    }
}