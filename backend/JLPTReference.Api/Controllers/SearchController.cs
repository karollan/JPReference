using Microsoft.AspNetCore.Mvc;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet("")]
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

    [HttpGet("kanji")]

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

    [HttpGet("vocabulary")]
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

    [HttpGet("proper-noun")]
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