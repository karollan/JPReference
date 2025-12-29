using JLPTReference.Api.Data;
using JLPTReference.Api.DTOs.ProperNoun;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.DTOs.Common;
using JLPTReference.Api.Entities.ProperNoun;
using JLPTReference.Api.Services.Search.Ranking;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

public class EfCoreProperNounSearchService : IProperNounSearchService
{
    private readonly IDbContextFactory<ApplicationDBContext> _contextFactory;
    private readonly IRankedQueryBuilder<ProperNoun, ProperNounRankingProfile> _queryBuilder;
    private readonly IProperNounRanker _ranker;
    private readonly ProperNounRankingProfile _rankingProfile;

    public EfCoreProperNounSearchService(
        IDbContextFactory<ApplicationDBContext> contextFactory,
        IRankedQueryBuilder<ProperNoun, ProperNounRankingProfile> queryBuilder,
        IProperNounRanker ranker,
        ProperNounRankingProfile rankingProfile)
    {
        _contextFactory = contextFactory;
        _queryBuilder = queryBuilder;
        _ranker = ranker;
        _rankingProfile = rankingProfile;
    }

    public async Task<SearchResultProperNoun> SearchAsync(SearchSpec spec, int pageSize, int page)
    {
        await using var context = _contextFactory.CreateDbContext();

        var baseQuery = context.ProperNoun
            .AsNoTracking()
            .AsQueryable();

        var rankedQuery = _queryBuilder.BuildRankedQuery(baseQuery, spec, _rankingProfile);
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

        // Build DTOs
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

        // Compute ranking scores
        var patterns = SearchPatternUtils.GetPatterns(spec.Tokens);
        var hasWildcard = spec.Tokens?.Any(t => t.HasWildcard) ?? false;

        foreach (var (dto, row) in dtos.Zip(rows))
        {
            var matchInfo = new ProperNounMatchInfo
            {
                ProperNounId = dto.Id
            };

            DetermineMatchDetails(matchInfo, patterns, hasWildcard,
                row.AllKanjiTexts, row.AllKanaTexts, row.AllTranslationTexts);

            _ranker.ComputeScores(new[] { matchInfo }, _rankingProfile);
            dto.RelevanceScore = matchInfo.RelevanceScore;
        }

        // Sort by score
        dtos = dtos.OrderByDescending(d => d.RelevanceScore).ToList();

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultProperNoun
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
        ProperNounMatchInfo info,
        List<string> patterns,
        bool hasWildcard,
        List<string> kanjiTexts,
        List<string> kanaTexts,
        List<string> translationTexts)
    {
        if (patterns.Count == 0)
        {
            info.BestMatchQuality = MatchQuality.None;
            return;
        }

        var bestQuality = MatchQuality.None;
        var locations = MatchLocation.None;
        int shortestMatchLength = int.MaxValue;

        // Check kana
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

        // Check kanji
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

        // Check translations
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
}

