namespace JLPTReference.Api.DTOs.Radical;

public class RadicalSummaryDto
{
    public Guid Id { get; set; }
    public required string Literal { get; set; }
    public int? StrokeCount { get; set; }
    public bool HasDetails { get; set; } // whether we can click it to get details
}