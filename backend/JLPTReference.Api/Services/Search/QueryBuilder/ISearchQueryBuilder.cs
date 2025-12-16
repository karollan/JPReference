using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Services.Search.QueryBuilder;

public interface ISearchQueryBuilder<TEntity>
{
    IQueryable<TEntity> BuildQuery(
        IQueryable<TEntity> query,
        SearchSpec spec
    );
}