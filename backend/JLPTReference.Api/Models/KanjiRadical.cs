namespace JLPTReference.Api.Models;
public class KanjiRadical
{
    public Guid Id { get; set; }
    public Guid KanjiId { get; set; }
    public required string Radical { get; set; }
    public int StrokeCount { get; set; }
    public string? Code { get; set; }
    
    public Kanji Kanji { get; set; } = null!;
}