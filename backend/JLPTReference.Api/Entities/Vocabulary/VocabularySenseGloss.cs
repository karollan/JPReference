namespace JLPTReference.Api.Entities.Vocabulary;

public class VocabularySenseGloss
{
    public Guid Id {get; set;}
    public required Guid SenseId {get; set;}
    public required string Lang {get; set;}
    public required string Text {get; set;}
    public string? Gender {get; set;}
    public string? Type {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}