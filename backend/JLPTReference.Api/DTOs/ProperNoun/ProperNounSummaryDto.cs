using JLPTReference.Api.DTOs.Common;

namespace JLPTReference.Api.DTOs.ProperNoun;

public class ProperNounSummaryDto
{
    public Guid Id { get; set; }
    public required string DictionaryId { get; set; }
    /// <summary>
    /// URL-friendly identifier. Format: "term" if unique, "term(reading)" if ambiguous.
    /// Example: "田中" or "中(なか)"
    /// </summary>
    public string? Slug { get; set; }
    public double RelevanceScore { get; set; }
    public KanjiFormDto? PrimaryKanji { get; set; }
    public KanaFormDto? PrimaryKana { get; set; }
    public List<KanjiFormDto>? OtherKanjiForms { get; set; } = new();
    public List<KanaFormDto>? OtherKanaForms { get; set; } = new();
    public List<TranslationSummaryDto>? Translations { get; set; } = new();
    public List<FuriganaDto>? Furigana { get; set; } = new();
}

public class TranslationSummaryDto {
    public List<TagInfoDto> Types { get; set; } = new();
    public List<TranslationTextDto> Translations { get; set; } = new();
}