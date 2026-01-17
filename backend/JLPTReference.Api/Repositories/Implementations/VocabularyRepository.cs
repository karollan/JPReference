using System.Text.RegularExpressions;
using JLPTReference.Api.Data;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.DTOs.Common;
using Microsoft.EntityFrameworkCore;
using JLPTReference.Api.Repositories.Interfaces;

namespace JLPTReference.Api.Repositories.Implementations;

public class VocabularyRepository : IVocabularyRepository
{
    private readonly IDbContextFactory<ApplicationDBContext> _contextFactory;
    private readonly ICachedTagRepository _cachedTagRepository;

    // Pattern to match term(reading) format, e.g., "中(なか)"
    private static readonly Regex TermWithReadingPattern = new(@"^(.+)\(([^)]+)\)$", RegexOptions.Compiled);

    public VocabularyRepository(IDbContextFactory<ApplicationDBContext> contextFactory, ICachedTagRepository cachedTagRepository)
    {
        _contextFactory = contextFactory;
        _cachedTagRepository = cachedTagRepository;
    }

    /// <summary>
    /// Parses a term that may include a reading in parentheses.
    /// Examples: "食べる" -> ("食べる", null), "中(なか)" -> ("中", "なか")
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

    public async Task<VocabularyDetailDto?> GetVocabularyDetailByTermAsync(string term)
    {
        if (string.IsNullOrEmpty(term))
            throw new ArgumentException("Vocabulary term is required");

        // Parse term to extract optional reading: "中(なか)" -> term="中", reading="なか"
        var (searchTerm, reading) = ParseTermWithReading(term);

        // Single-query ID lookup via SQL function
        await using var idContext = await _contextFactory.CreateDbContextAsync();
        var vocabularyId = await idContext.Database
            .SqlQuery<Guid?>($"SELECT jlpt.find_vocabulary_id_by_term({searchTerm}, {reading}) AS \"Value\"")
            .FirstOrDefaultAsync();

        if (vocabularyId == null || vocabularyId == Guid.Empty)
            return null;

        var vocabId = vocabularyId.Value;

        // Load cached tags (shared across requests)
        var allTags = await _cachedTagRepository.GetTagsDictionaryAsync();

        // Parallel batch 1: Core data using separate contexts
        var vocabularyTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.Vocabulary.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vocabId);
        });

        var kanjiFormsRawTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.VocabularyKanji.Where(vk => vk.VocabularyId == vocabId).AsNoTracking().ToListAsync();
        });

        var kanaFormsRawTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.VocabularyKana.Where(vka => vka.VocabularyId == vocabId).AsNoTracking().ToListAsync();
        });

        var sensesRawTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.VocabularySenses.Where(vs => vs.VocabularyId == vocabId).AsNoTracking().ToListAsync();
        });

        var furiganaTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.VocabularyFurigana.Where(vf => vf.VocabularyId == vocabId).AsNoTracking().ToListAsync();
        });

        var kanjiReferencesTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.VocabularyUsesKanji.Where(vuk => vuk.VocabularyId == vocabId)
                .Include(vuk => vuk.Kanji).AsNoTracking()
                .Select(vuk => new KanjiInfoDto { Id = vuk.Kanji.Id, Literal = vuk.Kanji.Literal })
                .ToListAsync();
        });

        await Task.WhenAll(vocabularyTask, kanjiFormsRawTask, kanaFormsRawTask, sensesRawTask, furiganaTask, kanjiReferencesTask);

        var vocabulary = vocabularyTask.Result;
        if (vocabulary == null)
            return null;

        var kanjiFormsRaw = kanjiFormsRawTask.Result;
        var kanaFormsRaw = kanaFormsRawTask.Result;
        var sensesRaw = sensesRawTask.Result;

        // Get IDs for tag queries
        var kanjiFormIds = kanjiFormsRaw.Select(k => k.Id).ToList();
        var kanaFormIds = kanaFormsRaw.Select(k => k.Id).ToList();
        var senseIds = sensesRaw.Select(s => s.Id).ToList();

        // Parallel batch 2: Tags and related data
        var kanjiTagsTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.VocabularyKanjiTags.Where(vkt => kanjiFormIds.Contains(vkt.VocabularyKanjiId)).AsNoTracking().ToListAsync();
        });

        var kanaTagsTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.VocabularyKanaTags.Where(vkat => kanaFormIds.Contains(vkat.VocabularyKanaId)).AsNoTracking().ToListAsync();
        });

        var senseTagsTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.VocabularySenseTags.Where(vst => senseIds.Contains(vst.SenseId)).AsNoTracking().ToListAsync();
        });

        var senseRelationsTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.VocabularySenseRelations.Where(vsr => senseIds.Contains(vsr.SourceSenseId)).AsNoTracking().ToListAsync();
        });

        var senseLanguageSourcesTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.VocabularySenseLanguageSources.Where(vsls => senseIds.Contains(vsls.SenseId)).AsNoTracking().ToListAsync();
        });

        var senseGlossesTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.VocabularySenseGlosses.Where(vsg => senseIds.Contains(vsg.SenseId)).AsNoTracking().ToListAsync();
        });

        var senseExamplesTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.VocabularySenseExamples.Where(vse => senseIds.Contains(vse.SenseId)).AsNoTracking().ToListAsync();
        });

        await Task.WhenAll(kanjiTagsTask, kanaTagsTask, senseTagsTask, senseRelationsTask, senseLanguageSourcesTask, senseGlossesTask, senseExamplesTask);

        var kanjiTags = kanjiTagsTask.Result;
        var kanaTags = kanaTagsTask.Result;
        var senseTags = senseTagsTask.Result;
        var senseRelations = senseRelationsTask.Result;
        var senseLanguageSources = senseLanguageSourcesTask.Result;
        var senseGlosses = senseGlossesTask.Result;
        var senseExamples = senseExamplesTask.Result;

        // Load example sentences
        var exampleIds = senseExamples.Select(e => e.Id).ToList();
        List<Entities.Vocabulary.VocabularySenseExampleSentence> exampleSentences;
        await using (var ctx = await _contextFactory.CreateDbContextAsync())
        {
            exampleSentences = await ctx.VocabularySenseExampleSentences
                .Where(vses => exampleIds.Contains(vses.ExampleId)).AsNoTracking().ToListAsync();
        }

        // Build DTOs
        var kanjiForms = kanjiFormsRaw.Select(vk => new KanjiFormDto
        {
            Text = vk.Text,
            IsCommon = vk.IsCommon,
            Tags = kanjiTags.Where(t => t.VocabularyKanjiId == vk.Id)
                .Select(t => allTags.TryGetValue(t.TagCode, out var tag)
                    ? new TagInfoDto { Code = tag.Code, Description = tag.Description, Category = tag.Category }
                    : new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "unknown" })
                .ToList()
        }).ToList();

        var kanaForms = kanaFormsRaw.Select(vka => new KanaFormDto
        {
            Text = vka.Text,
            IsCommon = vka.IsCommon,
            AppliesToKanji = vka.AppliesToKanji?.ToList() ?? new List<string>(),
            Tags = kanaTags.Where(t => t.VocabularyKanaId == vka.Id)
                .Select(t => allTags.TryGetValue(t.TagCode, out var tag)
                    ? new TagInfoDto { Code = tag.Code, Description = tag.Description, Category = tag.Category }
                    : new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "unknown" })
                .ToList()
        }).ToList();

        var senses = sensesRaw.Select(vs => new SenseDto
        {
            AppliesToKanji = vs.AppliesToKanji?.ToList() ?? new List<string>(),
            AppliesToKana = vs.AppliesToKana?.ToList() ?? new List<string>(),
            Info = vs.Info?.ToList() ?? new List<string>(),
            Tags = senseTags.Where(t => t.SenseId == vs.Id)
                .Select(t => allTags.TryGetValue(t.TagCode, out var tag)
                    ? new TagInfoDto { Code = tag.Code, Description = tag.Description, Category = tag.Category, Type = t.TagType }
                    : new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "unknown", Type = t.TagType })
                .ToList(),
            Relations = senseRelations.Where(r => r.SourceSenseId == vs.Id)
                .Select(r => new SenseRelationDto { RelationId = r.TargetVocabId ?? Guid.Empty, RelationSenseId = r.TargetSenseId ?? Guid.Empty, Term = r.TargetTerm, Reading = r.TargetReading, RelationType = r.RelationType })
                .ToList(),
            LanguageSources = senseLanguageSources.Where(ls => ls.SenseId == vs.Id)
                .Select(ls => new SenseLanguageSourceDto { Language = ls.Lang ?? string.Empty, Text = ls.Text ?? string.Empty, IsFull = ls.Full, IsWaei = ls.Wasei })
                .ToList(),
            Glosses = senseGlosses.Where(g => g.SenseId == vs.Id)
                .Select(g => new SenseGlossDto { Language = g.Lang, Text = g.Text, Gender = g.Gender, Type = g.Type })
                .ToList(),
            Examples = senseExamples.Where(e => e.SenseId == vs.Id)
                .Select(e => new SenseExampleDto
                {
                    SourceType = e.SourceType ?? string.Empty, SourceValue = e.SourceValue ?? string.Empty, Text = e.Text,
                    Sentences = exampleSentences.Where(s => s.ExampleId == e.Id)
                        .Select(s => new SenseExampleSentenceDto { Language = s.Lang, Text = s.Text }).ToList()
                }).ToList()
        }).ToList();

        var furigana = furiganaTask.Result.Select(vf => new FuriganaDto
        {
            Text = vf.Text, Reading = vf.Reading,
            Furigana = vf.Furigana.Select(f => new FuriganaPartDto { Ruby = f.Ruby, Rt = f.Rt }).ToList()
        }).ToList();

        return new VocabularyDetailDto
        {
            Id = vocabulary.Id, JmdictId = vocabulary.JmdictId, JlptLevel = vocabulary.JlptLevelNew,
            KanjiForms = kanjiForms, KanaForms = kanaForms, Senses = senses,
            ContainedKanji = kanjiReferencesTask.Result, Furigana = furigana, UpdatedAt = vocabulary.UpdatedAt
        };
    }
}