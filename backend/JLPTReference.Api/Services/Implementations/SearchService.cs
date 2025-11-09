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

    public async Task<SearchResultKanji> SearchKanjiAsync(string query, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 100) pageSize = 100;
        var results = await _searchRepository.SearchKanjiAsync(query, page, pageSize);
        return results;
    }

    public async Task<SearchResultProperNoun> SearchProperNounAsync(string query, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 100) pageSize = 100;
        var results = await _searchRepository.SearchProperNounAsync(query, page, pageSize);
        return results;
    }

    public async Task<SearchResultVocabulary> SearchVocabularyAsync(string query, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 100) pageSize = 100;
        var results = await _searchRepository.SearchVocabularyAsync(query, page, pageSize);
        return results;
    }
}