namespace JLPTReference.Api.Entities.Vocabulary;

public class VocabularyKanaTag
{
    public Guid Id {get; set;}
    public required Guid VocabularyKanaId {get; set;}
    public required string TagCode {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}