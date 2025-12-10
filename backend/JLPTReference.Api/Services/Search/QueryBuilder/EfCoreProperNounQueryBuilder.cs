using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Entities.ProperNoun;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

public class EfCoreProperNounQueryBuilder : ISearchQueryBuilder<ProperNoun>
{
    public IQueryable<ProperNoun> BuildQuery(IQueryable<ProperNoun> query, SearchSpec spec)
    {
        query = ApplyTokens(query, spec.Tokens);
        query = ApplyFilters(query, spec.Filters);

        return query;
    }

    private IQueryable<ProperNoun> ApplyTokens(IQueryable<ProperNoun> query, List<SearchToken> tokens)
    {
        if (tokens == null || tokens.Count == 0) return query;

        var patterns = tokens
            .Select(t => t.TransliterationBlocked ? [t.RawValue] : t.Variants)
            .Aggregate(
                new List<string> { "" },
                (acc, variants) =>
                    acc.SelectMany(prefix => variants.Select(v => prefix + v + '%')).ToList()
            )
            .ToList();

        query = query.Where(p =>
            p.KanjiForms.Any(k => patterns.Any(p => EF.Functions.ILike(k.Text, p))) ||
            p.KanaForms.Any(k => patterns.Any(p => EF.Functions.ILike(k.Text, p))) ||
            p.Translations.Any(t => t.Texts.Any(t => patterns.Any(p => EF.Functions.ILike(t.Text, p))))
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
                p.Translations.Any(t => t.Types.Any(t => filters.Tags.Contains(t.TagCode)))
            );
        }

        return query;
    }
}