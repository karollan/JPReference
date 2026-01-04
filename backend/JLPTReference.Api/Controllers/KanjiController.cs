using Microsoft.AspNetCore.Mvc;
using JLPTReference.Api.Services.Interfaces;

namespace JLPTReference.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KanjiController : ControllerBase {

    private readonly IKanjiService _kanjiService;

    public KanjiController(IKanjiService kanjiService) {
        _kanjiService = kanjiService;
    }

    [HttpGet("{literal}")]
    public async Task<IActionResult> GetByLiteral(string literal) {
        var result = await _kanjiService.GetByLiteralAsync(literal);
        return Ok(result);
    }

    [HttpGet("search/by-radicals")]
    public async Task<IActionResult> SearchByRadicals([FromQuery] List<string> radicals, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
        var result = await _kanjiService.SearchByRadicalsAsync(radicals, page, pageSize);
        return Ok(result);
    }
}