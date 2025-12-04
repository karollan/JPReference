namespace JLPTReference.Api.Entities.ProperNoun;

public class ProperNounTranslationType
{
    public Guid Id {get; set;}
    public required Guid TranslationId {get; set;}
    public required string TagCode {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}