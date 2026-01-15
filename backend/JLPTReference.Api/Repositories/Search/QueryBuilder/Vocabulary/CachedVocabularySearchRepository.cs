using System.Text.Json;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Repositories.Search.QueryBuilder;
using Microsoft.Extensions.Caching.Memory;

namespace JLPTReference.Api.Repositories.Implementations;

public class CachedVocabularySearchRepository : IVocabularySearchRepository
{
    private readonly IVocabularySearchRepository _innerService;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public CachedVocabularySearchRepository(IVocabularySearchRepository innerService, IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
        // Use default options or customize if needed for consistent serialization
        _jsonOptions = new JsonSerializerOptions { WriteIndented = false };
    }

    public async Task<SearchResultVocabulary> SearchAsync(SearchSpec spec, int pageSize, int page)
    {
        // specific key prefix + serialized arguments
        var specKey = JsonSerializer.Serialize(spec, _jsonOptions);
        var cacheKey = $"Search:Vocabulary:{specKey}:{pageSize}:{page}";

        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            // Set cache options
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(2));

            // Call actual service
            return await _innerService.SearchAsync(spec, pageSize, page);
        });
        return result ?? new SearchResultVocabulary();
    }
}
