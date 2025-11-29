namespace JLPTReference.Api.Entities.Vocabulary;

public class Vocabulary
{
    public Guid Id {get; set;}
    public required string JmdictId {get; set;}
    public int? JlptLevelNew {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
    
    public List<VocabularyKana> Kana { get; set; } = new();
    public List<VocabularyKanji> Kanji { get; set; } = new();
    public List<VocabularySense> Senses { get; set; } = new();
    public List<VocabularySenseRelation> SenseRelations { get; set; } = new();
    public List<VocabularySenseLanguageSource> SenseLanguageSources { get; set; } = new();
    public List<VocabularySenseGloss> SenseGlosses { get; set; } = new();
    public List<VocabularySenseExample> SenseExamples { get; set; } = new();
    public List<VocabularySenseExampleSentence> SenseExampleSentences { get; set; } = new();
} 