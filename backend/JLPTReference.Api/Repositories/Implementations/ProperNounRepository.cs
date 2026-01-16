using System.Text.RegularExpressions;
using JLPTReference.Api.Data;
using JLPTReference.Api.DTOs.ProperNoun;
using JLPTReference.Api.DTOs.Common;
using JLPTReference.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Repositories.Implementations;

public class ProperNounRepository : IProperNounRepository
{
    private readonly IDbContextFactory<ApplicationDBContext> _contextFactory;
    private readonly ICachedTagRepository _cachedTagRepository;

    // Pattern to match term(reading) format, e.g., "中(なか)"
    private static readonly Regex TermWithReadingPattern = new(@"^(.+)\(([^)]+)\)$", RegexOptions.Compiled);

    public ProperNounRepository(IDbContextFactory<ApplicationDBContext> contextFactory, ICachedTagRepository cachedTagRepository)
    {
        _contextFactory = contextFactory;
        _cachedTagRepository = cachedTagRepository;
    }

    /// <summary>
    /// Parses a term that may include a reading in parentheses.
    /// </summary>
    private static (string term, string? reading) ParseTermWithReading(string input)
    {
        var match = TermWithReadingPattern.Match(input);
        if (match.Success)
        {
            return (match.Groups[1].Value, match.Groups[2].Value);
        }
        return (input, null);
    }

    public async Task<ProperNounDetailDto?> GetProperNounDetailByTermAsync(string term)
    {
        if (string.IsNullOrEmpty(term))
            throw new ArgumentException("Proper noun term is required");

        var (searchTerm, reading) = ParseTermWithReading(term);

        // Single-query ID lookup via SQL function
        await using var idContext = await _contextFactory.CreateDbContextAsync();
        var properNounId = await idContext.Database
            .SqlQuery<Guid?>($"SELECT jlpt.find_proper_noun_id_by_term({searchTerm}, {reading}) AS \"Value\"")
            .FirstOrDefaultAsync();

        if (properNounId == null || properNounId == Guid.Empty)
            return null;

        var pnId = properNounId.Value;

        // Load cached tags
        var allTags = await _cachedTagRepository.GetTagsDictionaryAsync();

        // Parallel batch 1: Core data
        var properNounTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.ProperNoun.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pnId);
        });

        var kanjiFormsRawTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.ProperNounKanji.Where(p => p.ProperNounId == pnId).AsNoTracking().ToListAsync();
        });

        var kanaFormsRawTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.ProperNounKana.Where(p => p.ProperNounId == pnId).AsNoTracking().ToListAsync();
        });

        var translationsRawTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.ProperNounTranslations.Where(p => p.ProperNounId == pnId).AsNoTracking().ToListAsync();
        });

        var furiganaTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.ProperNounFurigana.Where(pf => pf.ProperNounId == pnId).AsNoTracking().ToListAsync();
        });

        var containedKanjiTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.ProperNounUsesKanji.Where(p => p.ProperNounId == pnId).AsNoTracking().ToListAsync();
        });

        await Task.WhenAll(properNounTask, kanjiFormsRawTask, kanaFormsRawTask, translationsRawTask, furiganaTask, containedKanjiTask);

        var properNoun = properNounTask.Result;
        if (properNoun == null)
            return null;

        var kanjiFormsRaw = kanjiFormsRawTask.Result;
        var kanaFormsRaw = kanaFormsRawTask.Result;
        var translationsRaw = translationsRawTask.Result;

        var kanjiFormIds = kanjiFormsRaw.Select(k => k.Id).ToList();
        var kanaFormIds = kanaFormsRaw.Select(k => k.Id).ToList();
        var translationIds = translationsRaw.Select(t => t.Id).ToList();
        var containedKanjiIds = containedKanjiTask.Result.Select(k => k.KanjiId).ToList();

        // Parallel batch 2: Tags and related
        var kanjiTagsTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.ProperNounKanjiTags.Where(p => kanjiFormIds.Contains(p.ProperNounKanjiId)).AsNoTracking().ToListAsync();
        });

        var kanaTagsTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.ProperNounKanaTags.Where(p => kanaFormIds.Contains(p.ProperNounKanaId)).AsNoTracking().ToListAsync();
        });

        var translationTypesTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.ProperNounTranslationTypes.Where(p => translationIds.Contains(p.TranslationId)).AsNoTracking().ToListAsync();
        });

        var translationTextsTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.ProperNounTranslationTexts.Where(p => translationIds.Contains(p.TranslationId)).AsNoTracking().ToListAsync();
        });

        var translationRelatedTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.ProperNounTranslationRelated.Where(p => translationIds.Contains(p.TranslationId)).AsNoTracking().ToListAsync();
        });

        var kanjisTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.Kanji.Where(k => containedKanjiIds.Contains(k.Id)).AsNoTracking().ToListAsync();
        });

        await Task.WhenAll(kanjiTagsTask, kanaTagsTask, translationTypesTask, translationTextsTask, translationRelatedTask, kanjisTask);

        var kanjiTags = kanjiTagsTask.Result;
        var kanaTags = kanaTagsTask.Result;
        var translationTypes = translationTypesTask.Result;
        var translationTexts = translationTextsTask.Result;
        var translationRelated = translationRelatedTask.Result;
        var kanjis = kanjisTask.Result;

        // Build DTOs
        var kanjiForms = kanjiFormsRaw.Select(p => new KanjiFormDto
        {
            Text = p.Text,
            Tags = kanjiTags.Where(t => t.ProperNounKanjiId == p.Id)
                .Select(t => allTags.TryGetValue(t.TagCode, out var tag)
                    ? new TagInfoDto { Code = tag.Code, Description = tag.Description, Category = tag.Category }
                    : new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "unknown" })
                .ToList()
        }).ToList();

        var kanaForms = kanaFormsRaw.Select(p => new KanaFormDto
        {
            Text = p.Text,
            AppliesToKanji = p.AppliesToKanji?.ToList() ?? new List<string>(),
            Tags = kanaTags.Where(t => t.ProperNounKanaId == p.Id)
                .Select(t => allTags.TryGetValue(t.TagCode, out var tag)
                    ? new TagInfoDto { Code = tag.Code, Description = tag.Description, Category = tag.Category }
                    : new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "unknown" })
                .ToList()
        }).ToList();

        var translations = translationsRaw.Select(p => new TranslationDto
        {
            Types = translationTypes.Where(t => t.TranslationId == p.Id)
                .Select(t => allTags.TryGetValue(t.TagCode, out var tag)
                    ? new TagInfoDto { Code = tag.Code, Description = tag.Description, Category = tag.Category }
                    : new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "unknown" })
                .ToList(),
            Related = translationRelated.Where(t => t.TranslationId == p.Id)
                .Select(t => new TranslationRelatedDto { Term = t.RelatedTerm, Reading = t.RelatedReading }).ToList(),
            Text = translationTexts.Where(t => t.TranslationId == p.Id)
                .Select(t => new TranslationTextDto { Language = t.Lang, Text = t.Text }).ToList()
        }).ToList();

        var furigana = furiganaTask.Result.Select(pf => new FuriganaDto
        {
            Text = pf.Text, Reading = pf.Reading,
            Furigana = pf.Furigana.Select(f => new FuriganaPartDto { Ruby = f.Ruby, Rt = f.Rt }).ToList()
        }).ToList();

        var containedKanjiForms = kanjis.Select(k => new KanjiInfoDto { Id = k.Id, Literal = k.Literal }).ToList();

        return new ProperNounDetailDto
        {
            Id = pnId, JmnedictId = properNoun.JmnedictId, KanjiForms = kanjiForms, KanaForms = kanaForms,
            Translations = translations, ContainedKanji = containedKanjiForms, Furigana = furigana, UpdatedAt = properNoun.UpdatedAt
        };
    }
}