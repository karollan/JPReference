using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.Repositories.Interfaces;
using JLPTReference.Api.Services.Search.Variants;
using JLPTReference.Api.Services.Search.Parser;
using System.Text.Json;

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
        var spec = _queryParser.Parse(request.Query);
        _variantGenerator.PopulateVariants(spec);

        var kanjiResults = SearchKanjiAsync(spec, request.PageSize, request.Page);
        var properNounResults = SearchProperNounAsync(spec, request.PageSize, request.Page);
        var vocabularyResults = SearchVocabularyAsync(spec, request.PageSize, request.Page);

        await Task.WhenAll(kanjiResults, properNounResults, vocabularyResults);
        return new GlobalSearchResponse
        {
            KanjiResults = await kanjiResults,
            ProperNounResults = await properNounResults,
            VocabularyResults = await vocabularyResults,
            SearchedTerms = spec.Tokens.Aggregate(new List<string>(), (acc, t) => acc.Concat(t.Variants).ToList())
        };
    }

    public async Task<SearchResultKanji> SearchKanjiAsync(GlobalSearchRequest request)
    {
        var spec = _queryParser.Parse(request.Query);
        _variantGenerator.PopulateVariants(spec);
        var results = await _searchRepository.SearchKanjiAsync(spec, request.PageSize, request.Page);
        return results;
    }

    public async Task<SearchResultKanji> SearchKanjiAsync(SearchSpec spec, int pageSize, int page)
    {
        var results = await _searchRepository.SearchKanjiAsync(spec, pageSize, page);
        return results;
    }

    public async Task<SearchResultProperNoun> SearchProperNounAsync(GlobalSearchRequest request)
    {
        var spec = _queryParser.Parse(request.Query);
        _variantGenerator.PopulateVariants(spec);

        var results = await _searchRepository.SearchProperNounAsync(spec, request.PageSize, request.Page);
        return results;
    }
    public async Task<SearchResultProperNoun> SearchProperNounAsync(SearchSpec spec, int pageSize, int page)
    {
        var results = await _searchRepository.SearchProperNounAsync(spec, pageSize, page);
        return results;
    }

    public async Task<SearchResultVocabulary> SearchVocabularyAsync(GlobalSearchRequest request)
    {
        var spec = _queryParser.Parse(request.Query);
        _variantGenerator.PopulateVariants(spec);
        var results = await _searchRepository.SearchVocabularyAsync(spec, request.PageSize, request.Page);
        return results;
    }
    public async Task<SearchResultVocabulary> SearchVocabularyAsync(SearchSpec spec, int pageSize, int page)
    {
        var results = await _searchRepository.SearchVocabularyAsync(spec, pageSize, page);
        return results;
    }
}