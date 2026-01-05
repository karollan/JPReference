using JLPTReference.Api.DTOs.Kanji;

namespace JLPTReference.Api.DTOs.Radical;

public class RadicalSearchResultDto
{
    public List<KanjiSimpleDto> Results { get; set; } = new();
    public List<Guid> CompatibleRadicalIds { get; set; } = new();
}
