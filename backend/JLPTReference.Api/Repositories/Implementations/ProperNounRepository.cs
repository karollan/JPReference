using JLPTReference.Api.Data;
using JLPTReference.Api.DTOs.ProperNoun;
using JLPTReference.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Repositories.Implementations;
public class ProperNounRepository : IProperNounRepository {
    private readonly ApplicationDBContext _context;

    public ProperNounRepository(ApplicationDBContext context) {
        this._context = context;
    }

    public async Task<ProperNounDetailDto> GetProperNounDetailByTermAsync(string term) {
        if (string.IsNullOrEmpty(term)) {
            throw new ArgumentException("Proper noun term is required");
        }

        var properNounId = await _context.ProperNounKanji
            .Where(p => p.Text == term)
            .Select(p => p.ProperNounId)
            .FirstOrDefaultAsync();

        if (properNounId == Guid.Empty) {
            properNounId = await _context.ProperNounKana
                .Where(p => p.Text == term)
                .Select(p => p.ProperNounId)
                .FirstOrDefaultAsync();
        }

        if (properNounId == Guid.Empty) {
            throw new Exception($"Proper noun '{term}' not found");
        }

        var properNoun = await _context.ProperNoun
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == properNounId);

        if (properNoun == null) {
            throw new Exception($"Proper noun '{term}' not found");
        }

        var allTags = await _context.Tags
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Code, t => t);
    
        var kanjiFormsRaw = await _context.ProperNounKanji
            .Where(p => p.ProperNounId == properNounId)
            .AsNoTracking()
            .ToListAsync();

        var kanjiFormIds = kanjiFormsRaw.Select(k => k.Id).ToList();
        var kanjiTags = await _context.ProperNounKanjiTags
            .Where(p => kanjiFormIds.Contains(p.ProperNounKanjiId))
            .AsNoTracking()
            .ToListAsync();

        var kanjiForms = kanjiFormsRaw.Select(p => new KanjiFormDto
        {
            Text = p.Text,
            Tags = kanjiTags
                .Where(t => t.ProperNounKanjiId == p.Id)
                .Select(t => allTags.TryGetValue(t.TagCode, out var tag)
                    ? new DTOs.Vocabulary.TagInfoDto { Code = tag.Code, Description = tag.Description, Category = tag.Category }
                    : new DTOs.Vocabulary.TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "unknown" })
                .ToList()
        }).ToList();

        var kanaFormsRaw = await _context.ProperNounKana
            .Where(p => p.ProperNounId == properNounId)
            .AsNoTracking()
            .ToListAsync();

        var kanaFormIds = kanaFormsRaw.Select(k => k.Id).ToList();
        var kanaTags = await _context.ProperNounKanaTags
            .Where(p => kanaFormIds.Contains(p.ProperNounKanaId))
            .AsNoTracking()
            .ToListAsync();

        var kanaForms = kanaFormsRaw.Select(p => new KanaFormDto
        {
            Text = p.Text,
            AppliesToKanji = p.AppliesToKanji?.ToList() ?? new List<string>(),
            Tags = kanaTags
                .Where(t => t.ProperNounKanaId == p.Id)
                .Select(t => allTags.TryGetValue(t.TagCode, out var tag)
                    ? new DTOs.Vocabulary.TagInfoDto { Code = tag.Code, Description = tag.Description, Category = tag.Category }
                    : new DTOs.Vocabulary.TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "unknown" })
                .ToList()
        }).ToList();
        
        var translationsRaw = await _context.ProperNounTranslations
            .Where(p => p.ProperNounId == properNounId)
            .AsNoTracking()
            .ToListAsync();

        var translationIds = translationsRaw.Select(t => t.Id).ToList();
        var translationTypes = await _context.ProperNounTranslationTypes
            .Where(p => translationIds.Contains(p.TranslationId))
            .AsNoTracking()
            .ToListAsync();
        var translationTexts = await _context.ProperNounTranslationTexts
            .Where(p => translationIds.Contains(p.TranslationId))
            .AsNoTracking()
            .ToListAsync();
        var translationRelated = await _context.ProperNounTranslationRelated
            .Where(p => translationIds.Contains(p.TranslationId))
            .AsNoTracking()
            .ToListAsync();
        var translations = translationsRaw.Select(p => new TranslationDto
        {
            Types = translationTypes
                .Where(t => t.TranslationId == p.Id)
                .Select(t => allTags.TryGetValue(t.TagCode, out var tag)
                    ? new DTOs.Vocabulary.TagInfoDto { Code = tag.Code, Description = tag.Description, Category = tag.Category }
                    : new DTOs.Vocabulary.TagInfoDto { Code = t.TagCode, Description = t.TagCode, Category = "unknown" })
                .ToList(),
            Related = translationRelated
                .Where(t => t.TranslationId == p.Id)
                .Select(t => new TranslationRelatedDto
                {
                    Term = t.RelatedTerm,
                    Reading = t.RelatedReading,
                })
                .ToList(),
            Text = translationTexts
                .Where(t => t.TranslationId == p.Id)
                .Select(t => new TranslationTextDto
                {
                    Language = t.Lang,
                    Text = t.Text,
                })
                .ToList()
        }).ToList();

        var containedKanji = await _context.ProperNounUsesKanji
            .Where(p => p.ProperNounId == properNounId)
            .AsNoTracking()
            .ToListAsync();
        var containedKanjiIds = containedKanji.Select(k => k.KanjiId).ToList();
        var kanjis = await _context.Kanji
            .Where(k => containedKanjiIds.Contains(k.Id))
            .AsNoTracking()
            .ToListAsync();
        var containedKanjiForms = kanjis.Select(k => new KanjiInfoDto
        {
            Id = k.Id,
            Literal = k.Literal,
        }).ToList();

        return new ProperNounDetailDto
        {
            Id = properNounId,
            JmnedictId = properNoun.JmnedictId,
            KanjiForms = kanjiForms,
            KanaForms = kanaForms,
            Translations = translations,
            ContainedKanji = containedKanjiForms,
        };
    }
}