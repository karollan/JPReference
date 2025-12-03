using Microsoft.AspNetCore.Mvc;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.Services.Interfaces;
namespace JLPTReference.Api.Controllers;


[ApiController]
[Route("api/vocabulary")]
public class VocabularyController : ControllerBase {
    private readonly IVocabularyService _vocabularyService;
    public VocabularyController(IVocabularyService vocabularyService) {
        _vocabularyService = vocabularyService;
    }

    [HttpGet("{term}")]
    public async Task<ActionResult<VocabularyDetailDto>> GetVocabularyDetailByTermAsync(string term) {
        return await _vocabularyService.GetVocabularyDetailByTermAsync(term);
    }
}