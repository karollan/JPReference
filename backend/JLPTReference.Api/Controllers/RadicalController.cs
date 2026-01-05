using Microsoft.AspNetCore.Mvc;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.DTOs.Radical;
using JLPTReference.Api.DTOs.Kanji;

namespace JLPTReference.Api.Controllers;

/// <summary>
/// specialized controller for Radical-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RadicalController : ControllerBase
{
    private readonly IRadicalService _radicalService;

    public RadicalController(IRadicalService radicalService)
    {
        _radicalService = radicalService;
    }

    /// <summary>
    /// Retrieves detailed information for a specific radical.
    /// </summary>
    /// <param name="literal">The radical literal (e.g., "ERROR: The key "一" does not exist in the source.").</param>
    /// <returns>Detailed radical information.</returns>
    [HttpGet("{literal}")]
    [ProducesResponseType(typeof(RadicalDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetRadicalByLiteral(string literal)
    {
        var radical = await _radicalService.GetRadicalByLiteralAsync(literal);
        if (radical == null) return NotFound();
        return Ok(radical);
    }

    /// <summary>
    /// Retrieves a list of all available radicals.
    /// </summary>
    /// <returns>A list of radical summaries.</returns>
    [HttpGet("list")]
    [ProducesResponseType(typeof(List<RadicalSummaryDto>), 200)]
    public async Task<IActionResult> GetRadicalsList()
    {
        var radicals = await _radicalService.GetRadicalsListAsync();
        return Ok(radicals);
    }

    /// <summary>
    /// Searches for Kanji that contain the specified radicals.
    /// </summary>
    /// <param name="radicalIds">List of radical IDs to search by.</param>
    /// <returns>Search results containing Kanji matches.</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(RadicalSearchResultDto), 200)]
    public async Task<IActionResult> SearchKanjiByRadicals([FromBody] List<Guid> radicalIds)
    {
        var results = await _radicalService.SearchKanjiByRadicalsAsync(radicalIds);
        return Ok(results);
    }
}