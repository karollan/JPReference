using JLPTReference.Api.Data;
using JLPTReference.Api.DTOs.Vocabulary;
using Microsoft.EntityFrameworkCore;
using JLPTReference.Api.Repositories.Interfaces;

namespace JLPTReference.Api.Repositories.Implementations;

public class VocabularyRepository : IVocabularyRepository {

    private readonly ApplicationDBContext _context;
    public VocabularyRepository(ApplicationDBContext context) {
        _context = context;
    }

    public async Task<VocabularyDetailDto> GetVocabularyDetailByTermAsync(string term) {
        if (string.IsNullOrEmpty(term))
            throw new ArgumentException("Vocabulary term is required");

        // Find vocabulary by matching term in either VocabularyKanji or VocabularyKana
        var vocabularyId = await _context.VocabularyKanji
            .Where(vk => vk.Text == term)
            .Select(vk => vk.VocabularyId)
            .FirstOrDefaultAsync();

        if (vocabularyId == Guid.Empty)
        {
            vocabularyId = await _context.VocabularyKana
                .Where(vka => vka.Text == term)
                .Select(vka => vka.VocabularyId)
                .FirstOrDefaultAsync();
        }

        if (vocabularyId == Guid.Empty)
            throw new Exception($"Vocabulary '{term}' not found");

        // Load the vocabulary entity
        var vocabulary = await _context.Vocabulary
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vocabularyId);

        if (vocabulary == null)
            throw new Exception($"Vocabulary '{term}' not found");

        // Load all tags for lookups
        var allTags = await _context.Tags
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Code, t => t);

        // Load kanji forms with their tags
        var kanjiFormsRaw = await _context.VocabularyKanji
            .Where(vk => vk.VocabularyId == vocabularyId)
            .AsNoTracking()
            .ToListAsync();

        var kanjiFormIds = kanjiFormsRaw.Select(k => k.Id).ToList();
        var kanjiTags = await _context.VocabularyKanjiTags
            .Where(vkt => kanjiFormIds.Contains(vkt.VocabularyKanjiId))
            .AsNoTracking()
            .ToListAsync();

        var kanjiForms = kanjiFormsRaw.Select(vk => new KanjiFormDto
        {
            Text = vk.Text,
            IsCommon = vk.IsCommon,
            Tags = kanjiTags
                .Where(t => t.VocabularyKanjiId == vk.Id)
                .Select(t => allTags.TryGetValue(t.TagCode, out var tag) 
                    ? new TagInfoDto { Code = tag.Code, Description = tag.Description, Category = tag.Category }
                    : new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "unknown" })
                .ToList()
        }).ToList();

        // Load kana forms with their tags
        var kanaFormsRaw = await _context.VocabularyKana
            .Where(vka => vka.VocabularyId == vocabularyId)
            .AsNoTracking()
            .ToListAsync();

        var kanaFormIds = kanaFormsRaw.Select(k => k.Id).ToList();
        var kanaTags = await _context.VocabularyKanaTags
            .Where(vkat => kanaFormIds.Contains(vkat.VocabularyKanaId))
            .AsNoTracking()
            .ToListAsync();

        var kanaForms = kanaFormsRaw.Select(vka => new KanaFormDto
        {
            Text = vka.Text,
            IsCommon = vka.IsCommon,
            AppliesToKanji = vka.AppliesToKanji?.ToList() ?? new List<string>(),
            Tags = kanaTags
                .Where(t => t.VocabularyKanaId == vka.Id)
                .Select(t => allTags.TryGetValue(t.TagCode, out var tag)
                    ? new TagInfoDto { Code = tag.Code, Description = tag.Description, Category = tag.Category }
                    : new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "unknown" })
                .ToList()
        }).ToList();

        // Load senses with related data
        var sensesRaw = await _context.VocabularySenses
            .Where(vs => vs.VocabularyId == vocabularyId)
            .AsNoTracking()
            .ToListAsync();

        var senseIds = sensesRaw.Select(s => s.Id).ToList();

        var senseTags = await _context.VocabularySenseTags
            .Where(vst => senseIds.Contains(vst.SenseId))
            .AsNoTracking()
            .ToListAsync();

        var senseRelations = await _context.VocabularySenseRelations
            .Where(vsr => senseIds.Contains(vsr.SourceSenseId))
            .AsNoTracking()
            .ToListAsync();

        var senseLanguageSources = await _context.VocabularySenseLanguageSources
            .Where(vsls => senseIds.Contains(vsls.SenseId))
            .AsNoTracking()
            .ToListAsync();

        var senseGlosses = await _context.VocabularySenseGlosses
            .Where(vsg => senseIds.Contains(vsg.SenseId))
            .AsNoTracking()
            .ToListAsync();

        var senseExamples = await _context.VocabularySenseExamples
            .Where(vse => senseIds.Contains(vse.SenseId))
            .AsNoTracking()
            .ToListAsync();

        var exampleIds = senseExamples.Select(e => e.Id).ToList();
        var exampleSentences = await _context.VocabularySenseExampleSentences
            .Where(vses => exampleIds.Contains(vses.ExampleId))
            .AsNoTracking()
            .ToListAsync();

        var senses = sensesRaw.Select(vs => new SenseDto
        {
            AppliesToKanji = vs.AppliesToKanji?.ToList() ?? new List<string>(),
            AppliesToKana = vs.AppliesToKana?.ToList() ?? new List<string>(),
            Info = vs.Info?.ToList() ?? new List<string>(),
            Tags = senseTags
                .Where(t => t.SenseId == vs.Id)
                .Select(t => allTags.TryGetValue(t.TagCode, out var tag)
                    ? new TagInfoDto { Code = tag.Code, Description = tag.Description, Category = tag.Category, Type = t.TagType }
                    : new TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "unknown", Type = t.TagType })
                .ToList(),
            Relations = senseRelations
                .Where(r => r.SourceSenseId == vs.Id)
                .Select(r => new SenseRelationDto
                {
                    RelationId = r.TargetVocabId ?? Guid.Empty,
                    RelationSenseId = r.TargetSenseId ?? Guid.Empty,
                    Term = r.TargetTerm,
                    Reading = r.TargetReading,
                    RelationType = r.RelationType
                })
                .ToList(),
            LanguageSources = senseLanguageSources
                .Where(ls => ls.SenseId == vs.Id)
                .Select(ls => new SenseLanguageSourceDto
                {
                    Language = ls.Lang ?? string.Empty,
                    Text = ls.Text ?? string.Empty,
                    IsFull = ls.Full,
                    IsWaei = ls.Wasei
                })
                .ToList(),
            Glosses = senseGlosses
                .Where(g => g.SenseId == vs.Id)
                .Select(g => new SenseGlossDto
                {
                    Language = g.Lang,
                    Text = g.Text,
                    Gender = g.Gender,
                    Type = g.Type
                })
                .ToList(),
            Examples = senseExamples
                .Where(e => e.SenseId == vs.Id)
                .Select(e => new SenseExampleDto
                {
                    SourceType = e.SourceType ?? string.Empty,
                    SourceValue = e.SourceValue ?? string.Empty,
                    Text = e.Text,
                    Sentences = exampleSentences
                        .Where(s => s.ExampleId == e.Id)
                        .Select(s => new SenseExampleSentenceDto
                        {
                            Language = s.Lang,
                            Text = s.Text
                        })
                        .ToList()
                })
                .ToList()
        }).ToList();

        // Load kanji references
        var kanjiReferences = await _context.VocabularyUsesKanji
            .Where(vuk => vuk.VocabularyId == vocabularyId)
            .Include(vuk => vuk.Kanji)
            .AsNoTracking()
            .Select(vuk => new KanjiInfoDto
            {
                Id = vuk.Kanji.Id,
                Literal = vuk.Kanji.Literal
            })
            .ToListAsync();

        // Assemble the final DTO
        return new VocabularyDetailDto
        {
            Id = vocabulary.Id,
            JmdictId = vocabulary.JmdictId,
            JlptLevel = vocabulary.JlptLevelNew,
            KanjiForms = kanjiForms,
            KanaForms = kanaForms,
            Senses = senses,
            ContainedKanji = kanjiReferences
        };
    }
}