namespace JLPTReference.Api.Entities.Vocabulary;

public class VocabularySenseExampleSentence
{
    public Guid Id {get; set;}
    public required Guid ExampleId {get; set;}
    public required string Lang {get; set;}
    public required string Text {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}