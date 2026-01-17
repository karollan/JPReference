using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Repositories.Search.QueryBuilder;

public interface IVocabularySearchRepository
{
    Task<SearchResultVocabulary> SearchAsync(SearchSpec spec, int pageSize, int page);
}

