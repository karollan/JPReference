namespace JLPTReference.Api.Entities.ProperNoun;

public class ProperNounKana
{
    public Guid Id {get; set;}
    public required Guid ProperNounId {get; set;}
    public required string Text {get; set;}
    public string[]? AppliesToKanji {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}