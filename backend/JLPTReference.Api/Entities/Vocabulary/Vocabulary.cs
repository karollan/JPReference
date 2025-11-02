namespace JLPTReference.Api.Entities.Vocabulary;

public class Vocabulary
{
    public Guid Id {get; set;}
    public required string JmdictId {get; set;}
    public int? JlptLevelNew {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
} 