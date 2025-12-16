using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

public interface IKanjiSearchService
{
    Task<SearchResultKanji> SearchAsync(SearchSpec spec, int pageSize, int page);
}

