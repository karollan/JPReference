using JLPTReference.Api.Data;
using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Radical;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Entities.Kanji;
using JLPTReference.Api.Services.Search.Ranking;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

public class EfCoreKanjiSearchService : IKanjiSearchService
{
    private readonly IDbContextFactory<ApplicationDBContext> _contextFactory;
    private readonly IRankedQueryBuilder<Kanji, KanjiRankingProfile> _queryBuilder;
    private readonly IKanjiRanker _ranker;
    private readonly KanjiRankingProfile _rankingProfile;

    public EfCoreKanjiSearchService(
        IDbContextFactory<ApplicationDBContext> contextFactory,
        IRankedQueryBuilder<Kanji, KanjiRankingProfile> queryBuilder,
        IKanjiRanker ranker,
        KanjiRankingProfile rankingProfile)
    {
        _contextFactory = contextFactory;
        _queryBuilder = queryBuilder;
        _ranker = ranker;
        _rankingProfile = rankingProfile;
    }

    public async Task<SearchResultKanji> SearchAsync(SearchSpec spec, int pageSize, int page)
    {
        await using var context = _contextFactory.CreateDbContext();

        var baseQuery = context.Kanji
            .AsNoTracking()
            .AsQueryable();

        var rankedQuery = _queryBuilder.BuildRankedQuery(baseQuery, spec, _rankingProfile);
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
            AllMeanings = k.Meanings.Where(m => m.Lang == "eng").Select(m => m.Value).ToList(),
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

        // Build DTOs
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

        // Compute ranking scores
        var patterns = SearchPatternUtils.GetPatterns(spec.Tokens);
        var hasWildcard = spec.Tokens?.Any(t => t.HasWildcard) ?? false;

        foreach (var (dto, row) in dtos.Zip(rows))
        {
            var matchInfo = new KanjiMatchInfo
            {
                KanjiId = dto.Id,
                Frequency = dto.Frequency,
                JlptLevel = dto.JlptLevel,
                Grade = dto.Grade
            };

            DetermineMatchDetails(matchInfo, patterns, hasWildcard,
                row.Literal, row.AllReadings, row.AllMeanings);

            _ranker.ComputeScores(new[] { matchInfo }, _rankingProfile);
            dto.RelevanceScore = matchInfo.RelevanceScore;
        }

        // Sort by score
        dtos = dtos.OrderByDescending(d => d.RelevanceScore).ToList();

        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new SearchResultKanji
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
        KanjiMatchInfo info,
        List<string> patterns,
        bool hasWildcard,
        string literal,
        List<string> readings,
        List<string> meanings)
    {
        if (patterns.Count == 0)
        {
            info.BestMatchQuality = MatchQuality.None;
            return;
        }

        var bestQuality = MatchQuality.None;
        var locations = MatchLocation.None;
        int shortestMatchLength = int.MaxValue;

        // Check literal
        foreach (var pattern in patterns)
        {
            var quality = SearchPatternUtils.DetermineMatchQuality(pattern, literal, hasWildcard);
            if (quality > bestQuality)
            {
                bestQuality = quality;
                shortestMatchLength = literal.Length;
            }
            else if (quality == bestQuality && literal.Length < shortestMatchLength)
            {
                shortestMatchLength = literal.Length;
            }
            if (quality != MatchQuality.None)
                locations |= MatchLocation.Literal;
        }

        // Check readings
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

        // Check meanings
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
}

