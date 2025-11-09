namespace JLPTReference.Api.DTOs.Kanji;

public class KanjiSearchFilters
{
    public List<int>? JLPTLevels { get; set; }
    public int? MinStrokeCount { get; set; }
    public int? MaxStrokeCount { get; set; }
    public int? Grade { get; set; }
    public List<string>? Radicals { get; set; }
    public string? ReadingType { get; set; } // "on", "kun", "nanori"
}