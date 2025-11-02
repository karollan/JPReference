namespace JLPTReference.Api.Entities.Radical;

public class Radical
{
    public Guid Id {get; set;}
    public required string Literal {get; set;}
    public required int StrokeCount {get; set;}
    public required string Code {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}