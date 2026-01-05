namespace JLPTReference.Api.DTOs.Radical;

public class RadicalSummaryDto
{
    public Guid Id { get; set; }
    public required string Literal { get; set; }
    public int? StrokeCount { get; set; }
}