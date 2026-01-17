namespace JLPTReference.Api.Entities.Vocabulary;

public class Tag
{
    public required string Code {get; set;}
    public required string Description {get; set;}
    public required string Category {get; set;}
    public required string[] Source {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}