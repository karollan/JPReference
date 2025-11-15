using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Services.Interfaces;

public interface ISearchService
{
    Task<GlobalSearchResponse> SearchAllAsync(GlobalSearchRequest request);
    Task<SearchResultKanji> SearchKanjiAsync(GlobalSearchRequest request);
    Task<SearchResultVocabulary> SearchVocabularyAsync(GlobalSearchRequest request);
    Task<SearchResultProperNoun> SearchProperNounAsync(GlobalSearchRequest request);
}