using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Repositories.Search.QueryBuilder;

public interface IProperNounSearchRepository
{
    Task<SearchResultProperNoun> SearchAsync(SearchSpec spec, int pageSize, int page);
}

