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
using JLPTReference.Api.Services.Search;
using JLPTReference.Api.Services.Search.QueryBuilder;
using JLPTReference.Api.Services.Search.Ranking;
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
    private readonly IRankedQueryBuilder<Vocabulary, VocabularyRankingProfile> _rankedVocabularyQueryBuilder;
    private readonly IRankedQueryBuilder<Kanji, KanjiRankingProfile> _rankedKanjiQueryBuilder;
    private readonly IRankedQueryBuilder<ProperNoun, ProperNounRankingProfile> _rankedProperNounQueryBuilder;
    private readonly IVocabularyRanker _vocabularyRanker;
    private readonly IKanjiRanker _kanjiRanker;
    private readonly IProperNounRanker _properNounRanker;
    private readonly VocabularyRankingProfile _vocabularyRankingProfile;
    private readonly KanjiRankingProfile _kanjiRankingProfile;
    private readonly ProperNounRankingProfile _properNounRankingProfile;
    private readonly IDbContextFactory<ApplicationDBContext> _contextFactory;
    
    public SearchRepository(
        IConfiguration configuration,
        ISearchQueryBuilder<Kanji> kanjiQueryBuilder,
        ISearchQueryBuilder<ProperNoun> properNounQueryBuilder,
        ISearchQueryBuilder<Vocabulary> vocabularyQueryBuilder,
        IRankedQueryBuilder<Vocabulary, VocabularyRankingProfile> rankedVocabularyQueryBuilder,
        IRankedQueryBuilder<Kanji, KanjiRankingProfile> rankedKanjiQueryBuilder,
        IRankedQueryBuilder<ProperNoun, ProperNounRankingProfile> rankedProperNounQueryBuilder,
        IVocabularyRanker vocabularyRanker,
        IKanjiRanker kanjiRanker,
        IProperNounRanker properNounRanker,
        VocabularyRankingProfile vocabularyRankingProfile,
        KanjiRankingProfile kanjiRankingProfile,
        ProperNounRankingProfile properNounRankingProfile,
        IDbContextFactory<ApplicationDBContext> contextFactory
    )
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        _kanjiQueryBuilder = kanjiQueryBuilder;
        _properNounQueryBuilder = properNounQueryBuilder;
        _vocabularyQueryBuilder = vocabularyQueryBuilder;
        _rankedVocabularyQueryBuilder = rankedVocabularyQueryBuilder;
        _rankedKanjiQueryBuilder = rankedKanjiQueryBuilder;
        _rankedProperNounQueryBuilder = rankedProperNounQueryBuilder;
        _vocabularyRanker = vocabularyRanker;
        _kanjiRanker = kanjiRanker;
        _properNounRanker = properNounRanker;
        _vocabularyRankingProfile = vocabularyRankingProfile;
        _kanjiRankingProfile = kanjiRankingProfile;
        _properNounRankingProfile = properNounRankingProfile;
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

        var rankedQuery = _rankedKanjiQueryBuilder.BuildRankedQuery(baseQuery, spec, _kanjiRankingProfile);
        var totalCount = await rankedQuery.CountAsync();

        var pagedQuery = rankedQuery
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
            AllReadings = k.Readings.Select(r => r.Value).ToList(),
            AllMeanings = k.Meanings.Where(m => m.Lang == "en").Select(m => m.Value).ToList(),
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

        var dtos = rows.Select(r => new KanjiSummaryDto
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
        }).ToList();

        // Compute detailed ranking scores
        var rowLookup = rows.ToDictionary(r => r.Id);
        ComputeKanjiScores(dtos, spec, rowLookup);
        dtos = dtos.OrderByDescending(d => d.RelevanceScore).ToList();

        return new SearchResultKanji
        {
            Data = dtos,
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

        var rankedQuery = _rankedProperNounQueryBuilder.BuildRankedQuery(baseQuery, spec, _properNounRankingProfile);
        var totalCount = await rankedQuery.CountAsync();

        var pagedQuery = rankedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var rows = await pagedQuery.Select(p => new
        {
            p.Id,
            p.JmnedictId,
            AllKanjiTexts = p.KanjiForms.Select(k => k.Text).ToList(),
            AllKanaTexts = p.KanaForms.Select(k => k.Text).ToList(),
            AllTranslationTexts = p.Translations.SelectMany(t => t.Texts.Select(txt => txt.Text)).ToList(),
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

        var dtos = rows.Select(r => new ProperNounSummaryDto
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
        }).ToList();

        // Compute detailed ranking scores
        var rowLookup = rows.ToDictionary(r => r.Id);
        ComputeProperNounScores(dtos, spec, rowLookup);
        dtos = dtos.OrderByDescending(d => d.RelevanceScore).ToList();

        return new SearchResultProperNoun
        {
            Data = dtos,
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

        // Use ranked query builder for ordering
        var rankedQuery = _rankedVocabularyQueryBuilder.BuildRankedQuery(baseQuery, spec, _vocabularyRankingProfile);
        var totalCount = await rankedQuery.CountAsync();

        // Apply pagination
        var pagedQuery = rankedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        // Project to intermediate type that includes all data needed for ranking
        var rows = await pagedQuery.Select(v => new
        {
            v.Id,
            v.JmdictId,
            v.JlptLevelNew,
            IsCommon = v.Kana.Any(k => k.IsCommon) || v.Kanji.Any(k => k.IsCommon),
            SenseCount = v.Senses.Count,
            
            // All text fields for ranking calculations
            AllKanaTexts = v.Kana.Select(k => k.Text).ToList(),
            AllKanjiTexts = v.Kanji.Select(k => k.Text).ToList(),
            FirstSenseGlosses = v.Senses
                .OrderBy(s => s.Id)
                .Take(1)
                .SelectMany(s => s.Glosses.Where(g => g.Lang == "eng").Select(g => g.Text))
                .ToList(),
            AllGlosses = v.Senses
                .SelectMany(s => s.Glosses.Where(g => g.Lang == "eng").Select(g => g.Text))
                .ToList(),

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

        // Map to DTOs
        var dtos = rows.Select(r => new VocabularySummaryDto
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
                AppliesToKanji = s.AppliesToKanji?.ToList(),
                AppliesToKana = s.AppliesToKana?.ToList(),
                Info = s.Info?.ToList(),
                Glosses = s.Glosses.Select(g => new SenseGlossDto { Language = g.Lang, Text = g.Text }).ToList(),
                Tags = s.Tags,
            }).ToList(),
        }).ToList();

        // Compute detailed ranking scores in memory
        var rowLookup = rows.ToDictionary(r => r.Id);
        ComputeVocabularyScores(dtos, spec, rowLookup);

        // Sort by computed score (descending) for final ordering within page
        dtos = dtos.OrderByDescending(d => d.RelevanceScore).ToList();

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultVocabulary
        {
            Data = dtos,
            Pagination = new PaginationMetadata
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
            }
        };
    }
    
    /// <summary>
    /// Computes detailed relevance scores for vocabulary DTOs using match analysis.
    /// </summary>
    private void ComputeVocabularyScores<TRow>(
        List<VocabularySummaryDto> dtos, 
        SearchSpec spec,
        Dictionary<Guid, TRow> rowLookup)
        where TRow : class
    {
        // Get search patterns
        var patterns = SearchPatternUtils.GetPatterns(spec.Tokens);
        var hasWildcard = spec.Tokens?.Any(t => t.HasWildcard) ?? false;
        
        if (patterns.Count == 0)
        {
            // No search tokens - assign base scores based on static properties
            foreach (var dto in dtos)
            {
                var info = new VocabularyMatchInfo
                {
                    VocabularyId = dto.Id,
                    BestMatchQuality = MatchQuality.None,
                    IsCommon = dto.IsCommon,
                    JlptLevel = dto.JlptLevel
                };
                
                var matchInfos = new[] { info };
                _vocabularyRanker.ComputeScores(matchInfos, _vocabularyRankingProfile);
                dto.RelevanceScore = info.RelevanceScore;
            }
            return;
        }

        // Use dynamic access to get the extra fields from the anonymous type
        foreach (var dto in dtos)
        {
            if (!rowLookup.TryGetValue(dto.Id, out var row))
                continue;

            // Access the anonymous type properties via reflection (type-safe alternative would require named type)
            var rowType = row.GetType();
            var allKanaTexts = (List<string>?)rowType.GetProperty("AllKanaTexts")?.GetValue(row) ?? new List<string>();
            var allKanjiTexts = (List<string>?)rowType.GetProperty("AllKanjiTexts")?.GetValue(row) ?? new List<string>();
            var firstSenseGlosses = (List<string>?)rowType.GetProperty("FirstSenseGlosses")?.GetValue(row) ?? new List<string>();
            var allGlosses = (List<string>?)rowType.GetProperty("AllGlosses")?.GetValue(row) ?? new List<string>();
            var senseCount = (int)(rowType.GetProperty("SenseCount")?.GetValue(row) ?? 0);

            var info = new VocabularyMatchInfo
            {
                VocabularyId = dto.Id,
                IsCommon = dto.IsCommon,
                JlptLevel = dto.JlptLevel,
                SenseCount = senseCount
            };

            // Determine match quality and location
            DetermineVocabularyMatchDetails(
                info, patterns, hasWildcard,
                allKanaTexts, allKanjiTexts, firstSenseGlosses, allGlosses);

            var matchInfos = new[] { info };
            _vocabularyRanker.ComputeScores(matchInfos, _vocabularyRankingProfile);
            dto.RelevanceScore = info.RelevanceScore;
        }
    }

    /// <summary>
    /// Determines match quality and locations for a vocabulary entry.
    /// </summary>
    private void DetermineVocabularyMatchDetails(
        VocabularyMatchInfo info,
        List<string> patterns,
        bool hasWildcard,
        List<string> kanaTexts,
        List<string> kanjiTexts,
        List<string> firstSenseGlosses,
        List<string> allGlosses)
    {
        var bestQuality = MatchQuality.None;
        var locations = MatchLocation.None;
        int shortestMatchLength = int.MaxValue;

        // Check kana matches
        foreach (var kana in kanaTexts)
        {
            foreach (var pattern in patterns)
            {
                var quality = SearchPatternUtils.DetermineMatchQuality(pattern, kana, hasWildcard);
                if (quality > bestQuality)
                {
                    bestQuality = quality;
                    shortestMatchLength = kana.Length;
                }
                else if (quality == bestQuality && kana.Length < shortestMatchLength)
                {
                    shortestMatchLength = kana.Length;
                }
                
                if (quality != MatchQuality.None)
                    locations |= MatchLocation.Kana;
            }
        }

        // Check kanji matches
        foreach (var kanji in kanjiTexts)
        {
            foreach (var pattern in patterns)
            {
                var quality = SearchPatternUtils.DetermineMatchQuality(pattern, kanji, hasWildcard);
                if (quality > bestQuality)
                {
                    bestQuality = quality;
                    shortestMatchLength = kanji.Length;
                }
                else if (quality == bestQuality && kanji.Length < shortestMatchLength)
                {
                    shortestMatchLength = kanji.Length;
                }
                
                if (quality != MatchQuality.None)
                    locations |= MatchLocation.Kanji;
            }
        }

        // Check first sense gloss matches
        foreach (var gloss in firstSenseGlosses)
        {
            foreach (var pattern in patterns)
            {
                var quality = SearchPatternUtils.DetermineMatchQuality(pattern, gloss, hasWildcard);
                if (quality > bestQuality)
                {
                    bestQuality = quality;
                    shortestMatchLength = gloss.Length;
                }
                else if (quality == bestQuality && gloss.Length < shortestMatchLength)
                {
                    shortestMatchLength = gloss.Length;
                }
                
                if (quality != MatchQuality.None)
                {
                    locations |= MatchLocation.Gloss;
                    locations |= MatchLocation.FirstSense;
                }
            }
        }

        // Check other glosses (if not already matched)
        if (!locations.HasFlag(MatchLocation.Gloss))
        {
            foreach (var gloss in allGlosses.Except(firstSenseGlosses))
            {
                foreach (var pattern in patterns)
                {
                    var quality = SearchPatternUtils.DetermineMatchQuality(pattern, gloss, hasWildcard);
                    if (quality > bestQuality)
                    {
                        bestQuality = quality;
                        shortestMatchLength = gloss.Length;
                    }
                    else if (quality == bestQuality && gloss.Length < shortestMatchLength)
                    {
                        shortestMatchLength = gloss.Length;
                    }
                    
                    if (quality != MatchQuality.None)
                        locations |= MatchLocation.Gloss;
                }
            }
        }

        info.BestMatchQuality = bestQuality;
        info.MatchLocations = locations;
        info.MatchedTextLength = shortestMatchLength == int.MaxValue ? 0 : shortestMatchLength;
    }

    private void ComputeKanjiScores<TRow>(
        List<KanjiSummaryDto> dtos, 
        SearchSpec spec,
        Dictionary<Guid, TRow> rowLookup)
        where TRow : class
    {
        var patterns = SearchPatternUtils.GetPatterns(spec.Tokens);
        var hasWildcard = spec.Tokens?.Any(t => t.HasWildcard) ?? false;
        
        foreach (var dto in dtos)
        {
            if (!rowLookup.TryGetValue(dto.Id, out var row))
                continue;

            var rowType = row.GetType();
            var allReadings = (List<string>?)rowType.GetProperty("AllReadings")?.GetValue(row) ?? new List<string>();
            var allMeanings = (List<string>?)rowType.GetProperty("AllMeanings")?.GetValue(row) ?? new List<string>();

            var info = new KanjiMatchInfo
            {
                KanjiId = dto.Id,
                Frequency = dto.Frequency,
                JlptLevel = dto.JlptLevel,
                Grade = dto.Grade
            };

            DetermineKanjiMatchDetails(info, patterns, hasWildcard, dto.Literal, allReadings, allMeanings);

            var matchInfos = new[] { info };
            _kanjiRanker.ComputeScores(matchInfos, _kanjiRankingProfile);
            dto.RelevanceScore = info.RelevanceScore;
        }
    }

    private void DetermineKanjiMatchDetails(
        KanjiMatchInfo info,
        List<string> patterns,
        bool hasWildcard,
        string literal,
        List<string> readings,
        List<string> meanings)
    {
        var bestQuality = MatchQuality.None;
        var locations = MatchLocation.None;
        int shortestMatchLength = int.MaxValue;

        // Check literal match
        foreach (var pattern in patterns)
        {
            var quality = SearchPatternUtils.DetermineMatchQuality(pattern, literal, hasWildcard);
            if (quality > bestQuality)
            {
                bestQuality = quality;
                shortestMatchLength = literal.Length;
            }
            
            if (quality != MatchQuality.None)
                locations |= MatchLocation.Literal;
        }

        // Check reading matches
        foreach (var reading in readings)
        {
            foreach (var pattern in patterns)
            {
                var quality = SearchPatternUtils.DetermineMatchQuality(pattern, reading, hasWildcard);
                if (quality > bestQuality)
                {
                    bestQuality = quality;
                    shortestMatchLength = reading.Length;
                }
                else if (quality == bestQuality && reading.Length < shortestMatchLength)
                {
                    shortestMatchLength = reading.Length;
                }
                
                if (quality != MatchQuality.None)
                    locations |= MatchLocation.Reading;
            }
        }

        // Check meaning matches
        foreach (var meaning in meanings)
        {
            foreach (var pattern in patterns)
            {
                var quality = SearchPatternUtils.DetermineMatchQuality(pattern, meaning, hasWildcard);
                if (quality > bestQuality)
                {
                    bestQuality = quality;
                    shortestMatchLength = meaning.Length;
                }
                else if (quality == bestQuality && meaning.Length < shortestMatchLength)
                {
                    shortestMatchLength = meaning.Length;
                }
                
                if (quality != MatchQuality.None)
                    locations |= MatchLocation.Meaning;
            }
        }

        info.BestMatchQuality = bestQuality;
        info.MatchLocations = locations;
        info.MatchedTextLength = shortestMatchLength == int.MaxValue ? 0 : shortestMatchLength;
    }

    private void ComputeProperNounScores<TRow>(
        List<ProperNounSummaryDto> dtos, 
        SearchSpec spec,
        Dictionary<Guid, TRow> rowLookup)
        where TRow : class
    {
        var patterns = SearchPatternUtils.GetPatterns(spec.Tokens);
        var hasWildcard = spec.Tokens?.Any(t => t.HasWildcard) ?? false;
        
        foreach (var dto in dtos)
        {
            if (!rowLookup.TryGetValue(dto.Id, out var row))
                continue;

            var rowType = row.GetType();
            var allKanjiTexts = (List<string>?)rowType.GetProperty("AllKanjiTexts")?.GetValue(row) ?? new List<string>();
            var allKanaTexts = (List<string>?)rowType.GetProperty("AllKanaTexts")?.GetValue(row) ?? new List<string>();
            var allTranslationTexts = (List<string>?)rowType.GetProperty("AllTranslationTexts")?.GetValue(row) ?? new List<string>();

            var info = new ProperNounMatchInfo
            {
                ProperNounId = dto.Id
            };

            DetermineProperNounMatchDetails(info, patterns, hasWildcard, allKanjiTexts, allKanaTexts, allTranslationTexts);

            var matchInfos = new[] { info };
            _properNounRanker.ComputeScores(matchInfos, _properNounRankingProfile);
            dto.RelevanceScore = info.RelevanceScore;
        }
    }

    private void DetermineProperNounMatchDetails(
        ProperNounMatchInfo info,
        List<string> patterns,
        bool hasWildcard,
        List<string> kanjiTexts,
        List<string> kanaTexts,
        List<string> translationTexts)
    {
        var bestQuality = MatchQuality.None;
        var locations = MatchLocation.None;
        int shortestMatchLength = int.MaxValue;

        // Check kanji matches
        foreach (var kanji in kanjiTexts)
        {
            foreach (var pattern in patterns)
            {
                var quality = SearchPatternUtils.DetermineMatchQuality(pattern, kanji, hasWildcard);
                if (quality > bestQuality)
                {
                    bestQuality = quality;
                    shortestMatchLength = kanji.Length;
                }
                else if (quality == bestQuality && kanji.Length < shortestMatchLength)
                {
                    shortestMatchLength = kanji.Length;
                }
                
                if (quality != MatchQuality.None)
                    locations |= MatchLocation.Kanji;
            }
        }

        // Check kana matches
        foreach (var kana in kanaTexts)
        {
            foreach (var pattern in patterns)
            {
                var quality = SearchPatternUtils.DetermineMatchQuality(pattern, kana, hasWildcard);
                if (quality > bestQuality)
                {
                    bestQuality = quality;
                    shortestMatchLength = kana.Length;
                }
                else if (quality == bestQuality && kana.Length < shortestMatchLength)
                {
                    shortestMatchLength = kana.Length;
                }
                
                if (quality != MatchQuality.None)
                    locations |= MatchLocation.Kana;
            }
        }

        // Check translation matches
        foreach (var translation in translationTexts)
        {
            foreach (var pattern in patterns)
            {
                var quality = SearchPatternUtils.DetermineMatchQuality(pattern, translation, hasWildcard);
                if (quality > bestQuality)
                {
                    bestQuality = quality;
                    shortestMatchLength = translation.Length;
                }
                else if (quality == bestQuality && translation.Length < shortestMatchLength)
                {
                    shortestMatchLength = translation.Length;
                }
                
                if (quality != MatchQuality.None)
                    locations |= MatchLocation.Translation;
            }
        }

        info.BestMatchQuality = bestQuality;
        info.MatchLocations = locations;
        info.MatchedTextLength = shortestMatchLength == int.MaxValue ? 0 : shortestMatchLength;
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