namespace JLPTReference.Api.Entities.Vocabulary;

public class VocabularyKanjiTag
{
    public Guid Id {get; set;}
    public required Guid VocabularyKanjiId {get; set;}
    public required string TagCode {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}

    public Tag Tag {get; set;} = null!;
}