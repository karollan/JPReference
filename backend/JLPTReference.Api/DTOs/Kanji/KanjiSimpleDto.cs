namespace JLPTReference.Api.DTOs.Kanji;

public class KanjiSimpleDto
{
    public Guid Id { get; set; }
    public required string Literal { get; set; }
    public int StrokeCount { get; set; }
}
