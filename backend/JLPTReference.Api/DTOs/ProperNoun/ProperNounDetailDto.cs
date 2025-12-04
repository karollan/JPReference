using JLPTReference.Api.DTOs.Vocabulary;

namespace JLPTReference.Api.DTOs.ProperNoun;

public class ProperNounDetailDto
{
    public Guid Id { get; set; }
    public required string JmnedictId { get; set; }
    public List<KanjiFormDto> KanjiForms { get; set; } = new();
    public List<KanaFormDto> KanaForms { get; set; } = new();
    public List<TranslationDto> Translations { get; set; } = new();
    public List<KanjiInfoDto> ContainedKanji { get; set; } = new();
}

public class KanjiInfoDto
{
    public Guid Id { get; set; }
    public required string Literal { get; set; }
}

public class KanjiFormDto {
    public required string Text { get; set; }
    public List<TagInfoDto> Tags { get; set; } = new();
}

public class KanaFormDto {
    public required string Text { get; set; }
    public List<string> AppliesToKanji { get; set; } = new();
    public List<TagInfoDto> Tags { get; set; } = new();
}

public class TranslationDto {
    public List<TagInfoDto> Types { get; set; } = new();
    public List<TranslationRelatedDto> Related { get; set; } = new();
    public List<TranslationTextDto> Text { get; set; } = new();
}

public class TranslationRelatedDto
{
    public required string Term { get; set; }
    public string? Reading { get; set; }
    public Guid? ReferenceProperNounId { get; set; }
    public Guid? ReferenceProperNounTranslationId { get; set; }
}

public class TranslationTextDto
{
    public required string Language { get; set; }
    public required string Text { get; set; }
}