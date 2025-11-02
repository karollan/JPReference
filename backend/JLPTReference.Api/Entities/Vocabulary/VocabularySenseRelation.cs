namespace JLPTReference.Api.Entities.Vocabulary;

public class VocabularySenseRelation
{
    public Guid Id {get; set;}
    public required Guid SourceSenseId {get; set;}
    public Guid? TargetVocabularyId {get; set;}
    public Guid? TargetSenseId {get; set;}
    public required string TargetTerm {get; set;}
    public string? TargetReading {get; set;}
    public required string RelationType {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}