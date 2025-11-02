namespace JLPTReference.Api.Entities.Relations;

public class ProperNounUsesKanji
{
    public Guid Id {get; set;}
    public required Guid ProperNounId {get; set;}
    public required Guid KanjiId {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}