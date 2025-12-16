using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Entities.Kanji;
using JLPTReference.Api.Services.Search.Ranking;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

public class EfCoreKanjiQueryBuilder : ISearchQueryBuilder<Kanji>, IRankedQueryBuilder<Kanji, KanjiRankingProfile>
{
    private readonly IKanjiRanker _ranker;
    
    public EfCoreKanjiQueryBuilder(IKanjiRanker ranker)
    {
        _ranker = ranker;
    }
    
    public IQueryable<Kanji> BuildQuery(IQueryable<Kanji> query, SearchSpec spec)
    {
        query = ApplyTokens(query, spec.Tokens);
        query = ApplyFilters(query, spec.Filters);
        return query;
    }

    public IOrderedQueryable<Kanji> BuildRankedQuery(
        IQueryable<Kanji> query, 
        SearchSpec spec,
        KanjiRankingProfile profile)
    {
        query = BuildQuery(query, spec);
        return ApplyRankingOrder(query, spec, profile);
    }

    private IOrderedQueryable<Kanji> ApplyRankingOrder(
        IQueryable<Kanji> query, 
        SearchSpec spec,
        KanjiRankingProfile profile)
    {
        var patterns = SearchPatternUtils.GetPatterns(spec.Tokens);
        var hasWildcard = spec.Tokens?.Any(t => t.HasWildcard) ?? false;
        
        if (patterns.Count == 0)
        {
            // No search tokens - order by frequency then JLPT then grade
            return query
                .OrderBy(k => k.Frequency ?? int.MaxValue)
                .ThenByDescending(k => k.JlptLevelNew.HasValue)
                .ThenBy(k => k.JlptLevelNew ?? 99)
                .ThenBy(k => k.Grade ?? 99);
        }

        var exactTerms = patterns
            .Select(p => p.TrimEnd('%'))
            .Where(p => !string.IsNullOrEmpty(p) && !hasWildcard)
            .Select(SearchPatternUtils.UnescapeLikePattern)
            .ToList();

        return query
            .OrderByDescending(k => 
                // Exact literal match (highest priority for kanji)
                exactTerms.Count > 0 && exactTerms.Any(t => 
                    EF.Functions.ILike(k.Literal, t)) ? 4 :
                // Exact reading match
                exactTerms.Count > 0 && k.Readings.Any(r => exactTerms.Any(t => 
                    EF.Functions.ILike(r.Value, t))) ? 3 :
                // Prefix reading match
                k.Readings.Any(r => patterns.Any(p => EF.Functions.ILike(r.Value, p))) ? 2 :
                // Meaning match
                1
            )
            // Then by frequency (lower = more common)
            .ThenBy(k => k.Frequency ?? int.MaxValue)
            // Then by JLPT (lower number = higher level)
            .ThenBy(k => k.JlptLevelNew ?? 99)
            // Then by grade
            .ThenBy(k => k.Grade ?? 99)
            // Stable ordering
            .ThenBy(k => k.Id);
    }

    private IQueryable<Kanji> ApplyTokens(IQueryable<Kanji> query, List<SearchToken> tokens)
    {
        if (tokens == null || tokens.Count == 0) return query;

        var patterns = SearchPatternUtils.GetPatterns(tokens);

        query = query.Where(k =>
            patterns.Any(p => EF.Functions.ILike(k.Literal, p)) ||
            k.Readings.Any(r => patterns.Any(p => EF.Functions.ILike(r.Value, p))) ||
            k.Meanings.Any(m => patterns.Any(p => EF.Functions.ILike(m.Value, p)))
        );

        return query;
    }

    private IQueryable<Kanji> ApplyFilters(IQueryable<Kanji> query, SearchFilters filters)
    {
        if (filters == null) return query;

        if (filters.JlptLevels is {Min: > 0, Max: > 0})
        {
            query = query.Where(k => 
                k.JlptLevelNew >= filters.JlptLevels.Min &&
                k.JlptLevelNew <= filters.JlptLevels.Max
        );
        }

        if (filters.StrokeCount is {Min: > 0, Max: > 0})
        {
            query = query.Where(k =>
                k.StrokeCount >= filters.StrokeCount.Min &&
                k.StrokeCount <= filters.StrokeCount.Max
            );
        }

        if (filters.Grades is {Min: > 0, Max: > 0})
        {
            query = query.Where(k =>
                k.Grade >= filters.Grades.Min &&
                k.Grade <= filters.Grades.Max
            );
        }

        if (filters.Frequency is {Min: > 0, Max: > 0})
        {
            query = query.Where(k =>
                k.Frequency >= filters.Frequency.Min &&
                k.Frequency <= filters.Frequency.Max
            );
        }

        if (filters.Languages is not null && filters.Languages.Count > 0)
        {
            query = query.Where(k => k.Meanings.Any(m => filters.Languages.Contains(m.Lang)));
        }

        return query;
    }
}
