using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Data;

namespace JLPTReference.Api.Services.Interfaces;

public interface ISearchQueryBuilder<TEntity> {
    IQueryable<TEntity> BuildQuery(
        IQueryable<TEntity> baseQuery,
        SearchSpec spec,
        ApplicationDBContext context
    );
}