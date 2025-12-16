namespace JLPTReference.Api.Entities.ProperNoun;

public class ProperNounKanji
{
    public Guid Id {get; set;}
    public required Guid ProperNounId {get; set;}
    public required string Text {get; set;}
    public bool IsPrimary {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}

    public List<ProperNounKanjiTag> Tags {get; set;} = new();
}