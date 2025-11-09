using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Services.Interfaces;

public interface ISearchService
{
    Task<GlobalSearchResponse> SearchAllAsync(GlobalSearchRequest request);
    Task<SearchResultKanji> SearchKanjiAsync(string query, int page, int pageSize);
    Task<SearchResultVocabulary> SearchVocabularyAsync(string query, int page, int pageSize);
    Task<SearchResultProperNoun> SearchProperNounAsync(string query, int page, int pageSize);
}