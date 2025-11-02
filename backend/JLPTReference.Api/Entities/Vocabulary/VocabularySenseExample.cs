namespace JLPTReference.Api.Entities.Vocabulary;

public class VocabularySenseExample
{
    public Guid Id {get; set;}
    public required Guid SenseId {get; set;}
    public string? SourceType {get; set;}
    public string? SourceValue {get; set;}
    public required string Text {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}