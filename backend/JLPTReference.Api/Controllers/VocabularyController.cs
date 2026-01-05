using Microsoft.AspNetCore.Mvc;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.Services.Interfaces;
namespace JLPTReference.Api.Controllers;


/// <summary>
/// specialized controller for Vocabulary-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class VocabularyController : ControllerBase {
    private readonly IVocabularyService _vocabularyService;
    public VocabularyController(IVocabularyService vocabularyService) {
        _vocabularyService = vocabularyService;
    }

    /// <summary>
    /// Retrieves detailed information for a specific vocabulary term.
    /// </summary>
    /// <param name="term">The vocabulary term.</param>
    /// <returns>Detailed vocabulary information.</returns>
    [HttpGet("{term}")]
    [ProducesResponseType(typeof(VocabularyDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<VocabularyDetailDto>> GetVocabularyDetailByTermAsync(string term) {
        var result = await _vocabularyService.GetVocabularyDetailByTermAsync(term);
        if (result == null) return NotFound();
        return result;
    }
}