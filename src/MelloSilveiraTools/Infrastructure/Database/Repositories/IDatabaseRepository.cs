using MelloSilveiraTools.Infrastructure.Database.Models.Filters;

namespace MelloSilveiraTools.Infrastructure.Database.Repositories;

/// <summary>
/// Repository that contains methods to deal with database.
/// </summary>
public interface IDatabaseRepository
{
    Task<long> CountAsync<TEntity, TFilter>(TFilter filter)
        where TFilter : FilterBase;

    Task<bool> ExistAsync<TEntity>(long id);

    Task<bool> ExistAsync<TEntity, TFilter>(TFilter filter);

    Task<TEntity?> GetFirstOrDefaultAsync<TEntity, TFilter>(TFilter filter)
        where TFilter : FilterBase;

    Task<TEntity?> GetAsync<TEntity>(long id);

    IAsyncEnumerable<TEntity> GetAsync<TEntity, TFilter>(TFilter filter, Pagination? pagination = null, CancellationToken? cancellationToken = null)
        where TEntity : class, new()
        where TFilter : FilterBase;

    IAsyncEnumerable<TEntity> GetDistinctAsync<TEntity, TFilter>(TFilter filter, Pagination? pagination = null, CancellationToken? cancellationToken = null)
        where TEntity : class, new()
        where TFilter : FilterBase;

    Task DeleteAllAsync<TEntity>();

    Task DeleteAsync<TEntity>(long id);

    Task DeleteAsync<TEntity, TFilter>(TFilter filter);

    Task<long> InsertAsync<TEntity>(TEntity entity);

    Task<long[]> InsertAsync<TEntity>(TEntity[] entity);

    Task<long[]> UpsertAsync<TEntity, TFilter>(TEntity[] entity, TFilter filter);

    Task<bool> TryUpdateAsync<TEntity>(TEntity entity);
}
