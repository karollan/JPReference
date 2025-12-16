using JLPTReference.Api.Entities.Vocabulary;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Services.Search.Ranking;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

public class EfCoreVocabularyQueryBuilder : ISearchQueryBuilder<Vocabulary>, IRankedQueryBuilder<Vocabulary, VocabularyRankingProfile>
{
    private readonly IVocabularyRanker _ranker;
    
    public EfCoreVocabularyQueryBuilder(IVocabularyRanker ranker)
    {
        _ranker = ranker;
    }
    
    public IQueryable<Vocabulary> BuildQuery(IQueryable<Vocabulary> query, SearchSpec spec)
    {
        query = ApplyTokens(query, spec.Tokens);
        query = ApplyFilters(query, spec.Filters);
        return query;
    }

    public IOrderedQueryable<Vocabulary> BuildRankedQuery(
        IQueryable<Vocabulary> query, 
        SearchSpec spec,
        VocabularyRankingProfile profile)
    {
        query = BuildQuery(query, spec);
        return ApplyRankingOrder(query, spec, profile);
    }

    private IOrderedQueryable<Vocabulary> ApplyRankingOrder(
        IQueryable<Vocabulary> query, 
        SearchSpec spec,
        VocabularyRankingProfile profile)
    {
        var patterns = SearchPatternUtils.GetPatterns(spec.Tokens);
        var hasWildcard = spec.Tokens?.Any(t => t.HasWildcard) ?? false;
        
        if (patterns.Count == 0)
        {
            return query
                .OrderByDescending(v => v.Kana.Any(k => k.IsCommon) || v.Kanji.Any(k => k.IsCommon))
                .ThenByDescending(v => v.JlptLevelNew.HasValue)
                .ThenBy(v => v.JlptLevelNew ?? 99);
        }

        var exactTerms = patterns
            .Select(p => p.TrimEnd('%'))
            .Where(p => !string.IsNullOrEmpty(p) && !hasWildcard)
            .Select(SearchPatternUtils.UnescapeLikePattern)
            .ToList();

        return query
            .OrderByDescending(v => 
                exactTerms.Count > 0 && v.Kana.Any(k => exactTerms.Any(t => 
                    EF.Functions.ILike(k.Text, t))) ? 3 :
                exactTerms.Count > 0 && v.Kanji.Any(k => exactTerms.Any(t => 
                    EF.Functions.ILike(k.Text, t))) ? 3 :
                v.Kana.Any(k => patterns.Any(p => EF.Functions.ILike(k.Text, p))) ? 2 :
                v.Kanji.Any(k => patterns.Any(p => EF.Functions.ILike(k.Text, p))) ? 2 :
                1
            )
            .ThenByDescending(v => v.Kana.Any(k => k.IsCommon) || v.Kanji.Any(k => k.IsCommon))
            .ThenBy(v => v.JlptLevelNew ?? 99)
            .ThenBy(v => v.Id);
    }

    private IQueryable<Vocabulary> ApplyTokens(IQueryable<Vocabulary> query, List<SearchToken> tokens)
    {
        if (tokens == null || tokens.Count == 0) return query;

        var patterns = SearchPatternUtils.GetPatterns(tokens);

        query = query.Where(v =>
            v.Kana.Any(k => patterns.Any(p => EF.Functions.ILike(k.Text, p))) ||
            v.Kanji.Any(k => patterns.Any(p => EF.Functions.ILike(k.Text, p))) ||
            v.Senses.Any(s => s.Glosses.Any(g => patterns.Any(p => EF.Functions.ILike(g.Text, p))))
        );

        return query;
    }

    private IQueryable<Vocabulary> ApplyFilters(IQueryable<Vocabulary> query, SearchFilters filters)
    {
        if (filters == null) return query;

        if (filters.JlptLevels is {Min: > 0, Max: > 0})
        {
            query = query.Where(v =>
                v.JlptLevelNew >= filters.JlptLevels.Min &&
                v.JlptLevelNew <= filters.JlptLevels.Max
            );
        }

        if (filters.CommonOnly is true)
        {
            query = query.Where(v => v.Kana.Any(k => k.IsCommon) && v.Kanji.Any(k => k.IsCommon));
        }

        if (filters.Tags is {Count: > 0})
        {
            query = query.Where(v => 
                v.Kana.Any(k => k.Tags.Any(t => filters.Tags.Contains(t.TagCode))) || 
                v.Kanji.Any(k => k.Tags.Any(t => filters.Tags.Contains(t.TagCode))) || 
                v.Senses.Any(s => s.Tags.Any(t => filters.Tags.Contains(t.TagCode)))
            );
        }

        if (filters.Languages is not null && filters.Languages.Count > 0)
        {
            query = query.Where(v => v.Senses.Any(s => s.Glosses.Any(g => filters.Languages.Contains(g.Lang))));
        }

        return query;
    }
}
