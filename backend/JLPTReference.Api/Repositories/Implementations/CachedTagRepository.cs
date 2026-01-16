using JLPTReference.Api.Data;
using JLPTReference.Api.Entities.Vocabulary;
using JLPTReference.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JLPTReference.Api.Repositories.Implementations;

/// <summary>
/// Cached repository for Tags - static reference data that rarely changes.
/// Uses IMemoryCache with 1-hour expiration.
/// </summary>
public class CachedTagRepository : ICachedTagRepository
{
    private readonly IDbContextFactory<ApplicationDBContext> _contextFactory;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "TagsDictionary";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);

    public CachedTagRepository(
        IDbContextFactory<ApplicationDBContext> contextFactory,
        IMemoryCache cache)
    {
        _contextFactory = contextFactory;
        _cache = cache;
    }

    public async Task<IReadOnlyDictionary<string, Tag>> GetTagsDictionaryAsync()
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, Tag>? cached) && cached != null)
        {
            return cached;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var tags = await context.Tags
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Code, t => t);

        var readOnlyDict = (IReadOnlyDictionary<string, Tag>)tags;

        _cache.Set(CacheKey, readOnlyDict, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheExpiration
        });

        return readOnlyDict;
    }
}
