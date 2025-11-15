namespace JLPTReference.Api.DTOs.Search;

public class GlobalSearchRequest
{
    public List<string> Queries { get; set; } = new();
    public List<string> Types { get; set; } = new(); // ["kanji", "vocab", "properNoun"]
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public double RelevanceThreshold { get; set; } = 0.4;
}