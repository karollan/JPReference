namespace JLPTReference.Api.Entities.ProperNoun;

public class ProperNounKanjiTag
{
    public Guid Id {get; set;}
    public required Guid ProperNounKanjiId {get; set;}
    public required string TagCode {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}