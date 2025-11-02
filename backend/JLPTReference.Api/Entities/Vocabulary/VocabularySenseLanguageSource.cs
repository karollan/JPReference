namespace JLPTReference.Api.Entities.Vocabulary;

public class VocabularySenseLanguageSource
{
    public Guid Id {get; set;}
    public required Guid SenseId {get; set;}
    public string? Lang {get; set;}
    public string? Text {get; set;}
    public bool? Full {get; set;}
    public bool? Wasei {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}