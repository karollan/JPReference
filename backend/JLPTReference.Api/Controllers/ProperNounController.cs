using Microsoft.AspNetCore.Mvc;
using JLPTReference.Api.Services.Interfaces;

namespace JLPTReference.Api.Controllers;

[ApiController]
[Route("api/proper-noun")]
public class ProperNounController : ControllerBase {
    private readonly IProperNounService _properNounService;

    public ProperNounController(IProperNounService properNounService) {
        _properNounService = properNounService;
    }

    [HttpGet("{term}")]
    public async Task<IActionResult> GetProperNounDetailByTermAsync(string term) {
        var properNoun = await _properNounService.GetProperNounDetailByTermAsync(term);
        return Ok(properNoun);
    }
}