namespace JLPTReference.Api.Entities.ProperNoun;

public class ProperNounTranslation
{
    public Guid Id {get; set;}
    public required Guid ProperNounId {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}