namespace JLPTReference.Api.Models;

public class KanjiDecomposition
{
    public Guid Id { get; set; }
    public Guid KanjiId { get; set; }
    public required string Component { get; set; }
    
    public Kanji Kanji { get; set; } = null!;
}