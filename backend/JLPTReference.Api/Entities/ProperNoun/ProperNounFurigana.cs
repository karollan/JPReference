using System.Text.Json.Serialization;

namespace JLPTReference.Api.Entities.ProperNoun;

public class ProperNounFurigana
{
    public Guid Id {get; set;}
    public Guid ProperNounId {get; set;}
    public required string Text {get; set;}
    public required string Reading {get; set;}
    public required List<FuriganaPart> Furigana {get; set;} = new();
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}

public class FuriganaPart
{
    [JsonPropertyName("ruby")]
    public string Ruby {get; set;} = null!;
    [JsonPropertyName("rt")]
    public string? Rt {get; set;}
}
