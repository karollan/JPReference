using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Repositories.Interfaces;

public interface ISearchRepository
{
    const double MIN_RELEVANCE_SCORE = 0.4;
    Task<GlobalSearchResponse> SearchAllAsync(GlobalSearchRequest request);
    Task<SearchResultKanji> SearchKanjiAsync(GlobalSearchRequest request);
    Task<SearchResultProperNoun> SearchProperNounAsync(GlobalSearchRequest request);
    Task<SearchResultVocabulary> SearchVocabularyAsync(GlobalSearchRequest request);
}