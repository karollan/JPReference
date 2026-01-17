using JLPTReference.Api.Entities.Vocabulary;

namespace JLPTReference.Api.Repositories.Interfaces;

/// <summary>
/// Provides cached access to the Tags dictionary.
/// Tags are static reference data and safe to cache long-term.
/// </summary>
public interface ICachedTagRepository
{
    /// <summary>
    /// Gets the tags dictionary, loading from cache or database as needed.
    /// </summary>
    Task<IReadOnlyDictionary<string, Tag>> GetTagsDictionaryAsync();
}
