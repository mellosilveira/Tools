using MelloSilveiraTools.Database.Infrastructure.Database.Models.Filters;

namespace MelloSilveiraTools.Database.Domain.Repositories;

/// <summary>
/// Provides a generic, filter-driven abstraction over persistent storage for entity types.
/// </summary>
/// <remarks>
/// All asynchronous members accept a <see cref="CancellationToken"/> and will throw
/// <see cref="OperationCanceledException"/> when cancellation is signalled.
///
/// Transient database failures (connection drops, broken protocol, network blips, command timeouts caused by
/// transient conditions) are absorbed and retried resilience pipeline. Once the pipeline budget is exhausted 
/// the underlying exceptions are re-thrown to the caller. Non-transient failures — for example unique-constraint 
/// violations, permission errors, or definitive command timeouts — are not retried and are propagated as <see cref="TimeoutException"/>.
/// </remarks>
public interface IRepository
{
    /// <summary>
    /// Counts the entities matching the supplied filter.
    /// </summary>
    Task<long> CountAsync<TEntity, TFilter>(TFilter filter, CancellationToken cancellationToken = default)
        where TFilter : FilterBase;

    /// <summary>
    /// Indicates whether an entity with the given identifier exists.
    /// </summary>
    Task<bool> ExistAsync<TEntity>(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether any entity matching the supplied filter exists.
    /// </summary>
    Task<bool> ExistAsync<TEntity, TFilter>(TFilter filter, CancellationToken cancellationToken = default)
        where TFilter : FilterBase;

    /// <summary>
    /// Gets the first entity matching the supplied filter, or the default value when none is found.
    /// </summary>
    Task<TEntity?> GetFirstOrDefaultAsync<TEntity, TFilter>(TFilter filter, SortOrder sortOrder = SortOrder.Asc, CancellationToken cancellationToken = default)
        where TFilter : FilterBase;

    /// <summary>
    /// Gets the entity with the given identifier, or <c>null</c> when it does not exist.
    /// </summary>
    Task<TEntity?> GetAsync<TEntity>(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams all entities matching the supplied filter, optionally paginated.
    /// </summary>
    IAsyncEnumerable<TEntity> GetAsync<TEntity, TFilter>(TFilter filter, Pagination? pagination = null, CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase;

    /// <summary>
    /// Streams the distinct entities matching the supplied filter, optionally paginated.
    /// </summary>
    IAsyncEnumerable<TEntity> GetDistinctAsync<TEntity, TFilter>(TFilter filter, Pagination? pagination = null, CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase;

    /// <summary>
    /// Deletes every entity of the specified type.
    /// </summary>
    Task DeleteAllAsync<TEntity>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the entity with the given identifier.
    /// </summary>
    Task DeleteAsync<TEntity>(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the entities matching the supplied filter.
    /// </summary>
    Task DeleteAsync<TEntity, TFilter>(TFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new entity and returns its generated identifier.
    /// </summary>
    Task<long> InsertAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a batch of entities and returns their generated identifiers in the same order.
    /// </summary>
    Task<long[]> InsertAsync<TEntity>(TEntity[] entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts the entity or updates it when a matching record already exists, returning the resulting identifier.
    /// </summary>
    Task<long> UpsertAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates a batch of entities using the supplied filter to detect existing records.
    /// </summary>
    Task<long[]> UpsertAsync<TEntity, TFilter>(TEntity[] entity, TFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to update the supplied entity, returning <c>true</c> only when a record was actually changed.
    /// </summary>
    Task<bool> TryUpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default);
}
