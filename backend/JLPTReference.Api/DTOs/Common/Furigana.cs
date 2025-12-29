using System.Text.Json.Serialization;

namespace JLPTReference.Api.DTOs.Common;

public class FuriganaDto
{
    public required string Text { get; set; }
    public required string Reading { get; set; }
    public required List<FuriganaPartDto> Furigana { get; set; } = new();
}

public class FuriganaPartDto
{
    [JsonPropertyName("ruby")]
    public string Ruby { get; set; } = null!;
    [JsonPropertyName("rt")]
    public string? Rt { get; set; }
}