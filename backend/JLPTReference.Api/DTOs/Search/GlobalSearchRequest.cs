namespace JLPTReference.Api.DTOs.Search;

public class GlobalSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public List<string> Types { get; set; } = new(); // ["kanji", "vocab", "properNoun"]
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}