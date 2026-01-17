using JLPTReference.Api.Entities.Relations;
namespace JLPTReference.Api.Entities.Vocabulary;

public class Vocabulary
{
    public Guid Id {get; set;}
    public required string JmdictId {get; set;}
    public int? JlptLevelNew {get; set;}
    public string? Slug { get; set; }
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
    
    public List<VocabularyKana> Kana { get; set; } = new();
    public List<VocabularyKanji> Kanji { get; set; } = new();
    public List<VocabularySense> Senses { get; set; } = new();
    public List<VocabularyUsesKanji> KanjiReferences { get; set; } = new();
    public List<VocabularyFurigana> Furigana { get; set; } = new();
}