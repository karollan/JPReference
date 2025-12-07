namespace JLPTReference.Api.DTOs.Search;

public class GlobalSearchResponse
{
    public List<string> SearchedTerms { get; set; } = new();
    public SearchResultKanji KanjiResults { get; set; } = new();
    public SearchResultVocabulary VocabularyResults { get; set; } = new();
    public SearchResultProperNoun ProperNounResults { get; set; } = new();
}