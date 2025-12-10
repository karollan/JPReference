using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Entities.Kanji;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

public class EfCoreKanjiQueryBuilder : ISearchQueryBuilder<Kanji>
{
    public IQueryable<Kanji> BuildQuery(IQueryable<Kanji> query, SearchSpec spec)
    {
        query = ApplyTokens(query, spec.Tokens);
        query = ApplyFilters(query, spec.Filters);
        return query;
    }

    private IQueryable<Kanji> ApplyTokens(IQueryable<Kanji> query, List<SearchToken> tokens)
    {
        if (tokens == null || tokens.Count == 0) return query;

        foreach (var token in tokens)
        {
            if (token.TransliterationBlocked)
            {
                if (token.HasWildcard)
                {
                    token.RawValue = token.RawValue.Replace('*', '%');
                    token.RawValue = token.RawValue.Replace('?', '_');
                }
                query = query.Where(k => 
                    EF.Functions.ILike(k.Literal, token.RawValue) ||
                    k.Readings.Any(r => EF.Functions.ILike(r.Value, token.RawValue)) ||
                    k.Meanings.Any(m => m.Lang == "en" && EF.Functions.ILike(m.Value, token.RawValue))
                );
            }
            else {
                foreach (var variant in token.Variants)
                {
                    query = query.Where(k =>
                        EF.Functions.ILike(k.Literal, variant + '%') ||
                        k.Readings.Any(r => EF.Functions.ILike(r.Value, variant + '%')) ||
                        k.Meanings.Any(m => m.Lang == "en" && EF.Functions.ILike(m.Value, variant + '%'))
                    );
                }
            }
        }

        return query;
    }

    private IQueryable<Kanji> ApplyFilters(IQueryable<Kanji> query, SearchFilters filters)
    {
        if (filters == null) return query;

        if (filters.JlptLevels is {Count: > 0})
        {
            query = query.Where(k => filters.JlptLevels.Contains(k.JlptLevelNew ?? 0));
        }

        if (filters.StrokeCount is {Min: > 0, Max: > 0})
        {
            query = query.Where(k =>
                k.StrokeCount >= filters.StrokeCount.Min &&
                k.StrokeCount <= filters.StrokeCount.Max
            );
        }

        if (filters.Grades is {Count: > 0})
        {
            query = query.Where(k => filters.Grades.Contains(k.Grade ?? 0));
        }

        return query;
    }
}