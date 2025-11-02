namespace JLPTReference.Api.Entities.Kanji;

public class Kanji
{
    public Guid Id {get; set;}
    public required string Literal {get; set;}
    public int? Grade {get; set;}
    public required int StrokeCount {get; set;}
    public int? Frequency {get; set;}
    public int? JlptLevelOld {get; set;}
    public int? JlptLevelNew {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}