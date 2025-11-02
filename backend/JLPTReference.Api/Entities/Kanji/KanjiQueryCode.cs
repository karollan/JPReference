namespace JLPTReference.Api.Entities.Kanji;

public class KanjiQueryCode
{
    public Guid Id {get; set;}
    public required Guid KanjiId {get; set;}
    public required string Type {get; set;}
    public required string Value {get; set;}
    public string? SkipMissclassification {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}