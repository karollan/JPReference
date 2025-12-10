using System.Text.Json;

using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Entities.Kanji;
using JLPTReference.Api.Entities.ProperNoun;
using JLPTReference.Api.Entities.Vocabulary;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.DTOs.ProperNoun;
using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Radical;
using JLPTReference.Api.Repositories.Interfaces;
using JLPTReference.Api.Services.Search.QueryBuilder;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using JLPTReference.Api.Data;
namespace JLPTReference.Api.Repositories.Implementations;

public class SearchRepository : ISearchRepository
{
    private readonly string _connectionString;
    private readonly ISearchQueryBuilder<Kanji> _kanjiQueryBuilder;
    private readonly ISearchQueryBuilder<ProperNoun> _properNounQueryBuilder;
    private readonly ISearchQueryBuilder<Vocabulary> _vocabularyQueryBuilder;
    private readonly IDbContextFactory<ApplicationDBContext> _contextFactory;
    public SearchRepository(
        IConfiguration configuration,
        ISearchQueryBuilder<Kanji> kanjiQueryBuilder,
        ISearchQueryBuilder<ProperNoun> properNounQueryBuilder,
        ISearchQueryBuilder<Vocabulary> vocabularyQueryBuilder,
        IDbContextFactory<ApplicationDBContext> contextFactory
    )
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        _kanjiQueryBuilder = kanjiQueryBuilder;
        _properNounQueryBuilder = properNounQueryBuilder;
        _vocabularyQueryBuilder = vocabularyQueryBuilder;
        _contextFactory = contextFactory;
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
        await using var context = _contextFactory.CreateDbContext();
        var baseQuery = context.Kanji
            .Include(k => k.Readings)
            .Include(k => k.Meanings)
            .Include(k => k.Radicals)
            .ThenInclude(r => r.Radical)
            .AsQueryable();
        var query = _kanjiQueryBuilder.BuildQuery(
            baseQuery,
            spec
        );
        var totalCount = await query.CountAsync();

        query = query.Skip((page - 1) * pageSize).Take(pageSize);

        var results = await query.ToListAsync();
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

    public async Task<SearchResultProperNoun> SearchProperNounAsync(SearchSpec spec, int pageSize, int page)
    {
        await using var context = _contextFactory.CreateDbContext();
        var baseQuery = context.ProperNoun
            .Include(p => p.KanjiForms)
            .ThenInclude(k => k.Tags)
            .Include(p => p.KanaForms)
            .ThenInclude(k => k.Tags)
            .Include(p => p.Translations)
            .ThenInclude(t => t.Types)
            .Include(p => p.Translations)
            .ThenInclude(t => t.Texts)
            .Include(p => p.Translations)
            .ThenInclude(t => t.RelatedTerms)
            .AsQueryable();
        var query = _properNounQueryBuilder.BuildQuery(
            baseQuery,
            spec
        );
        var totalCount = await query.CountAsync();

        query = query.Skip((page - 1) * pageSize).Take(pageSize);

        var results = await query.ToListAsync();
        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultProperNoun
        {
            Data = results.Select(r => new ProperNounSummaryDto
            {
                Id = r.Id,
                DictionaryId = r.JmnedictId,
                RelevanceScore = 0,
                PrimaryKanji = new DTOs.ProperNoun.KanjiFormDto
                {
                    Text = r.KanjiForms.FirstOrDefault()?.Text ?? string.Empty,
                    Tags = r.KanjiForms.FirstOrDefault()?.Tags.Select(t => new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "proper_noun" }).ToList() ?? new List<TagInfoDto>(),
                },
                PrimaryKana = new DTOs.ProperNoun.KanaFormDto
                {
                    Text = r.KanaForms.FirstOrDefault()?.Text ?? string.Empty,
                    Tags = r.KanaForms.FirstOrDefault()?.Tags.Select(t => new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "proper_noun" }).ToList() ?? new List<TagInfoDto>(),
                },
                OtherKanjiForms = r.KanjiForms.Skip(1).Select(k => new DTOs.ProperNoun.KanjiFormDto
                {
                    Text = k.Text,
                    Tags = k.Tags.Select(t => new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "proper_noun" }).ToList(),
                }).ToList(),
                OtherKanaForms = r.KanaForms.Skip(1).Select(k => new DTOs.ProperNoun.KanaFormDto
                {
                    Text = k.Text,
                    Tags = k.Tags.Select(t => new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "proper_noun" }).ToList(),
                }).ToList(),
                Translations = r.Translations.Select(t => new TranslationSummaryDto
                {
                    Types = t.Types.Select(t => new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "proper_noun" }).ToList(),
                    Translations = t.Texts.Select(t => new TranslationTextDto { Language = t.Lang, Text = t.Text }).ToList(),
                }).ToList(),
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

    public async Task<SearchResultVocabulary> SearchVocabularyAsync(SearchSpec spec, int pageSize, int page)
    {
        await using var context = _contextFactory.CreateDbContext();
        var baseQuery = context.Vocabulary
            .Include(v => v.Kana)
            .ThenInclude(k => k.Tags)
            .Include(v => v.Kanji)
            .ThenInclude(k => k.Tags)
            .Include(v => v.Senses)
            .ThenInclude(s => s.Tags)
            .Include(v => v.Senses)
            .ThenInclude(s => s.Relations)
            .Include(v => v.Senses)
            .ThenInclude(s => s.LanguageSources)
            .Include(v => v.Senses)
            .ThenInclude(s => s.Glosses)
            .AsQueryable();
        var query = _vocabularyQueryBuilder.BuildQuery(
            baseQuery,
            spec
        );
        var totalCount = await query.CountAsync();

        query = query.Skip((page - 1) * pageSize).Take(pageSize);

        var results = await query.ToListAsync();
        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultVocabulary
        {
            Data = results.Select(r => new VocabularySummaryDto
            {
                Id = r.Id,
                DictionaryId = r.JmdictId,
                JlptLevel = r.JlptLevelNew ?? null,
                RelevanceScore = 0,
                PrimaryKanji = new DTOs.Vocabulary.KanjiFormDto
                {
                    Text = r.Kanji.FirstOrDefault()?.Text ?? string.Empty,
                    Tags = r.Kanji.FirstOrDefault()?.Tags.Select(t => new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "vocabulary" }).ToList() ?? new List<TagInfoDto>(),
                },
                PrimaryKana = new DTOs.Vocabulary.KanaFormDto
                {
                    Text = r.Kana.FirstOrDefault()?.Text ?? string.Empty,
                    Tags = r.Kana.FirstOrDefault()?.Tags.Select(t =>new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "vocabulary" }).ToList() ?? new List<TagInfoDto>(),
                },
                OtherKanjiForms = r.Kanji.Skip(1).Select(k => new DTOs.Vocabulary.KanjiFormDto
                {
                    Text = k.Text,
                    Tags = k.Tags.Select(t => new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "vocabulary" }).ToList(),
                }).ToList(),
                OtherKanaForms = r.Kana.Skip(1).Select(k => new DTOs.Vocabulary.KanaFormDto
                {
                    Text = k.Text,
                    Tags = k.Tags.Select(t => new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "vocabulary" }).ToList(),
                }).ToList(),
                Senses = r.Senses.Take(3).Select(s => new SenseSummaryDto
                {
                    AppliesToKanji = s.AppliesToKanji?.ToList(),
                    AppliesToKana = s.AppliesToKana?.ToList(),
                    Info = s.Info?.ToList(),
                    Glosses = s.Glosses.Select(g => new SenseGlossDto { Language = g.Lang, Text = g.Text }).ToList(),
                    Tags = s.Tags.Select(t => new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "vocabulary" }).ToList(),
                }).ToList(),
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