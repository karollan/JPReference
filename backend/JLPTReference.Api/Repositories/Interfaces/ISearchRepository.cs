using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Repositories.Interfaces;

public interface ISearchRepository
{
    const double MIN_RELEVANCE_SCORE = 0.4;
    Task<GlobalSearchResponse> SearchAllAsync(GlobalSearchRequest request);
    Task<SearchResultKanji> SearchKanjiAsync(GlobalSearchRequest request);
    Task<SearchResultKanji> SearchKanjiAsync(SearchSpec spec, int pageSize, int page);
    Task<SearchResultProperNoun> SearchProperNounAsync(GlobalSearchRequest request);
    Task<SearchResultProperNoun> SearchProperNounAsync(SearchSpec spec, int pageSize, int page);
    Task<SearchResultVocabulary> SearchVocabularyAsync(GlobalSearchRequest request);
    Task<SearchResultVocabulary> SearchVocabularyAsync(SearchSpec spec, int pageSize, int page);
}