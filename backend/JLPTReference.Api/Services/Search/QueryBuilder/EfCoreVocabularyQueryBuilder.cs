using JLPTReference.Api.Entities.Vocabulary;
using JLPTReference.Api.DTOs.Search;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Services.Search.QueryBuilder;
public class EfCoreVocabularyQueryBuilder : ISearchQueryBuilder<Vocabulary>
{
    public IQueryable<Vocabulary> BuildQuery(IQueryable<Vocabulary> query, SearchSpec spec)
    {
        query = ApplyTokens(query, spec.Tokens);
        query = ApplyFilters(query, spec.Filters);
        return query;
    }

    private IQueryable<Vocabulary> ApplyTokens(IQueryable<Vocabulary> query, List<SearchToken> tokens)
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

        query = query.Where(v =>
            v.Kana.Any(k => patterns.Any(p => EF.Functions.ILike(k.Text, p))) ||
            v.Kanji.Any(k => patterns.Any(p => EF.Functions.ILike(k.Text, p))) ||
            v.Senses.Any(s => s.Glosses.Any(g => g.Lang == "eng" && patterns.Any(p => EF.Functions.ILike(g.Text, p))))
        );

        return query;
    }

    private IQueryable<Vocabulary> ApplyFilters(IQueryable<Vocabulary> query, SearchFilters filters)
    {
        if (filters == null) return query;

        if (filters.JlptLevels is {Count: > 0})
        {
            query = query.Where(v => filters.JlptLevels.Contains(v.JlptLevelNew ?? 0));
        }

        if (filters.PartOfSpeech is {Count: > 0})
        {
            query = query.Where(v => v.Senses.Any(s => s.Tags.Any(t => filters.PartOfSpeech.Contains(t.TagCode))));
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

        return query;
    }
}