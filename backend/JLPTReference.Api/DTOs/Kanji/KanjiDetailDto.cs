using JLPTReference.Api.DTOs.Radical;
namespace JLPTReference.Api.DTOs.Kanji;

public class KanjiDetailDto
{
    public Guid Id { get; set; }
    public required string Literal { get; set; }
    public List<KanjiMeaningDto> Meanings { get; set; } = new();
    public List<KanjiReadingDto> Readings { get; set; } = new();
    public List<KanjiCodepointDto> Codepoints { get; set; } = new();
    public List<KanjiDictionaryReferenceDto> DictionaryReferences { get; set; } = new();
    public List<KanjiQueryCodeDto> QueryCodes { get; set; } = new();
    public List<KanjiNanoriDto> Nanori { get; set; } = new();
    public required int StrokeCount { get; set;}
    public int? Frequency { get; set; }
    public int? Grade { get; set; }
    public int? JlptLevel { get; set; }
    public List<RadicalSummaryDto> Radicals { get; set; } = new();
    public List<KanjiVocabularyDto> VocabularyReferences { get; set; } = new();
}

public class KanjiVocabularyDto {
    public Guid Id { get; set; }
    public required string Term { get; set; }
}

public class KanjiMeaningDto {
    public required string Language { get; set; }
    public required string Meaning { get; set; }
}

public class KanjiReadingDto {
    public required string Type { get; set; }
    public required string Value { get; set; }
    public string? Status { get; set; }
    public string? OnType { get; set; }
}

public class KanjiCodepointDto {
    public required string Type { get; set; }
    public required string Value { get; set; }
}

public class KanjiDictionaryReferenceDto {
    public required string Type { get; set; }
    public required string Value { get; set; }
    public int? MorohashiVolume { get; set; }
    public int? MorohashiPage { get; set; }
}

public class KanjiQueryCodeDto {
    public required string Type { get; set; }
    public required string Value { get; set; }
    public string? SkipMisclassification { get; set; }
}

public class KanjiNanoriDto {
    public required string Value { get; set; }
}