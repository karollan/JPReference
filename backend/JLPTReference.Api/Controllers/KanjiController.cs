using Microsoft.AspNetCore.Mvc;
using JLPTReference.Api.Services.Interfaces;

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
}