using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.Repositories.Interfaces;

namespace JLPTReference.Api.Services.Implementations;

public class SearchService : ISearchService
{
    private readonly ISearchRepository _searchRepository;

    public SearchService(ISearchRepository searchRepository)
    {
        _searchRepository = searchRepository;
    }
    
    public async Task<GlobalSearchResponse> SearchAllAsync(GlobalSearchRequest request)
    {
        var results = await _searchRepository.SearchAllAsync(request);
        return results;
    }

    public async Task<SearchResultKanji> SearchKanjiAsync(GlobalSearchRequest request)
    {
        if (request.Page < 1) request.Page = 1;
        if (request.PageSize < 1) request.PageSize = 50;
        if (request.PageSize > 100) request.PageSize = 100;
        var results = await _searchRepository.SearchKanjiAsync(request);
        return results;
    }

    public async Task<SearchResultProperNoun> SearchProperNounAsync(GlobalSearchRequest request)
    {
        if (request.Page < 1) request.Page = 1;
        if (request.PageSize < 1) request.PageSize = 50;
        if (request.PageSize > 100) request.PageSize = 100;
        var results = await _searchRepository.SearchProperNounAsync(request);
        return results;
    }

    public async Task<SearchResultVocabulary> SearchVocabularyAsync(GlobalSearchRequest request)
    {
        if (request.Page < 1) request.Page = 1;
        if (request.PageSize < 1) request.PageSize = 50;
        if (request.PageSize > 100) request.PageSize = 100;
        var results = await _searchRepository.SearchVocabularyAsync(request);
        return results;
    }
}