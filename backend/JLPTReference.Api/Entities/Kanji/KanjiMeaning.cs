namespace JLPTReference.Api.Entities.Kanji;

public class KanjiMeaning
{
    public Guid Id {get; set;}
    public required Guid KanjiId {get; set;}
    public required string Lang {get; set;}
    public required string Value {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}