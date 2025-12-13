using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Entities.ProperNoun;
using JLPTReference.Api.Services.Search.Ranking;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

public class EfCoreProperNounQueryBuilder : ISearchQueryBuilder<ProperNoun>, IRankedQueryBuilder<ProperNoun, ProperNounRankingProfile>
{
    private readonly IProperNounRanker _ranker;
    
    public EfCoreProperNounQueryBuilder(IProperNounRanker ranker)
    {
        _ranker = ranker;
    }
    
    public IQueryable<ProperNoun> BuildQuery(IQueryable<ProperNoun> query, SearchSpec spec)
    {
        query = ApplyTokens(query, spec.Tokens);
        query = ApplyFilters(query, spec.Filters);
        return query;
    }

    public IOrderedQueryable<ProperNoun> BuildRankedQuery(
        IQueryable<ProperNoun> query, 
        SearchSpec spec,
        ProperNounRankingProfile profile)
    {
        query = BuildQuery(query, spec);
        return ApplyRankingOrder(query, spec, profile);
    }

    private IOrderedQueryable<ProperNoun> ApplyRankingOrder(
        IQueryable<ProperNoun> query, 
        SearchSpec spec,
        ProperNounRankingProfile profile)
    {
        var patterns = SearchPatternUtils.GetPatterns(spec.Tokens);
        var hasWildcard = spec.Tokens?.Any(t => t.HasWildcard) ?? false;
        
        if (patterns.Count == 0)
        {
            // No search tokens - order by creation date
            return query
                .OrderBy(p => p.CreatedAt)
                .ThenBy(p => p.Id);
        }

        var exactTerms = patterns
            .Select(p => p.TrimEnd('%'))
            .Where(p => !string.IsNullOrEmpty(p) && !hasWildcard)
            .Select(SearchPatternUtils.UnescapeLikePattern)
            .ToList();

        return query
            .OrderByDescending(p => 
                // Exact kana match
                exactTerms.Count > 0 && p.KanaForms.Any(k => exactTerms.Any(t => 
                    EF.Functions.ILike(k.Text, t))) ? 3 :
                // Exact kanji match
                exactTerms.Count > 0 && p.KanjiForms.Any(k => exactTerms.Any(t => 
                    EF.Functions.ILike(k.Text, t))) ? 3 :
                // Prefix kana match
                p.KanaForms.Any(k => patterns.Any(pt => EF.Functions.ILike(k.Text, pt))) ? 2 :
                // Prefix kanji match
                p.KanjiForms.Any(k => patterns.Any(pt => EF.Functions.ILike(k.Text, pt))) ? 2 :
                // Translation match
                1
            )
            // Stable ordering
            .ThenBy(p => p.Id);
    }

    private IQueryable<ProperNoun> ApplyTokens(IQueryable<ProperNoun> query, List<SearchToken> tokens)
    {
        if (tokens == null || tokens.Count == 0) return query;

        var patterns = SearchPatternUtils.GetPatterns(tokens);

        query = query.Where(p =>
            p.KanjiForms.Any(k => patterns.Any(pt => EF.Functions.ILike(k.Text, pt))) ||
            p.KanaForms.Any(k => patterns.Any(pt => EF.Functions.ILike(k.Text, pt))) ||
            p.Translations.Any(t => t.Texts.Any(txt => patterns.Any(pt => EF.Functions.ILike(txt.Text, pt))))
        );
        
        return query;
    }

    private IQueryable<ProperNoun> ApplyFilters(IQueryable<ProperNoun> query, SearchFilters filters)
    {
        if (filters == null) return query;

        if (filters.Tags is {Count: > 0})
        {
            query = query.Where(p => 
                p.KanjiForms.Any(k => k.Tags.Any(t => filters.Tags.Contains(t.TagCode))) ||
                p.KanaForms.Any(k => k.Tags.Any(t => filters.Tags.Contains(t.TagCode))) ||
                p.Translations.Any(t => t.Types.Any(ty => filters.Tags.Contains(ty.TagCode)))
            );
        }

        return query;
    }
}
