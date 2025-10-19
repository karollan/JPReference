using JLPTReference.Api.Models;
using JLPTReference.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VocabularyController : ControllerBase
{
    private readonly ApplicationDBContext _context;

    public VocabularyController(ApplicationDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VocabularyDto>>> GetVocabulary(
        [FromQuery] string? search = null,
        [FromQuery] int? jlptLevel = null,
        [FromQuery] bool? isCommon = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.Vocabulary
            .Include(v => v.Examples)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(v => 
                (v.Kanji != null && v.Kanji.Any(k => k.Contains(search))) ||
                (v.Kana != null && v.Kana.Any(k => k.Contains(search))) ||
                (v.Gloss != null && v.Gloss.Any(g => g.Contains(search))));
        }

        if (jlptLevel.HasValue)
        {
            query = query.Where(v => v.JlptNew == jlptLevel.Value || v.JlptOld == jlptLevel.Value);
        }

        if (isCommon.HasValue)
        {
            query = query.Where(v => v.IsCommon == isCommon.Value);
        }

        // Apply pagination
        var totalCount = await query.CountAsync();
        var vocabulary = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vocabularyDtos = vocabulary.Select(v => new VocabularyDto
        {
            JmdictId = v.JmdictId,
            Kanji = v.Kanji,
            Kana = v.Kana,
            PartOfSpeech = v.PartOfSpeech,
            Field = v.Field,
            Dialect = v.Dialect,
            Misc = v.Misc,
            Info = v.Info,
            LanguageSource = v.LanguageSource,
            Gloss = v.Gloss,
            GlossLanguages = v.GlossLanguages,
            Related = v.Related,
            Antonym = v.Antonym,
            IsCommon = v.IsCommon,
            JlptOld = v.JlptOld,
            JlptNew = v.JlptNew,
            Examples = v.Examples.Select(e => new VocabularyExampleDto
            {
                Source = e.Source,
                Text = e.Text,
                JapaneseSentences = e.JapaneseSentences,
                EnglishSentences = e.EnglishSentences
            }).ToArray()
        });

        // Add pagination headers
        Response.Headers.Add("X-Total-Count", totalCount.ToString());
        Response.Headers.Add("X-Page", page.ToString());
        Response.Headers.Add("X-Page-Size", pageSize.ToString());

        return Ok(vocabularyDtos);
    }

    [HttpGet("{jmdictId}")]
    public async Task<ActionResult<VocabularyDto>> GetVocabulary(string jmdictId)
    {
        var vocabulary = await _context.Vocabulary
            .Include(v => v.Examples)
            .FirstOrDefaultAsync(v => v.JmdictId == jmdictId);

        if (vocabulary == null)
        {
            return NotFound();
        }

        var vocabularyDto = new VocabularyDto
        {
            JmdictId = vocabulary.JmdictId,
            Kanji = vocabulary.Kanji,
            Kana = vocabulary.Kana,
            PartOfSpeech = vocabulary.PartOfSpeech,
            Field = vocabulary.Field,
            Dialect = vocabulary.Dialect,
            Misc = vocabulary.Misc,
            Info = vocabulary.Info,
            LanguageSource = vocabulary.LanguageSource,
            Gloss = vocabulary.Gloss,
            GlossLanguages = vocabulary.GlossLanguages,
            Related = vocabulary.Related,
            Antonym = vocabulary.Antonym,
            IsCommon = vocabulary.IsCommon,
            JlptOld = vocabulary.JlptOld,
            JlptNew = vocabulary.JlptNew,
            Examples = vocabulary.Examples.Select(e => new VocabularyExampleDto
            {
                Source = e.Source,
                Text = e.Text,
                JapaneseSentences = e.JapaneseSentences,
                EnglishSentences = e.EnglishSentences
            }).ToArray()
        };

        return Ok(vocabularyDto);
    }

    [HttpGet("jlpt/{level}")]
    public async Task<ActionResult<IEnumerable<VocabularyDto>>> GetVocabularyByJlptLevel(int level)
    {
        var vocabulary = await _context.Vocabulary
            .Include(v => v.Examples)
            .Where(v => v.JlptNew == level || v.JlptOld == level)
            .ToListAsync();

        var vocabularyDtos = vocabulary.Select(v => new VocabularyDto
        {
            JmdictId = v.JmdictId,
            Kanji = v.Kanji,
            Kana = v.Kana,
            PartOfSpeech = v.PartOfSpeech,
            Field = v.Field,
            Dialect = v.Dialect,
            Misc = v.Misc,
            Info = v.Info,
            LanguageSource = v.LanguageSource,
            Gloss = v.Gloss,
            GlossLanguages = v.GlossLanguages,
            Related = v.Related,
            Antonym = v.Antonym,
            IsCommon = v.IsCommon,
            JlptOld = v.JlptOld,
            JlptNew = v.JlptNew,
            Examples = v.Examples.Select(e => new VocabularyExampleDto
            {
                Source = e.Source,
                Text = e.Text,
                JapaneseSentences = e.JapaneseSentences,
                EnglishSentences = e.EnglishSentences
            }).ToArray()
        });

        return Ok(vocabularyDtos);
    }

    [HttpGet("common")]
    public async Task<ActionResult<IEnumerable<VocabularyDto>>> GetCommonVocabulary(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.Vocabulary
            .Include(v => v.Examples)
            .Where(v => v.IsCommon);

        var totalCount = await query.CountAsync();
        var vocabulary = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vocabularyDtos = vocabulary.Select(v => new VocabularyDto
        {
            JmdictId = v.JmdictId,
            Kanji = v.Kanji,
            Kana = v.Kana,
            PartOfSpeech = v.PartOfSpeech,
            Field = v.Field,
            Dialect = v.Dialect,
            Misc = v.Misc,
            Info = v.Info,
            LanguageSource = v.LanguageSource,
            Gloss = v.Gloss,
            GlossLanguages = v.GlossLanguages,
            Related = v.Related,
            Antonym = v.Antonym,
            IsCommon = v.IsCommon,
            JlptOld = v.JlptOld,
            JlptNew = v.JlptNew,
            Examples = v.Examples.Select(e => new VocabularyExampleDto
            {
                Source = e.Source,
                Text = e.Text,
                JapaneseSentences = e.JapaneseSentences,
                EnglishSentences = e.EnglishSentences
            }).ToArray()
        });

        Response.Headers.Add("X-Total-Count", totalCount.ToString());
        Response.Headers.Add("X-Page", page.ToString());
        Response.Headers.Add("X-Page-Size", pageSize.ToString());

        return Ok(vocabularyDtos);
    }
}
