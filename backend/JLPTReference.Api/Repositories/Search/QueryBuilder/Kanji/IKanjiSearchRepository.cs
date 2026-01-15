using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Repositories.Search.QueryBuilder;

public interface IKanjiSearchRepository
{
    Task<SearchResultKanji> SearchAsync(SearchSpec spec, int pageSize, int page);
}

