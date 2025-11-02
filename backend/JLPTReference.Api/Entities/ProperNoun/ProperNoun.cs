namespace JLPTReference.Api.Entities.ProperNoun;

public class ProperNoun
{
    public Guid Id {get; set;}
    public required string JmnedictId {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}