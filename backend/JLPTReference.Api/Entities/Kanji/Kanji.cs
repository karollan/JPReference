using JLPTReference.Api.Entities.Relations;
namespace JLPTReference.Api.Entities.Kanji;

public class Kanji
{
    public Guid Id {get; set;}
    public required string Literal {get; set;}
    public int? Grade {get; set;}
    public required int StrokeCount {get; set;}
    public int? Frequency {get; set;}
    public int? JlptLevelOld {get; set;}
    public int? JlptLevelNew {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}

    public List<KanjiMeaning> Meanings { get; set; } = new();
    public List<KanjiReading> Readings { get; set; } = new();
    public List<KanjiCodepoint> Codepoints { get; set; } = new();
    public List<KanjiDictionaryReference> DictionaryReferences { get; set; } = new();
    public List<KanjiQueryCode> QueryCodes { get; set; } = new();
    public List<KanjiNanori> Nanori { get; set; } = new();
    public List<KanjiRadical> Radicals { get; set; } = new();
    public List<VocabularyUsesKanji> VocabularyReferences { get; set; } = new();
}