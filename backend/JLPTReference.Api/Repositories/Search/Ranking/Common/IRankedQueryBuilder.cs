using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Repositories.Search.QueryBuilder;

namespace JLPTReference.Api.Repositories.Search.Ranking;

public interface IRankedQueryBuilder<TEntity, TProfile> : ISearchQueryBuilder<TEntity>
    where TProfile : class
{
    IOrderedQueryable<TEntity> BuildRankedQuery(
        IQueryable<TEntity> query, 
        SearchSpec spec,
        TProfile profile);
}

