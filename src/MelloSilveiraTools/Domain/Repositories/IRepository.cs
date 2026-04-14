using MelloSilveiraTools.Infrastructure.Database.Models.Filters;

namespace MelloSilveiraTools.Domain.Repositories;

public interface IRepository
{
    Task<long> CountAsync<TEntity, TFilter>(TFilter filter, CancellationToken cancellationToken = default)
        where TFilter : FilterBase;

    Task<bool> ExistAsync<TEntity>(long id, CancellationToken cancellationToken = default);

    Task<bool> ExistAsync<TEntity, TFilter>(TFilter filter, CancellationToken cancellationToken = default)
        where TFilter : FilterBase;

    Task<TEntity?> GetFirstOrDefaultAsync<TEntity, TFilter>(TFilter filter, SortOrder sortOrder = SortOrder.Asc, CancellationToken cancellationToken = default)
        where TFilter : FilterBase;

    Task<TEntity?> GetAsync<TEntity>(long id, CancellationToken cancellationToken = default);

    IAsyncEnumerable<TEntity> GetAsync<TEntity, TFilter>(TFilter filter, Pagination? pagination = null, CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase;

    IAsyncEnumerable<TEntity> GetDistinctAsync<TEntity, TFilter>(TFilter filter, Pagination? pagination = null, CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase;

    Task DeleteAllAsync<TEntity>(CancellationToken cancellationToken = default);

    Task DeleteAsync<TEntity>(long id, CancellationToken cancellationToken = default);

    Task DeleteAsync<TEntity, TFilter>(TFilter filter, CancellationToken cancellationToken = default);

    Task<long> InsertAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default);

    Task<long[]> InsertAsync<TEntity>(TEntity[] entity, CancellationToken cancellationToken = default);

    Task<long[]> UpsertAsync<TEntity, TFilter>(TEntity[] entity, TFilter filter, CancellationToken cancellationToken = default);

    Task<bool> TryUpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default);
}
