using System.Text.Json;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Repositories.Search.QueryBuilder;
using Microsoft.Extensions.Caching.Memory;

namespace JLPTReference.Api.Repositories.Implementations;

public class CachedProperNounSearchRepository : IProperNounSearchRepository
{
    private readonly IProperNounSearchRepository _innerService;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public CachedProperNounSearchRepository(IProperNounSearchRepository innerService, IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions { WriteIndented = false };
    }

    public async Task<SearchResultProperNoun> SearchAsync(SearchSpec spec, int pageSize, int page)
    {
        var specKey = JsonSerializer.Serialize(spec, _jsonOptions);
        var cacheKey = $"Search:ProperNoun:{specKey}:{pageSize}:{page}";

        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(2));
            return await _innerService.SearchAsync(spec, pageSize, page);
        });
        return result ?? new SearchResultProperNoun();
    }
}
