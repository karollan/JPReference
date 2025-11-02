namespace JLPTReference.Api.Entities.ProperNoun;

public class ProperNounTranslationText
{
    public Guid Id {get; set;}
    public required Guid TranslationId {get; set;}
    public required string Lang {get; set;}
    public required string Text {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}