namespace JLPTReference.Api.Entities.ProperNoun;

public class ProperNounTranslationType
{
    public Guid Id {get; set;}
    public required Guid ProperNounTranslationId {get; set;}
    public required string TagCode {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}