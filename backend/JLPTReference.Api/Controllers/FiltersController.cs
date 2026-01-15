using Microsoft.AspNetCore.Mvc;
using JLPTReference.Api.Repositories.Interfaces;
using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Controllers;

/// <summary>
/// Provides available filters for search functionality.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FiltersController : ControllerBase
{
    private readonly ITagRepository _tagRepository;

    public FiltersController(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    /// <summary>
    /// Returns all available filters including static filters and tag-based filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(FiltersResponse), 200)]
    public async Task<IActionResult> GetAvailableFilters()
    {
        // Static filters - these are built into the application
        var staticFilters = new List<FilterDefinitionDto>
        {
            new("jlpt", "range", "JLPT level (1-5)", new[] { "kanji", "vocabulary" }, 1, 5),
            new("stroke", "range", "Stroke count", new[] { "kanji" }, 1, 24),
            new("grade", "range", "Grade level (1-12)", new[] { "kanji" }, 1, 10),
            new("freq", "range", "Frequency ranking", new[] { "kanji" }, 1, 2501),
            new("common", "boolean", "Common words only", new[] { "kanji", "vocabulary" }),
            new("lang", "enum", "Language filter", new[] { "kanji", "vocabulary", "properNoun" })
        };
        
        // Tag-based filters from database
        var tags = await _tagRepository.GetAllTagsAsync();
        var tagFilters = tags.Select(t => new FilterDefinitionDto(
            t.Code, 
            "boolean", 
            t.Description, 
            t.Source.Select(s => s == "proper-noun" ? "properNoun" : s).ToArray()
        )).ToList();
        
        return Ok(new FiltersResponse
        {
            StaticFilters = staticFilters,
            TagFilters = tagFilters
        });
    }
}

/// <summary>
/// Response containing available filters.
/// </summary>
public class FiltersResponse
{
    public List<FilterDefinitionDto> StaticFilters { get; set; } = new();
    public List<FilterDefinitionDto> TagFilters { get; set; } = new();
}

/// <summary>
/// Definition of a filter for the frontend.
/// </summary>
public class FilterDefinitionDto
{
    public string Key { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public string[] AppliesTo { get; set; }
    public int? Min { get; set; }
    public int? Max { get; set; }

    public FilterDefinitionDto(string key, string type, string description, string[] appliesTo, int? min = null, int? max = null)
    {
        Key = key;
        Type = type;
        Description = description;
        AppliesTo = appliesTo;
        Min = min;
        Max = max;
    }
}
