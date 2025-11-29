namespace JLPTReference.Api.Entities.Relations;

public class VocabularyUsesKanji
{
    public Guid Id {get; set;}
    public required Guid VocabularyId {get; set;}
    public required Guid KanjiId {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}

    public required Vocabulary.Vocabulary Vocabulary { get; set; }
    public required Kanji.Kanji Kanji { get; set; }
}