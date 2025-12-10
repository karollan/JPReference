using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.Repositories.Interfaces;
using JLPTReference.Api.Services.Search.Variants;
using JLPTReference.Api.Services.Search.Parser;

namespace JLPTReference.Api.Services.Implementations;

public class SearchService : ISearchService, ITransliterationService
{
    private readonly ISearchRepository _searchRepository;
    private readonly IQueryParser _queryParser;
    private readonly IVariantGenerator _variantGenerator;

    public SearchService(ISearchRepository searchRepository, IQueryParser queryParser, IVariantGenerator variantGenerator)
    {
        _searchRepository = searchRepository;
        _queryParser = queryParser;
        _variantGenerator = variantGenerator;
    }
    
    public async Task<GlobalSearchResponse> SearchAllAsync(GlobalSearchRequest request)
    {
        request.Queries = ITransliterationService.GetAllSearchVariants(request.Query);

        var results = await _searchRepository.SearchAllAsync(request);
        return results;
    }

    public async Task<SearchResultKanji> SearchKanjiAsync(GlobalSearchRequest request)
    {
        var spec = _queryParser.Parse(request.Query);
        _variantGenerator.PopulateVariants(spec);

        var results = await _searchRepository.SearchKanjiAsync(spec, request.PageSize, request.Page);
        return results;
    }

    public async Task<SearchResultProperNoun> SearchProperNounAsync(GlobalSearchRequest request)
    {
        request.Queries = ITransliterationService.GetAllSearchVariants(request.Query);

        var results = await _searchRepository.SearchProperNounAsync(request);
        return results;
    }

    public async Task<SearchResultVocabulary> SearchVocabularyAsync(GlobalSearchRequest request)
    {
        request.Queries = ITransliterationService.GetAllSearchVariants(request.Query);

        var results = await _searchRepository.SearchVocabularyAsync(request);
        return results;
    }
}