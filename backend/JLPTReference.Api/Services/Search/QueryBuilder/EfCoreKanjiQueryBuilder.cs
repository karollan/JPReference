using System.Text.Json;
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

        var patterns = tokens
            .Select(t => t.TransliterationBlocked ? [t.RawValue] : t.Variants)
            .Aggregate(
                new List<string> { "" },
                (acc, variants) =>
                    acc.SelectMany(prefix => variants.Select(v => prefix + v + '%')).ToList()
            )
            .ToList();

        query = query.Where(k =>
            patterns.Any(p => EF.Functions.ILike(k.Literal, p)) ||
            k.Readings.Any(r => patterns.Any(p => EF.Functions.ILike(r.Value, p))) ||
            k.Meanings.Any(m => m.Lang == "en" && patterns.Any(p => EF.Functions.ILike(m.Value, p)))
        );

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