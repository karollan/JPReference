using JLPTReference.Api.Data;
using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Radical;
using Microsoft.EntityFrameworkCore;
using JLPTReference.Api.Repositories.Interfaces;
namespace JLPTReference.Api.Repositories.Implementations;
public class KanjiRepository : IKanjiRepository {

    private readonly ApplicationDBContext _context;
    public KanjiRepository(ApplicationDBContext context) {
        _context = context;
    }

    public async Task<KanjiDetailDto> GetKanjiDetailByLiteralAsync(string literal) {
        var kanji = await _context.Kanji
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Literal == literal);

        if (kanji == null)
            throw new Exception($"Kanji '{literal}' not found");

        // 2️⃣ Load related collections separately
        var meanings = await _context.KanjiMeanings
            .Where(m => m.KanjiId == kanji.Id)
            .AsNoTracking()
            .Select(m => new KanjiMeaningDto
            {
                Language = m.Lang,
                Meaning = m.Value
            })
            .ToListAsync();

        var readings = await _context.KanjiReadings
            .Where(r => r.KanjiId == kanji.Id)
            .AsNoTracking()
            .Select(r => new KanjiReadingDto
            {
                Type = r.Type,
                Value = r.Value,
                Status = r.Status,
                OnType = r.OnType
            })
            .ToListAsync();

        var codepoints = await _context.KanjiCodepoints
            .Where(c => c.KanjiId == kanji.Id)
            .AsNoTracking()
            .Select(c => new KanjiCodepointDto
            {
                Type = c.Type,
                Value = c.Value
            })
            .ToListAsync();

        var dictionaryReferences = await _context.KanjiDictionaryReferences
            .Where(d => d.KanjiId == kanji.Id)
            .AsNoTracking()
            .Select(d => new KanjiDictionaryReferenceDto
            {
                Type = d.Type,
                Value = d.Value,
                MorohashiVolume = d.MorohashiVolume,
                MorohashiPage = d.MorohashiPage
            })
            .ToListAsync();

        var queryCodes = await _context.KanjiQueryCodes
            .Where(q => q.KanjiId == kanji.Id)
            .AsNoTracking()
            .Select(q => new KanjiQueryCodeDto
            {
                Type = q.Type,
                Value = q.Value,
                SkipMisclassification = q.SkipMissclassification
            })
            .ToListAsync();

        var nanori = await _context.KanjiNanori
            .Where(n => n.KanjiId == kanji.Id)
            .AsNoTracking()
            .Select(n => new KanjiNanoriDto
            {
                Value = n.Value
            })
            .ToListAsync();

        var radicals = await _context.KanjiRadicals
            .Where(r => r.KanjiId == kanji.Id)
            .AsNoTracking()
            .Select(r => new RadicalSummaryDto
            {
                Id = r.Radical.Id,
                Literal = r.Radical.Literal
            })
            .ToListAsync();

        var vocabularyReferences = await _context.VocabularyUsesKanji
            .Where(v => v.KanjiId == kanji.Id)
            .Include(v => v.Vocabulary)
                .ThenInclude(v => v.Kanji)
            .Include(v => v.Vocabulary)
                .ThenInclude(v => v.Kana)
            .AsNoTracking()
            .ToListAsync();

        // 3️⃣ Map VocabularyReferences with the "first Kanji or first Kana" logic
        var vocabDtos = vocabularyReferences.Select(v => new KanjiVocabularyDto
        {
            Id = v.Id,
            KanjiId = v.KanjiId,
            VocabularyId = v.VocabularyId,
            Term = (v.Vocabulary.Kanji.Any()
                    ? v.Vocabulary.Kanji.Select(k => k.Text).FirstOrDefault()
                    : v.Vocabulary.Kana.Select(k => k.Text).FirstOrDefault()) ?? string.Empty
        }).ToList();

        // 4️⃣ Assemble the final DTO
        var dto = new KanjiDetailDto
        {
            Id = kanji.Id,
            Literal = kanji.Literal,
            StrokeCount = kanji.StrokeCount,
            Frequency = kanji.Frequency,
            Grade = kanji.Grade,
            JlptLevel = kanji.JlptLevelNew,
            Meanings = meanings,
            Readings = readings,
            Codepoints = codepoints,
            DictionaryReferences = dictionaryReferences,
            QueryCodes = queryCodes,
            Nanori = nanori,
            Radicals = radicals,
            VocabularyReferences = vocabDtos
        };

        if (dto == null) {
            throw new Exception("Kanji not found");
        }

        return dto;
    }
}