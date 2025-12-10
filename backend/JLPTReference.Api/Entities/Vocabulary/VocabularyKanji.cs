namespace JLPTReference.Api.Entities.Vocabulary;

public class VocabularyKanji
{
    public Guid Id {get; set;}
    public required Guid VocabularyId {get; set;}
    public required string Text {get; set;}
    public bool IsCommon {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}

    public List<VocabularyKanjiTag> Tags {get; set;} = new();
}