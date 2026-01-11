using System.Text.Json;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Services.Search.QueryBuilder;
using Microsoft.Extensions.Caching.Memory;

namespace JLPTReference.Api.Services.Implementations;

public class CachedKanjiSearchService : IKanjiSearchService
{
    private readonly IKanjiSearchService _innerService;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public CachedKanjiSearchService(IKanjiSearchService innerService, IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions { WriteIndented = false };
    }

    public async Task<SearchResultKanji> SearchAsync(SearchSpec spec, int pageSize, int page)
    {
        var specKey = JsonSerializer.Serialize(spec, _jsonOptions);
        var cacheKey = $"Search:Kanji:{specKey}:{pageSize}:{page}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(2));
            return await _innerService.SearchAsync(spec, pageSize, page);
        })!;
    }

    public async Task<SearchResultKanji> SearchByRadicalsAsync(List<string> radicals, int pageSize, int page)
    {
        // Simple list serialization for key
        var radicalsKey = string.Join(",", radicals.OrderBy(r => r)); // order to ensure consistency
        var cacheKey = $"Search:Kanji:Radicals:{radicalsKey}:{pageSize}:{page}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(2));
            return await _innerService.SearchByRadicalsAsync(radicals, pageSize, page);
        })!;
    }
}
