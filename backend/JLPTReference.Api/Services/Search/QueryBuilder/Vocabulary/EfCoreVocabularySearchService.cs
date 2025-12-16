using JLPTReference.Api.Data;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.Entities.Vocabulary;
using JLPTReference.Api.Services.Search.Ranking;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

/// <summary>
/// EF Core-based vocabulary search service.
/// Uses LINQ queries translated to SQL by EF Core.
/// Simpler but slower than the SQL-based implementation for large datasets.
/// </summary>
public class EfCoreVocabularySearchService : IVocabularySearchService
{
    private readonly IDbContextFactory<ApplicationDBContext> _contextFactory;
    private readonly IRankedQueryBuilder<Vocabulary, VocabularyRankingProfile> _queryBuilder;
    private readonly IVocabularyRanker _ranker;
    private readonly VocabularyRankingProfile _rankingProfile;

    public EfCoreVocabularySearchService(
        IDbContextFactory<ApplicationDBContext> contextFactory,
        IRankedQueryBuilder<Vocabulary, VocabularyRankingProfile> queryBuilder,
        IVocabularyRanker ranker,
        VocabularyRankingProfile rankingProfile)
    {
        _contextFactory = contextFactory;
        _queryBuilder = queryBuilder;
        _ranker = ranker;
        _rankingProfile = rankingProfile;
    }

    public async Task<VocabularySearchResult> SearchAsync(SearchSpec spec, int pageSize, int page)
    {
        await using var context = _contextFactory.CreateDbContext();

        var baseQuery = context.Vocabulary
            .AsNoTracking()
            .AsQueryable();

        var rankedQuery = _queryBuilder.BuildRankedQuery(baseQuery, spec, _rankingProfile);
        var totalCount = await rankedQuery.CountAsync();

        var pagedQuery = rankedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var rows = await pagedQuery.Select(v => new
        {
            v.Id,
            v.JmdictId,
            v.JlptLevelNew,
            IsCommon = v.Kana.Any(k => k.IsCommon) || v.Kanji.Any(k => k.IsCommon),
            SenseCount = v.Senses.Count,
            
            AllKanaTexts = v.Kana.Select(k => k.Text).ToList(),
            AllKanjiTexts = v.Kanji.Select(k => k.Text).ToList(),
            FirstSenseGlosses = v.Senses
                .OrderBy(s => s.Id)
                .Take(1)
                .SelectMany(s => s.Glosses.Select(g => g.Text))
                .ToList(),
            AllGlosses = v.Senses
                .SelectMany(s => s.Glosses.Select(g => g.Text))
                .ToList(),

            PrimaryKanji = v.Kanji
                .OrderByDescending(k => k.IsCommon)
                .ThenBy(k => k.CreatedAt)
                .ThenBy(k => k.Id)
                .Select(k => new
                {
                    k.Text,
                    k.IsCommon,
                    Tags = k.Tags.Select(t => new TagInfoDto
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
                    Tags = k.Tags.Select(t => new TagInfoDto
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
                    Tags = k.Tags.Select(t => new TagInfoDto
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
                    Tags = k.Tags.Select(t => new TagInfoDto
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
                    Tags = s.Tags.Select(t => new TagInfoDto
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

        // Build DTOs
        var dtos = rows.Select(r => new VocabularySummaryDto
        {
            Id = r.Id,
            DictionaryId = r.JmdictId,
            JlptLevel = r.JlptLevelNew,
            RelevanceScore = 0,
            IsCommon = r.IsCommon,
            PrimaryKanji = new KanjiFormDto
            {
                Text = r.PrimaryKanji?.Text ?? string.Empty,
                IsCommon = r.PrimaryKanji?.IsCommon ?? false,
                Tags = r.PrimaryKanji?.Tags ?? new List<TagInfoDto>(),
            },
            PrimaryKana = new KanaFormDto
            {
                Text = r.PrimaryKana?.Text ?? string.Empty,
                IsCommon = r.PrimaryKana?.IsCommon ?? false,
                Tags = r.PrimaryKana?.Tags ?? new List<TagInfoDto>(),
            },
            OtherKanjiForms = r.OtherKanji.Select(k => new KanjiFormDto
            {
                Text = k.Text,
                IsCommon = k.IsCommon,
                Tags = k.Tags,
            }).ToList(),
            OtherKanaForms = r.OtherKana.Select(k => new KanaFormDto
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

        // Compute ranking scores
        var patterns = SearchPatternUtils.GetPatterns(spec.Tokens);
        var hasWildcard = spec.Tokens?.Any(t => t.HasWildcard) ?? false;

        foreach (var (dto, row) in dtos.Zip(rows))
        {
            var matchInfo = new VocabularyMatchInfo
            {
                VocabularyId = dto.Id,
                IsCommon = dto.IsCommon,
                JlptLevel = dto.JlptLevel,
                SenseCount = row.SenseCount
            };

            // Determine match quality
            DetermineMatchDetails(matchInfo, patterns, hasWildcard, 
                row.AllKanaTexts, row.AllKanjiTexts, row.FirstSenseGlosses, row.AllGlosses);

            _ranker.ComputeScores(new[] { matchInfo }, _rankingProfile);
            dto.RelevanceScore = matchInfo.RelevanceScore;
        }

        // Sort by score
        dtos = dtos.OrderByDescending(d => d.RelevanceScore).ToList();

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new VocabularySearchResult
        {
            Data = dtos,
            Pagination = new PaginationMetadata
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            }
        };
    }

    private void DetermineMatchDetails(
        VocabularyMatchInfo info,
        List<string> patterns,
        bool hasWildcard,
        List<string> kanaTexts,
        List<string> kanjiTexts,
        List<string> firstSenseGlosses,
        List<string> allGlosses)
    {
        if (patterns.Count == 0)
        {
            info.BestMatchQuality = MatchQuality.None;
            return;
        }

        var bestQuality = MatchQuality.None;
        var locations = MatchLocation.None;
        int shortestMatchLength = int.MaxValue;

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
}

