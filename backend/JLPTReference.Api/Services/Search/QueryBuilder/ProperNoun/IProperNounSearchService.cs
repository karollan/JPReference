using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

public interface IProperNounSearchService
{
    Task<SearchResultProperNoun> SearchAsync(SearchSpec spec, int pageSize, int page);
}

