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
    public List<string>? PartOfSpeech { get; set; }
    public bool? CommonOnly { get; set; }
    public List<string>? Tags { get; set; }
    public IntRange? StrokeCount { get; set; } // #stroke:5 or #stroke:5-10
    public IntRange? Grades { get; set; }
    public IntRange? Frequency { get; set; }
}

public class IntRange {
    public int Min { get; set; }
    public int Max { get; set; }
}