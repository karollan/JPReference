using Microsoft.AspNetCore.Mvc;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.DTOs.ProperNoun;

namespace JLPTReference.Api.Controllers;

/// <summary>
/// Controller for Proper Noun operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProperNounController : ControllerBase {
    private readonly IProperNounService _properNounService;

    public ProperNounController(IProperNounService properNounService) {
        _properNounService = properNounService;
    }

    /// <summary>
    /// Retrieves details for a specific proper noun by term.
    /// </summary>
    /// <param name="term">The proper noun term.</param>
    /// <returns>Detailed information about the proper noun.</returns>
    [HttpGet("{term}")]
    [ProducesResponseType(typeof(ProperNounDetailDto), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<IActionResult> GetProperNounDetailByTermAsync(string term) {
        var properNoun = await _properNounService.GetProperNounDetailByTermAsync(term);
        if (properNoun == null) return NotFound(new {
            message = $"Proper noun '{term}' not found"
        });
        return Ok(properNoun);
    }
}