using JLPTReference.Api.Data;
using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Radical;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Services.Search.QueryBuilder;
using Microsoft.EntityFrameworkCore;
using JLPTReference.Api.Repositories.Interfaces;
namespace JLPTReference.Api.Repositories.Implementations;
public class KanjiRepository : IKanjiRepository {

    private readonly ApplicationDBContext _context;
    private readonly IVocabularySearchService _vocabularySearchService;

    public KanjiRepository(ApplicationDBContext context, IVocabularySearchService vocabularySearchService) {
        _context = context;
        _vocabularySearchService = vocabularySearchService;
    }

    public async Task<KanjiDetailDto> GetKanjiDetailByLiteralAsync(string literal) {
        var kanji = await _context.Kanji
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Literal == literal);

        if (kanji == null)
            throw new Exception($"Kanji '{literal}' not found");

        // Load related collections separately
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
        
        var vocab = await _vocabularySearchService.SearchAsync(
            new SearchSpec{
                Filters = new SearchFilters{
                    Languages = new List<string>{ "eng" },
                },
                Tokens = new List<SearchToken>{
                    new SearchToken{
                        RawValue = literal + '%',
                        Variants = new List<string>(),
                        HasWildcard = true,
                        TransliterationBlocked = true
                    }
                }
            },
            5,
            1
        );

        // Assemble the final DTO
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
            VocabularyReferences = new KanjiVocabularyDto{
                TotalCount = vocabularyReferences.Count,
                Vocabulary = vocab.Data
            }
        };

        if (dto == null) {
            throw new Exception("Kanji not found");
        }

        return dto;
    }
}