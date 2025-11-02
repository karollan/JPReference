namespace JLPTReference.Api.Entities.Kanji;

public class KanjiReading
{
    public Guid Id {get; set;}
    public required Guid KanjiId {get; set;}
    public required string Type {get; set;}
    public required string Value {get; set;}
    public string? Status {get; set;}
    public string? OnType {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}