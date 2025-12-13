using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.DTOs.Vocabulary;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

/// <summary>
/// Abstraction for vocabulary search that can be implemented by either
/// EF Core or raw SQL backends.
/// </summary>
public interface IVocabularySearchService
{
    /// <summary>
    /// Searches vocabulary with ranking and pagination.
    /// </summary>
    Task<VocabularySearchResult> SearchAsync(SearchSpec spec, int pageSize, int page);
}

/// <summary>
/// Result from vocabulary search including data and pagination.
/// </summary>
public class VocabularySearchResult
{
    public List<VocabularySummaryDto> Data { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}

