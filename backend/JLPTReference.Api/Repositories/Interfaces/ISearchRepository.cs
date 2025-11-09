using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Repositories.Interfaces;

public interface ISearchRepository
{
    const double MIN_RELEVANCE_SCORE = 0.3;
    Task<GlobalSearchResponse> SearchAllAsync(GlobalSearchRequest request);
    Task<SearchResultKanji> SearchKanjiAsync(string query, int page, int pageSize, double relevanceThreshold = MIN_RELEVANCE_SCORE);
    Task<SearchResultProperNoun> SearchProperNounAsync(string query, int page, int pageSize, double relevanceThreshold = MIN_RELEVANCE_SCORE);
    Task<SearchResultVocabulary> SearchVocabularyAsync(string query, int page, int pageSize, double relevanceThreshold = MIN_RELEVANCE_SCORE);
}