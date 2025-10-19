namespace JLPTReference.Api.Models;
public class Vocabulary
{
    public Guid Id { get; set; }
    public required string JmdictId { get; set; }
    public string[]? Kanji { get; set; }
    public string[]? Kana { get; set; }
    public string[]? PartOfSpeech { get; set; }
    public string[]? Field { get; set; }
    public string[]? Dialect { get; set; }
    public string[]? Misc { get; set; }
    public string[]? Info { get; set; }
    public string[]? LanguageSource { get; set; }
    public string[]? Gloss { get; set; }
    public string[]? GlossLanguages { get; set; }
    public string[]? Related { get; set; }
    public string[]? Antonym { get; set; }
    public bool IsCommon { get; set; }
    public int? JlptOld { get; set; }
    public int? JlptNew { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<VocabularyExample> Examples { get; set; } = new List<VocabularyExample>();
}

public class VocabularyDto
{
    public required string JmdictId { get; set; }
    public string[]? Kanji { get; set; }
    public string[]? Kana { get; set; }
    public string[]? PartOfSpeech { get; set; }
    public string[]? Field { get; set; }
    public string[]? Dialect { get; set; }
    public string[]? Misc { get; set; }
    public string[]? Info { get; set; }
    public string[]? LanguageSource { get; set; }
    public string[]? Gloss { get; set; }
    public string[]? GlossLanguages { get; set; }
    public string[]? Related { get; set; }
    public string[]? Antonym { get; set; }
    public bool IsCommon { get; set; }
    public int? JlptOld { get; set; }
    public int? JlptNew { get; set; }
    public VocabularyExampleDto[]? Examples { get; set; }
}