using JLPTReference.Api.DTOs.Radical;

namespace JLPTReference.Api.DTOs.Kanji;

public class KanjiSummaryDto
{
    public Guid Id { get; set; }
    public double RelevanceScore { get; set; }
    public required string Literal { get; set; }
    public required int StrokeCount { get; set; }
    public int? Frequency { get; set; }
    public int? Grade { get; set; }
    public int? JlptLevel { get; set; }
    public List<KanjiReadingDto> KunyomiReadings { get; set; } = new();
    public List<KanjiReadingDto> OnyomiReadings { get; set; } = new();
    public List<KanjiMeaningDto> Meanings { get; set; } = new();
    public List<RadicalSummaryDto> Radicals { get; set; } = new();
}