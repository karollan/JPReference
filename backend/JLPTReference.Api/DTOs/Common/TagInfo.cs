namespace JLPTReference.Api.DTOs.Common;

public class TagInfoDto
{
    public required string Code { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }
    public string? Type { get; set; } // For sense tags: 'pos', 'field', 'dialect', 'misc'
}