namespace JLPTReference.Api.Models;
public class Radical
{
    public Guid Id { get; set; }
    public required string Character { get; set; }
    public int StrokeCount { get; set; }
    public string? Code { get; set; }
    public string[]? Kanji { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
