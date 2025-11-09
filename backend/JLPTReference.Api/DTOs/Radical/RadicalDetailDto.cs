namespace JLPTReference.Api.DTOs.Radical;

public class RadicalDetailDto
{
    public Guid Id { get; set; }
    public required string Literal { get; set; }
    public required int StrokeCount { get; set; }
    public required string Code { get; set; }
    public List<RadicalKanjiDto> Kanji { get; set; } = new();
}

public class RadicalKanjiDto
{
    public Guid Id { get; set; }
    public required string Literal { get; set; }
}