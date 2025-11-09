namespace JLPTReference.Api.DTOs.Vocabulary;

public class VocabularySummaryDto
{
    public Guid Id { get; set; }
    public required string DictionaryId { get; set; }
    public double RelevanceScore { get; set; }
    public KanjiFormDto? PrimaryKanji { get; set; }
    public KanaFormDto? PrimaryKana { get; set; }
    public List<KanjiFormDto>? OtherKanjiForms { get; set; } = new();
    public List<KanaFormDto>? OtherKanaForms { get; set; } = new();
    public List<SenseSummaryDto>? Senses { get; set; } = new();
    public int? JlptLevel { get; set; }
    public bool IsCommon { get; set; }
}

public class SenseSummaryDto
{
    public List<string>? AppliesToKanji { get; set; }
    public List<string>? AppliesToKana { get; set; }
    public List<string>? Info { get; set; }
    public List<SenseGlossDto> Glosses { get; set; } = new();
    public List<TagInfoDto> Tags { get; set; } = new();
}