using Microsoft.AspNetCore.Mvc;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Controllers;

/// <summary>
/// specialized controller for Kanji-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class KanjiController : ControllerBase {

    private readonly IKanjiService _kanjiService;

    public KanjiController(IKanjiService kanjiService) {
        _kanjiService = kanjiService;
    }

    /// <summary>
    /// Retrieves detailed information for a specific Kanji character.
    /// </summary>
    /// <param name="literal">The Kanji character to look up (e.g., "水").</param>
    /// <returns>Detailed Kanji information including readings, meanings, and strokes.</returns>
    /// <response code="200">Returns the requested Kanji details.</response>
    /// <response code="404">If the Kanji is not found.</response>
    [HttpGet("{literal}")]
    [ProducesResponseType(typeof(KanjiDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByLiteral(string literal) {
        var result = await _kanjiService.GetByLiteralAsync(literal);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Searches for Kanji based on a list of radicals.
    /// </summary>
    /// <param name="radicals">List of radical literals to search by.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Results per page (default 20).</param>
    /// <returns>A paginated list of Kanji matching the radicals.</returns>
    [HttpGet("search/by-radicals")]
    [ProducesResponseType(typeof(SearchResultKanji), 200)]
    public async Task<IActionResult> SearchByRadicals([FromQuery] List<string> radicals, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
        var result = await _kanjiService.SearchByRadicalsAsync(radicals, page, pageSize);
        return Ok(result);
    }
}