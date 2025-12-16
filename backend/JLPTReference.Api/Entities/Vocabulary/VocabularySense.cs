namespace JLPTReference.Api.Entities.Vocabulary;

public class VocabularySense
{
    public Guid Id {get; set;}
    public required Guid VocabularyId {get; set;}
    public string[]? AppliesToKanji {get; set;}
    public string[]? AppliesToKana {get; set;}
    public string[]? Info {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}

    public List<VocabularySenseTag> Tags {get; set;} = new();
    public List<VocabularySenseRelation> Relations {get; set;} = new();
    public List<VocabularySenseLanguageSource> LanguageSources {get; set;} = new();
    public List<VocabularySenseGloss> Glosses {get; set;} = new();
    public List<VocabularySenseExample> Examples {get; set;} = new();
}