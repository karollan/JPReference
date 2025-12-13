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
            .AsNoTracking()
            .AsQueryable();

        var filteredQuery = _kanjiQueryBuilder.BuildQuery(baseQuery, spec);
        var totalCount = await filteredQuery.CountAsync();

        var pagedQuery = filteredQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var rows = await pagedQuery.Select(k => new
        {
            k.Id,
            k.Literal,
            k.Grade,
            k.StrokeCount,
            k.Frequency,
            k.JlptLevelNew,
            Kunyomi = k.Readings
                .Where(r => r.Type == "ja_kun")
                .OrderBy(r => r.Id)
                .Select(r => new { r.Id, r.Type, r.Value, r.Status, r.OnType })
                .ToList(),
            Onyomi = k.Readings
                .Where(r => r.Type == "ja_on")
                .OrderBy(r => r.Id)
                .Select(r => new { r.Id, r.Type, r.Value, r.Status, r.OnType })
                .ToList(),
            Meanings = k.Meanings
                .OrderBy(m => m.Id)
                .Select(m => new { m.Id, m.Lang, m.Value })
                .ToList(),
            Radicals = k.Radicals
                .OrderBy(r => r.Id)
                .Select(r => new { r.Id, Literal = r.Radical.Literal })
                .ToList()
        }).ToListAsync();
        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultKanji
        {
            Data = rows.Select(r => new KanjiSummaryDto
            {
                Id = r.Id,
                Literal = r.Literal,
                Grade = r.Grade ?? null,
                StrokeCount = r.StrokeCount,
                Frequency = r.Frequency ?? null,
                JlptLevel = r.JlptLevelNew ?? null,
                RelevanceScore = 0,
                KunyomiReadings = r.Kunyomi.Select(k => new KanjiReadingDto
                {
                    Id = k.Id,
                    Type = k.Type,
                    Value = k.Value,
                    Status = k.Status,
                    OnType = k.OnType
                }).ToList(),
                OnyomiReadings = r.Onyomi.Select(k => new KanjiReadingDto
                {
                    Id = k.Id,
                    Type = k.Type,
                    Value = k.Value,
                    Status = k.Status,
                    OnType = k.OnType
                }).ToList(),
                Meanings = r.Meanings.Select(m => new KanjiMeaningDto
                {
                    Id = m.Id,
                    Language = m.Lang,
                    Meaning = m.Value
                }).ToList(),
                Radicals = r.Radicals.Select(rad => new RadicalSummaryDto
                {
                    Id = rad.Id,
                    Literal = rad.Literal
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
            .AsNoTracking()
            .AsQueryable();

        var filteredQuery = _properNounQueryBuilder.BuildQuery(baseQuery, spec);
        var totalCount = await filteredQuery.CountAsync();

        var pagedQuery = filteredQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var rows = await pagedQuery.Select(p => new
        {
            p.Id,
            p.JmnedictId,
            PrimaryKanji = p.KanjiForms
                .OrderBy(k => k.CreatedAt)
                .ThenBy(k => k.Id)
                .Select(k => new
                {
                    k.Text,
                    Tags = k.Tags.Select(t => new TagInfoDto
                    {
                        Code = t.TagCode,
                        Description = t.Tag.Description,
                        Category = t.Tag.Category
                    }).ToList()
                })
                .FirstOrDefault(),
            PrimaryKana = p.KanaForms
                .OrderBy(k => k.CreatedAt)
                .ThenBy(k => k.Id)
                .Select(k => new
                {
                    k.Text,
                    k.AppliesToKanji,
                    Tags = k.Tags.Select(t => new TagInfoDto
                    {
                        Code = t.TagCode,
                        Description = t.Tag.Description,
                        Category = t.Tag.Category
                    }).ToList()
                })
                .FirstOrDefault(),
            OtherKanji = p.KanjiForms
                .OrderBy(k => k.CreatedAt)
                .ThenBy(k => k.Id)
                .Skip(1)
                .Select(k => new
                {
                    k.Text,
                    Tags = k.Tags.Select(t => new TagInfoDto
                    {
                        Code = t.TagCode,
                        Description = t.Tag.Description,
                        Category = t.Tag.Category
                    }).ToList()
                })
                .ToList(),
            OtherKana = p.KanaForms
                .OrderBy(k => k.CreatedAt)
                .ThenBy(k => k.Id)
                .Skip(1)
                .Select(k => new
                {
                    k.Text,
                    k.AppliesToKanji,
                    Tags = k.Tags.Select(t => new TagInfoDto
                    {
                        Code = t.TagCode,
                        Description = t.Tag.Description,
                        Category = t.Tag.Category
                    }).ToList()
                })
                .ToList(),
            Translations = p.Translations
                .OrderBy(t => t.Id)
                .Select(t => new
                {
                    Types = t.Types
                        .Select(tt => new TagInfoDto
                        {
                            Code = tt.TagCode,
                            Description = tt.Tag.Description,
                            Category = tt.Tag.Category
                        }).ToList(),
                    Texts = t.Texts.OrderBy(x => x.Id).Select(x => new { x.Lang, x.Text }).ToList()
                })
                .ToList()
        }).ToListAsync();
        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultProperNoun
        {
            Data = rows.Select(r => new ProperNounSummaryDto
            {
                Id = r.Id,
                DictionaryId = r.JmnedictId,
                RelevanceScore = 0,
                PrimaryKanji = new DTOs.ProperNoun.KanjiFormDto
                {
                    Text = r.PrimaryKanji?.Text ?? string.Empty,
                    Tags = r.PrimaryKanji?.Tags ?? new List<TagInfoDto>(),
                },
                PrimaryKana = new DTOs.ProperNoun.KanaFormDto
                {
                    Text = r.PrimaryKana?.Text ?? string.Empty,
                    AppliesToKanji = r.PrimaryKana?.AppliesToKanji?.ToList() ?? new List<string>(),
                    Tags = r.PrimaryKana?.Tags ?? new List<TagInfoDto>(),
                },
                OtherKanjiForms = r.OtherKanji.Select(k => new DTOs.ProperNoun.KanjiFormDto
                {
                    Text = k.Text,
                    Tags = k.Tags,
                }).ToList(),
                OtherKanaForms = r.OtherKana.Select(k => new DTOs.ProperNoun.KanaFormDto
                {
                    Text = k.Text,
                    AppliesToKanji = k.AppliesToKanji?.ToList() ?? new List<string>(),
                    Tags = k.Tags,
                }).ToList(),
                Translations = r.Translations.Select(t => new TranslationSummaryDto
                {
                    Types = t.Types,
                    Translations = t.Texts.Select(tt => new TranslationTextDto { Language = tt.Lang, Text = tt.Text }).ToList(),
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
            .AsNoTracking()
            .AsQueryable();

        var filteredQuery = _vocabularyQueryBuilder.BuildQuery(baseQuery, spec);
        var totalCount = await filteredQuery.CountAsync();

        // Stable ordering is required for consistent paging.
        var pagedQuery = filteredQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var rows = await pagedQuery.Select(v => new
        {
            v.Id,
            v.JmdictId,
            v.JlptLevelNew,
            IsCommon = v.Kana.Any(k => k.IsCommon) && v.Kanji.Any(k => k.IsCommon),

            PrimaryKanji = v.Kanji
                .OrderByDescending(k => k.IsCommon)
                .ThenBy(k => k.CreatedAt)
                .ThenBy(k => k.Id)
                .Select(k => new
                {
                    k.Text,
                    k.IsCommon,
                    Tags = k.Tags
                        .Select(t => new TagInfoDto
                        {
                            Code = t.TagCode,
                            Description = t.Tag.Description,
                            Category = t.Tag.Category
                        }).ToList()
                })
                .FirstOrDefault(),
            PrimaryKana = v.Kana
                .OrderByDescending(k => k.IsCommon)
                .ThenBy(k => k.CreatedAt)
                .ThenBy(k => k.Id)
                .Select(k => new
                {
                    k.Text,
                    k.IsCommon,
                    Tags = k.Tags
                        .Select(t => new TagInfoDto
                        {
                            Code = t.TagCode,
                            Description = t.Tag.Description,
                            Category = t.Tag.Category
                    }).ToList()
                })
                .FirstOrDefault(),

            OtherKanji = v.Kanji
                .OrderByDescending(k => k.IsCommon)
                .ThenBy(k => k.CreatedAt)
                .ThenBy(k => k.Id)
                .Skip(1)
                .Select(k => new
                {
                    k.Text,
                    k.IsCommon,
                    Tags = k.Tags
                        .Select(t => new TagInfoDto
                        {
                            Code = t.TagCode,
                            Description = t.Tag.Description,
                            Category = t.Tag.Category
                    }).ToList()
                })
                .ToList(),
            OtherKana = v.Kana
                .OrderByDescending(k => k.IsCommon)
                .ThenBy(k => k.CreatedAt)
                .ThenBy(k => k.Id)
                .Skip(1)
                .Select(k => new
                {
                    k.Text,
                    k.IsCommon,
                    Tags = k.Tags
                        .Select(t => new TagInfoDto
                        {
                            Code = t.TagCode,
                                Description = t.Tag.Description,
                                Category = t.Tag.Category
                        }).ToList()
                })
                .ToList(),

            Senses = v.Senses
                .OrderBy(s => s.Id)
                .Take(3)
                .Select(s => new
                {
                    s.AppliesToKanji,
                    s.AppliesToKana,
                    s.Info,
                    Tags = s.Tags
                        .Select(t => new TagInfoDto
                        {
                            Code = t.TagCode,
                            Description = t.Tag.Description,
                            Category = t.Tag.Category,
                            Type = t.TagType
                        }).ToList(),
                    Glosses = s.Glosses
                        .OrderBy(g => g.Id)
                        .Select(g => new { g.Lang, g.Text })
                        .ToList()
                })
                .ToList()
        }).ToListAsync();

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultVocabulary
        {
            Data = rows.Select(r => new VocabularySummaryDto
            {
                Id = r.Id,
                DictionaryId = r.JmdictId,
                JlptLevel = r.JlptLevelNew,
                RelevanceScore = 0,
                IsCommon = r.IsCommon,
                PrimaryKanji = new DTOs.Vocabulary.KanjiFormDto
                {
                    Text = r.PrimaryKanji?.Text ?? string.Empty,
                    IsCommon = r.PrimaryKanji?.IsCommon ?? false,
                    Tags = r.PrimaryKanji?.Tags ?? new List<TagInfoDto>(),
                },
                PrimaryKana = new DTOs.Vocabulary.KanaFormDto
                {
                    Text = r.PrimaryKana?.Text ?? string.Empty,
                    IsCommon = r.PrimaryKana?.IsCommon ?? false,
                    Tags = r.PrimaryKana?.Tags ?? new List<TagInfoDto>(),
                },
                OtherKanjiForms = r.OtherKanji.Select(k => new DTOs.Vocabulary.KanjiFormDto
                {
                    Text = k.Text,
                    IsCommon = k.IsCommon,
                    Tags = k.Tags,
                }).ToList(),
                OtherKanaForms = r.OtherKana.Select(k => new DTOs.Vocabulary.KanaFormDto
                {
                    Text = k.Text,
                    IsCommon = k.IsCommon,
                    Tags = k.Tags,
                }).ToList(),
                Senses = r.Senses.Select(s => new SenseSummaryDto
                {
                    // NOTE: underlying storage is string[]?; convert after materialization.
                    AppliesToKanji = s.AppliesToKanji?.ToList(),
                    AppliesToKana = s.AppliesToKana?.ToList(),
                    Info = s.Info?.ToList(),
                    Glosses = s.Glosses.Select(g => new SenseGlossDto { Language = g.Lang, Text = g.Text }).ToList(),
                    Tags = s.Tags,
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