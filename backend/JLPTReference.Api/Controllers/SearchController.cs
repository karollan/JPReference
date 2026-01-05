using Microsoft.AspNetCore.Mvc;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    /// <summary>
    /// Performs a global search across Kanji, Vocabulary, and Proper Nouns.
    /// </summary>
    /// <param name="query">The search term.</param>
    /// <param name="types">List of types to include: "kanji", "vocab", "properNoun". Defaults to all.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Results per page.</param>
    /// <returns>Combined search results.</returns>
    [HttpGet("")]
    [ProducesResponseType(typeof(GlobalSearchResponse), 200)]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] List<string> types,
        [FromQuery] int page,
        [FromQuery] int pageSize
    ) {
        if (string.IsNullOrEmpty(query)) {
            return BadRequest("Query is required");
        }
        // Default types to all types if not provided
        if (types == null || types.Count == 0) {
            types = ["kanji", "vocab", "properNoun"];
        }
        if (page <= 0) {
            return BadRequest("Page must be greater than 0");
        }
        if (pageSize <= 0) {
            return BadRequest("PageSize must be greater than 0");
        }

        GlobalSearchRequest request = new GlobalSearchRequest {
            Query = query,
            Types = types,
            Page = page,
            PageSize = pageSize
        };
        
        var result = await _searchService.SearchAllAsync(request);

        return Ok(result);
    }

    /// <summary>
    /// Searches only for Kanji characters.
    /// </summary>
    /// <param name="query">The search term.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Results per page.</param>
    /// <returns>Paginated Kanji results.</returns>
    [HttpGet("kanji")]
    [ProducesResponseType(typeof(SearchResultKanji), 200)]
    public async Task<IActionResult> SearchKanji(
        [FromQuery] string query,
        [FromQuery] int page,
        [FromQuery] int pageSize
    ) {
        if (string.IsNullOrEmpty(query)) {
            return BadRequest("Term is required");
        }
        if (page <= 0) {
            page = 1;
        }
        if (pageSize <= 0) {
            return BadRequest("PageSize must be greater than 0");
        }
        if (pageSize > 100) {
            pageSize = 100;
        }

        GlobalSearchRequest request = new GlobalSearchRequest {
            Query = query,
            Page = page,
            PageSize = pageSize
        };
        var result = await _searchService.SearchKanjiAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Searches only for Vocabulary terms.
    /// </summary>
    /// <param name="query">The search term.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Results per page.</param>
    /// <returns>Paginated Vocabulary results.</returns>
    [HttpGet("vocabulary")]
    [ProducesResponseType(typeof(SearchResultVocabulary), 200)]
    public async Task<IActionResult> SearchVocabulary(
        [FromQuery] string query,
        [FromQuery] int page,
        [FromQuery] int pageSize
    ) {
        if (string.IsNullOrEmpty(query)) {
            return BadRequest("Term is required");
        }
        if (page <= 0) {
            page = 1;
        }
        if (pageSize <= 0) {
            return BadRequest("PageSize must be greater than 0");
        }
        if (pageSize > 100) {
            pageSize = 100;
        }
        GlobalSearchRequest request = new GlobalSearchRequest {
            Query = query,
            Page = page,
            PageSize = pageSize
        };
        var result = await _searchService.SearchVocabularyAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Searches only for Proper Nouns.
    /// </summary>
    /// <param name="query">The search term.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Results per page.</param>
    /// <returns>Paginated Proper Noun results.</returns>
    [HttpGet("proper-noun")]
    [ProducesResponseType(typeof(SearchResultProperNoun), 200)]
    public async Task<IActionResult> SearchProperNoun(
        [FromQuery] string query,
        [FromQuery] int page,
        [FromQuery] int pageSize
    ) {
        if (string.IsNullOrEmpty(query)) {
            return BadRequest("Term is required");
        }
        if (page <= 0) {
            page = 1;
        }
        if (pageSize <= 0) {
            return BadRequest("PageSize must be greater than 0");
        }
        if (pageSize > 100) {
            pageSize = 100;
        }
        GlobalSearchRequest request = new GlobalSearchRequest {
            Query = query,
            Page = page,
            PageSize = pageSize
        };
        var result = await _searchService.SearchProperNounAsync(request);
        return Ok(result);
    }
}