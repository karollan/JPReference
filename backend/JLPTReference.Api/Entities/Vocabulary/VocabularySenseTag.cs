namespace JLPTReference.Api.Entities.Vocabulary;

public class VocabularySenseTag
{
    public Guid Id {get; set;}
    public required Guid SenseId {get; set;}
    public required string TagCode {get; set;}
    public required string TagType {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}

    public Tag Tag {get; set;} = null!;
}

