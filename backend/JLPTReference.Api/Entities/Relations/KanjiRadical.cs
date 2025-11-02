namespace JLPTReference.Api.Entities.Relations;

public class KanjiRadical
{
    public Guid Id {get; set;}
    public required Guid KanjiId {get; set;}
    public required Guid RadicalId {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}