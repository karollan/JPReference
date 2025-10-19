using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JLPTReference.Api.Models;
using JLPTReference.Api.Data;
using System.Globalization;
using System.Text;

namespace JLPTReference.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KanjiController : ControllerBase
{
    private readonly ApplicationDBContext _context;

    public KanjiController(ApplicationDBContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Normalizes a string for search comparison by removing diacritics, converting to lowercase,
    /// and handling various Unicode normalizations for better internationalization support.
    /// </summary>
    /// <param name="input">The input string to normalize</param>
    /// <returns>Normalized string suitable for search comparison</returns>
    private static string NormalizeSearchString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Normalize Unicode to NFD (decomposed form) to separate base characters from diacritics
        var normalized = input.Normalize(NormalizationForm.FormD);
        
        var stringBuilder = new StringBuilder();
        foreach (var c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory == UnicodeCategory.NonSpacingMark)
                continue;
                
            if (c >= 0xFF01 && c <= 0xFF5E)
            {
                stringBuilder.Append((char)(c - 0xFF01 + 0x21));
            }
            else
            {
                stringBuilder.Append(c);
            }
        }
        return stringBuilder.ToString().ToLowerInvariant().Trim();
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetKanjiList(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] List<int>? jlptLevel = null
    )
    {
        // If search is provided, we need to do in-memory filtering for proper Unicode normalization
        if (!string.IsNullOrEmpty(search))
        {
            var normalizedSearch = NormalizeSearchString(search);            
            // Load all kanjis for proper Unicode normalization
            var allKanjis = await _context.Kanji.ToListAsync();
            
            var filteredKanjis = allKanjis.Where(k => 
                NormalizeSearchString(k.Character).Contains(normalizedSearch) ||
                (k.ReadingsKun != null && k.ReadingsKun.Any(r => NormalizeSearchString(r).Contains(normalizedSearch))) ||
                (k.ReadingsOn != null && k.ReadingsOn.Any(r => NormalizeSearchString(r).Contains(normalizedSearch))) ||
                k.Meanings.Any(m => NormalizeSearchString(m).Contains(normalizedSearch))
            );
            
            // Apply JLPT level filter if specified
            if (jlptLevel != null && jlptLevel.Any())
            {
                filteredKanjis = filteredKanjis.Where(k => jlptLevel.Contains(0) ? k.JlptNew == null : k.JlptNew.HasValue && jlptLevel.Contains(k.JlptNew.Value));
            }
            
            var searchTotalCount = filteredKanjis.Count();
            var searchKanjis = filteredKanjis
                .OrderBy(k => k.Character)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var searchResult = new
            {
                items = searchKanjis,
                totalCount = searchTotalCount,
                page = page,
                pageSize = pageSize
            };

            return Ok(searchResult);
        }

        // No search - use efficient database query
        var query = _context.Kanji.AsQueryable();
        
        if (jlptLevel != null && jlptLevel.Any())
        {
            query = query.Where(k => jlptLevel.Contains(0) ? k.JlptNew == null : k.JlptNew.HasValue && jlptLevel.Contains(k.JlptNew.Value));
        }

        var totalCount = await query.CountAsync();
        
        var kanjis = await query
            .OrderBy(k => k.Frequency)
            .Skip((page-1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new
        {
            items = kanjis,
            totalCount = totalCount,
            page = page,
            pageSize = pageSize
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Kanji>> GetKanji(Guid id)
    {
        var kanji = await _context.Kanji.FindAsync(id);
        if (kanji == null) return NotFound();

        return kanji;
    }
}