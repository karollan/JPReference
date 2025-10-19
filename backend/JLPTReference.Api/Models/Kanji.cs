namespace JLPTReference.Api.Models;

public class Kanji
{
    public Guid Id { get; set; }
    public required string Character { get; set; }
    public required string[] Meanings { get; set; }
    public string[]? ReadingsOn { get; set; }
    public string[]? ReadingsKun { get; set; }
    public int? StrokeCount { get; set; }
    public int? Grade { get; set; }
    public int? Frequency { get; set; }
    public int? JlptOld { get; set; }
    public int? JlptNew { get; set; }
    public string[]? Codepoints { get; set; }
    public string[]? Radicals { get; set; }
    public string[]? Variants { get; set; }
    public string[]? RadicalNames { get; set; }
    public string[]? DictionaryReferences { get; set; }
    public string[]? QueryCodes { get; set; }
    public string[]? Nanori { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<KanjiRadical> KanjiRadicals { get; set; } = new List<KanjiRadical>();
    public ICollection<KanjiDecomposition> KanjiDecompositions { get; set; } = new List<KanjiDecomposition>();
}
public class KanjiDto
{
    public required string Character { get; set; }
    public required string[] Meanings { get; set; }
    public string[]? ReadingsOn { get; set; }
    public string[]? ReadingsKun { get; set; }
    public int? StrokeCount { get; set; }
    public int? Grade { get; set; }
    public int? Frequency { get; set; }
    public int? JlptOld { get; set; }
    public int? JlptNew { get; set; }
    public string[]? Codepoints { get; set; }
    public string[]? Radicals { get; set; }
    public string[]? Variants { get; set; }
    public string[]? RadicalNames { get; set; }
    public string[]? DictionaryReferences { get; set; }
    public string[]? QueryCodes { get; set; }
    public string[]? Nanori { get; set; }
}

