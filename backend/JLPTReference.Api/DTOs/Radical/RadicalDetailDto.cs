using JLPTReference.Api.DTOs.Kanji;

namespace JLPTReference.Api.DTOs.Radical;

public class RadicalDetailDto
{
    public Guid Id { get; set; }
    public required string Literal { get; set; }
    public required int StrokeCount { get; set; }
    public string? Code { get; set; }
    public int? KangXiNumber { get; set; }
    public List<RadicalGroupMemberDto> Variants { get; set; } = new();
    public List<string> Meanings { get; set; } = new();
    public List<string> Readings { get; set; } = new();
    public List<string> Notes { get; set; } = new();
    public DateTime UpdatedAt { get; set; }

    public List<KanjiSummaryDto> Kanji { get; set; } = new();
}

public class RadicalGroupMemberDto {
    public Guid Id { get; set; }
    public required string Literal { get; set; }

    public List<KanjiSummaryDto> Kanji { get; set; } = new();
}
