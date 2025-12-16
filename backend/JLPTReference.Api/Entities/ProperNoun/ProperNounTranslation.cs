namespace JLPTReference.Api.Entities.ProperNoun;

public class ProperNounTranslation
{
    public Guid Id {get; set;}
    public required Guid ProperNounId {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}

    public List<ProperNounTranslationType> Types {get; set;} = new();
    public List<ProperNounTranslationText> Texts {get; set;} = new();
    public List<ProperNounTranslationRelated> RelatedTerms {get; set;} = new();
}