using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.Repositories.Search.QueryBuilder;
using JLPTReference.Api.Repositories.Search.Variants;
using JLPTReference.Api.Repositories.Search.Parser;
using System.Text.Json;

namespace JLPTReference.Api.Services.Implementations;

public class SearchService : ISearchService, ITransliterationService
{
    private readonly IQueryParser _queryParser;
    private readonly IVariantGenerator _variantGenerator;

    private readonly IVocabularySearchRepository _vocabularySearchRepository;
    private readonly IKanjiSearchRepository _kanjiSearchRepository;
    private readonly IProperNounSearchRepository _properNounSearchRepository;

    public SearchService(IQueryParser queryParser, IVariantGenerator variantGenerator, IVocabularySearchRepository vocabularySearchRepository, IKanjiSearchRepository kanjiSearchRepository, IProperNounSearchRepository properNounSearchRepository)
    {
        _queryParser = queryParser;
        _variantGenerator = variantGenerator;
        _vocabularySearchRepository = vocabularySearchRepository;
        _kanjiSearchRepository = kanjiSearchRepository;
        _properNounSearchRepository = properNounSearchRepository;
    }
    
    public async Task<GlobalSearchResponse> SearchAllAsync(GlobalSearchRequest request)
    {
        var spec = _queryParser.Parse(request.Query);
        _variantGenerator.PopulateVariants(spec);

        // Determine which types to search based on active filters
        var targets = AnalyzeFilterTargets(spec.Filters);

        // Only fire searches for applicable targets
        var kanjiTask = targets.HasFlag(FilterTarget.Kanji)
            ? SearchKanjiAsync(spec, request.PageSize, request.Page)
            : Task.FromResult(new SearchResultKanji());
        
        var vocabularyTask = targets.HasFlag(FilterTarget.Vocabulary)
            ? SearchVocabularyAsync(spec, request.PageSize, request.Page)
            : Task.FromResult(new SearchResultVocabulary());
        
        var properNounTask = targets.HasFlag(FilterTarget.ProperNoun)
            ? SearchProperNounAsync(spec, request.PageSize, request.Page)
            : Task.FromResult(new SearchResultProperNoun());

        await Task.WhenAll(kanjiTask, vocabularyTask, properNounTask);
        
        return new GlobalSearchResponse
        {
            KanjiResults = await kanjiTask,
            ProperNounResults = await properNounTask,
            VocabularyResults = await vocabularyTask,
            SearchedTerms = spec.Tokens.Aggregate(new List<string>(), (acc, t) => acc.Concat(t.Variants).ToList())
        };
    }
    
    /// <summary>
    /// Analyzes active filters to determine which data types should be searched.
    /// If no filters are active, all types are searched.
    /// Language filter is excluded from analysis as it applies to all types.
    /// </summary>
    private static FilterTarget AnalyzeFilterTargets(SearchFilters filters)
    {
        if (!HasActiveFilters(filters))
            return FilterTarget.All;
        
        FilterTarget result = FilterTarget.None;
        
        if (filters.JlptLevels != null)
            result |= FilterMapping.Applicability[nameof(SearchFilters.JlptLevels)];
        if (filters.CommonOnly != null)
            result |= FilterMapping.Applicability[nameof(SearchFilters.CommonOnly)];
        if (filters.Tags?.Count > 0)
            result |= FilterMapping.Applicability[nameof(SearchFilters.Tags)];
        if (filters.StrokeCount != null)
            result |= FilterMapping.Applicability[nameof(SearchFilters.StrokeCount)];
        if (filters.Grades != null)
            result |= FilterMapping.Applicability[nameof(SearchFilters.Grades)];
        if (filters.Frequency != null)
            result |= FilterMapping.Applicability[nameof(SearchFilters.Frequency)];
        // Note: Languages excluded - applies to all types
        
        return result == FilterTarget.None ? FilterTarget.All : result;
    }
    
    /// <summary>
    /// Checks if any non-language filters are active.
    /// </summary>
    private static bool HasActiveFilters(SearchFilters filters)
    {
        return filters.JlptLevels != null
            || filters.CommonOnly != null
            || (filters.Tags?.Count > 0)
            || filters.StrokeCount != null
            || filters.Grades != null
            || filters.Frequency != null;
    }

    public async Task<SearchResultKanji> SearchKanjiAsync(GlobalSearchRequest request)
    {
        var spec = _queryParser.Parse(request.Query);
        _variantGenerator.PopulateVariants(spec);

        var targets = AnalyzeFilterTargets(spec.Filters);

        var results = targets.HasFlag(FilterTarget.Kanji)
            ? _kanjiSearchRepository.SearchAsync(spec, request.PageSize, request.Page)
            : Task.FromResult(new SearchResultKanji());
        return await results;
    }

    public async Task<SearchResultKanji> SearchKanjiAsync(SearchSpec spec, int pageSize, int page)
    {
        var results = await _kanjiSearchRepository.SearchAsync(spec, pageSize, page);
        return results;
    }

    public async Task<SearchResultProperNoun> SearchProperNounAsync(GlobalSearchRequest request)
    {
        var spec = _queryParser.Parse(request.Query);
        _variantGenerator.PopulateVariants(spec);

        var targets = AnalyzeFilterTargets(spec.Filters);

        var results = targets.HasFlag(FilterTarget.ProperNoun)
            ? _properNounSearchRepository.SearchAsync(spec, request.PageSize, request.Page)
            : Task.FromResult(new SearchResultProperNoun());
        return await results;
    }
    public async Task<SearchResultProperNoun> SearchProperNounAsync(SearchSpec spec, int pageSize, int page)
    {
        var results = await _properNounSearchRepository.SearchAsync(spec, pageSize, page);
        return results;
    }

    public async Task<SearchResultVocabulary> SearchVocabularyAsync(GlobalSearchRequest request)
    {
        var spec = _queryParser.Parse(request.Query);
        _variantGenerator.PopulateVariants(spec);

        var targets = AnalyzeFilterTargets(spec.Filters);

        var results = targets.HasFlag(FilterTarget.Vocabulary)
            ? _vocabularySearchRepository.SearchAsync(spec, request.PageSize, request.Page)
            : Task.FromResult(new SearchResultVocabulary());
        return await results;
    }
    public async Task<SearchResultVocabulary> SearchVocabularyAsync(SearchSpec spec, int pageSize, int page)
    {
        var results = await _vocabularySearchRepository.SearchAsync(spec, pageSize, page);
        return results;
    }
}