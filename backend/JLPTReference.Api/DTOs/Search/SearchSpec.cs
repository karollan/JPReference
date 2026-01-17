namespace JLPTReference.Api.DTOs.Search;
public class SearchSpec {
    public List<SearchToken> Tokens { get; set; } = new();
    public SearchFilters Filters { get; set; } = new();
}

public class SearchToken {
    public required string RawValue { get; set; }
    public List<string> Variants { get; set; } = new();
    public bool HasWildcard { get; set; }
    public bool TransliterationBlocked { get; set; }
}

public class SearchFilters {
    public IntRange? JlptLevels { get; set; }
    public bool? CommonOnly { get; set; }
    public List<string>? Tags { get; set; }
    public IntRange? StrokeCount { get; set; } // #stroke:5 or #stroke:5-10
    public IntRange? Grades { get; set; }
    public IntRange? Frequency { get; set; }
    public List<string>? Languages { get; set; }
}

public class IntRange {
    public int Min { get; set; }
    public int Max { get; set; }
}

/// <summary>
/// Flags indicating which data types a filter applies to.
/// </summary>
[Flags]
public enum FilterTarget
{
    None = 0,
    Kanji = 1,
    Vocabulary = 2,
    ProperNoun = 4,
    All = Kanji | Vocabulary | ProperNoun
}

/// <summary>
/// Maps filter properties to which data types they apply to.
/// This is used by SearchService to determine which search methods to invoke.
/// </summary>
public static class FilterMapping
{
    /// <summary>
    /// Maps each filter property name to the data types it can filter.
    /// </summary>
    public static readonly Dictionary<string, FilterTarget> Applicability = new()
    {
        { nameof(SearchFilters.JlptLevels), FilterTarget.Kanji | FilterTarget.Vocabulary },
        { nameof(SearchFilters.CommonOnly), FilterTarget.Kanji | FilterTarget.Vocabulary },
        { nameof(SearchFilters.Tags), FilterTarget.Vocabulary | FilterTarget.ProperNoun },
        { nameof(SearchFilters.StrokeCount), FilterTarget.Kanji },
        { nameof(SearchFilters.Grades), FilterTarget.Kanji },
        { nameof(SearchFilters.Frequency), FilterTarget.Kanji },
        { nameof(SearchFilters.Languages), FilterTarget.All }
    };
    
    /// <summary>
    /// Filters that should be excluded from "should search this type" logic.
    /// Language is always applied and shouldn't cause other types to be skipped.
    /// </summary>
    public static readonly HashSet<string> ExcludedFromTargetAnalysis = new()
    {
        nameof(SearchFilters.Languages)
    };
}