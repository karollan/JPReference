using Microsoft.AspNetCore.Mvc;
using JLPTReference.Api.Services.Interfaces;

namespace JLPTReference.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RadicalController : ControllerBase
{
    private readonly IRadicalService _radicalService;

    public RadicalController(IRadicalService radicalService)
    {
        _radicalService = radicalService;
    }

    [HttpGet("{literal}")]
    public async Task<IActionResult> GetRadicalByLiteral(string literal)
    {
        var radical = await _radicalService.GetRadicalByLiteralAsync(literal);
        return Ok(radical);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetRadicalsList()
    {
        var radicals = await _radicalService.GetRadicalsListAsync();
        return Ok(radicals);
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchKanjiByRadicals([FromBody] List<Guid> radicalIds)
    {
        var results = await _radicalService.SearchKanjiByRadicalsAsync(radicalIds);
        return Ok(results);
    }
}