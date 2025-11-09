namespace JLPTReference.Api.DTOs.Vocabulary;

public class VocabularySearchFilters
{
    public List<int>? JLPTLevels { get; set; }
    public List<string>? PartOfSpeech { get; set; }
    public List<string>? Fields { get; set; }
    public List<string>? Dialects { get; set; }
    public bool? IsCommon { get; set; }
}