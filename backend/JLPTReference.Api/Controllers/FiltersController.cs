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

    // Supported languages (must match frontend LANGUAGE_PAIRS)
    private static readonly string[] SupportedLanguages = { "eng", "ger", "rus", "hun", "dut", "spa", "fre", "swe", "slv", "por" };

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

    /// <summary>
    /// Returns the complete filter registry in the same format as frontend FILTER_REGISTRY.
    /// This is the source of truth for filter definitions.
    /// </summary>
    [HttpGet("registry")]
    [ProducesResponseType(typeof(List<FilterRegistryEntryDto>), 200)]
    public async Task<IActionResult> GetFilterRegistry()
    {
        var registry = new List<FilterRegistryEntryDto>
        {
            // Static filters matching frontend FilterDefinition format
            new("common", "boolean") { Description = "Common words only", AppliesTo = new[] { "kanji", "vocabulary" } },
            new("jlpt", "range") { ValueType = "int", Min = 1, Max = 5, Description = "JLPT level range (1-5)", AppliesTo = new[] { "kanji", "vocabulary" } },
            new("stroke", "range") { ValueType = "int", Min = 1, Max = 24, Description = "Stroke count range (1-24)", AppliesTo = new[] { "kanji" } },
            new("grade", "range") { ValueType = "int", Min = 1, Max = 10, Description = "Grade level range (1-10)", AppliesTo = new[] { "kanji" } },
            new("freq", "range") { ValueType = "int", Min = 1, Max = 2501, Description = "Frequency range (1-2501)", AppliesTo = new[] { "kanji" } },
            new("lang", "enum") { ValueType = "string", EnumValues = SupportedLanguages, Description = "Language", AppliesTo = new[] { "kanji", "vocabulary", "properNoun" } }
        };

        // Tag-based filters from database
        var tags = await _tagRepository.GetAllTagsAsync();
        registry.AddRange(tags.Select(t => new FilterRegistryEntryDto(t.Code, "boolean")
        {
            Description = t.Description,
            AppliesTo = t.Source.Select(s => s == "proper-noun" ? "properNoun" : s).ToArray()
        }));

        return Ok(registry);
    }
}

/// <summary>
/// Response containing available filters (grouped).
/// </summary>
public class FiltersResponse
{
    public List<FilterDefinitionDto> StaticFilters { get; set; } = new();
    public List<FilterDefinitionDto> TagFilters { get; set; } = new();
}

/// <summary>
/// Definition of a filter for the frontend (simplified).
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

/// <summary>
/// Complete filter registry entry matching frontend FilterDefinition interface.
/// </summary>
public class FilterRegistryEntryDto
{
    public string Key { get; set; }
    public string Type { get; set; }  // 'boolean' | 'enum' | 'equality' | 'range' | 'multi-op'
    public string? ValueType { get; set; }  // 'int' | 'string'
    public int? Min { get; set; }
    public int? Max { get; set; }
    public string[]? EnumValues { get; set; }
    public string? Description { get; set; }
    public string[]? AppliesTo { get; set; }  // 'kanji' | 'vocabulary' | 'properNoun'

    public FilterRegistryEntryDto(string key, string type)
    {
        Key = key;
        Type = type;
    }
}
