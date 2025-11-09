namespace JLPTReference.Api.DTOs.Search;

public class GlobalSearchResponse
{
    public List<SearchResultKanji> KanjiResults { get; set; } = new();
    public List<SearchResultVocabulary> VocabularyResults { get; set; } = new();
    public List<SearchResultProperNoun> ProperNounResults { get; set; } = new();
}