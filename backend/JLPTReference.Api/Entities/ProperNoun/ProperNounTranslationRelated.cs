namespace JLPTReference.Api.Entities.ProperNoun;

public class ProperNounTranslationRelated
{
    public Guid Id {get; set;}
    public required Guid TranslationId {get; set;}
    public required string RelatedTerm {get; set;}
    public string? RelatedReading {get; set;}
    public Guid? ReferenceProperNounId {get; set;}
    public Guid? ReferenceProperNounTranslationId {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}