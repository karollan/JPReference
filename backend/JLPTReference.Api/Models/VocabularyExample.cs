namespace JLPTReference.Api.Models;
public class VocabularyExample
{
    public Guid Id { get; set; }
    public Guid VocabularyId { get; set; }
    public required string Source { get; set; }
    public required string Text { get; set; }
    public string[]? JapaneseSentences { get; set; }
    public string[]? EnglishSentences { get; set; }
    
    public Vocabulary Vocabulary { get; set; } = null!;
}
public class VocabularyExampleDto
{
    public required string Source { get; set; }
    public required string Text { get; set; }
    public string[]? JapaneseSentences { get; set; }
    public string[]? EnglishSentences { get; set; }
}