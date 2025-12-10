namespace JLPTReference.Api.Entities.Vocabulary;

public class VocabularyKana
{
    public Guid Id {get; set;}
    public required Guid VocabularyId {get; set;}
    public required string Text {get; set;}
    public string[]? AppliesToKanji {get; set;}
    public bool IsCommon {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}

    public List<VocabularyKanaTag> Tags {get; set;} = new();
}