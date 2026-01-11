using System.Text.Json;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Services.Search.QueryBuilder;
using Microsoft.Extensions.Caching.Memory;

namespace JLPTReference.Api.Services.Implementations;

public class CachedVocabularySearchService : IVocabularySearchService
{
    private readonly IVocabularySearchService _innerService;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public CachedVocabularySearchService(IVocabularySearchService innerService, IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
        // Use default options or customize if needed for consistent serialization
        _jsonOptions = new JsonSerializerOptions { WriteIndented = false };
    }

    public async Task<VocabularySearchResult> SearchAsync(SearchSpec spec, int pageSize, int page)
    {
        // specific key prefix + serialized arguments
        var specKey = JsonSerializer.Serialize(spec, _jsonOptions);
        var cacheKey = $"Search:Vocabulary:{specKey}:{pageSize}:{page}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            // Set cache options
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(2));

            // Call actual service
            return await _innerService.SearchAsync(spec, pageSize, page);
        })!;
    }
}
