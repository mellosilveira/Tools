using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using System.Diagnostics.CodeAnalysis;

namespace MelloSilveiraTools.Database.Repositories;

/// <summary>
/// Provides a generic, filter-driven, AOT-compatible abstraction over persistent storage for entity types.
/// </summary>
/// <remarks>
/// All asynchronous members accept a <see cref="CancellationToken"/> and will throw
/// <see cref="OperationCanceledException"/> when cancellation is signalled.
///
/// Transient database failures (connection drops, broken protocol, network blips, command timeouts caused by
/// transient conditions) are absorbed and retried by the resilience pipeline. Once the pipeline budget is exhausted
/// the underlying exceptions are re-thrown to the caller. Non-transient failures — for example unique-constraint
/// violations, permission errors, or definitive command timeouts — are not retried and are propagated as <see cref="TimeoutException"/>.
/// </remarks>
public interface IRepository
{
    /// <summary>
    /// Counts the entities matching the supplied filter.
    /// </summary>
    Task<long> CountAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TFilter filter,
        CancellationToken cancellationToken = default)
        where TFilter : FilterBase;

    /// <summary>
    /// Indicates whether an entity with the given identifier exists.
    /// </summary>
    Task<bool> ExistAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether any entity matching the supplied filter exists.
    /// </summary>
    Task<bool> ExistAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TFilter filter,
        CancellationToken cancellationToken = default)
        where TFilter : FilterBase;

    /// <summary>
    /// Gets the first entity matching the supplied filter, or the default value when none is found.
    /// </summary>
    Task<TEntity?> GetFirstOrDefaultAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TFilter filter,
        SortOrder sortOrder = SortOrder.Asc,
        CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase;

    /// <summary>
    /// Gets the entity with the given identifier, or <c>null</c> when it does not exist.
    /// </summary>
    Task<TEntity?> GetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        long id,
        CancellationToken cancellationToken = default)
        where TEntity : class, new();

    /// <summary>
    /// Streams all entities matching the supplied filter, optionally paginated.
    /// </summary>
    IAsyncEnumerable<TEntity> GetAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TFilter filter,
        Pagination? pagination = null,
        CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase;

    /// <summary>
    /// Returns the entity whose <c>[UniqueColumn]</c>-annotated column equals <paramref name="value"/>,
    /// or <c>null</c> when no row matches. Useful as a typed lookup on hash-based identifiers
    /// (and any other single-column unique key) without needing to write a <see cref="FilterBase"/>-derived filter.
    /// </summary>
    /// <param name="value">Value to compare against the unique column. Bound as a SQL parameter.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="TEntity"/> has zero or more than one <c>[UniqueColumn]</c>-annotated
    /// property — for composite unique keys, use a custom <c>FilterBase</c> with multiple <c>[FilterColumn]</c>
    /// entries instead.
    /// </exception>
    Task<TEntity?> GetByUniqueColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        object value,
        CancellationToken cancellationToken = default)
        where TEntity : class, new();

    /// <summary>
    /// Streams the distinct entities matching the supplied filter, optionally paginated.
    /// </summary>
    IAsyncEnumerable<TEntity> GetDistinctAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TFilter filter,
        Pagination? pagination = null,
        CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase;

    /// <summary>
    /// Deletes every entity of the specified type.
    /// </summary>
    Task DeleteAllAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the entity with the given identifier.
    /// </summary>
    Task DeleteAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the entities matching the supplied filter.
    /// </summary>
    Task DeleteAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new entity and returns its generated identifier.
    /// </summary>
    Task<long> InsertAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts the entity. When a row with the same unique-key value already exists, returns the
    /// existing row's primary key WITHOUT modifying the existing record (semantics: ON CONFLICT DO NOTHING
    /// + fallback SELECT in a single atomic statement).
    /// </summary>
    /// <returns>
    /// <c>Inserted</c> is <c>true</c> when a new row was created and <c>false</c> when an existing
    /// row was returned. <c>Id</c> is always the primary key of the canonical row (new or pre-existing).
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="TEntity"/> has no <c>[UniqueColumn]</c>-annotated property —
    /// without a unique constraint there is no conflict to detect, so callers should use <see cref="InsertAsync{TEntity}(TEntity, CancellationToken)"/> instead.
    /// </exception>
    Task<Result<long>> TryInsertAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a batch of entities and returns their generated identifiers in the same order.
    /// </summary>
    Task<long[]> InsertAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        TEntity[] entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts the entity or updates it when a matching record already exists, returning the resulting identifier.
    /// </summary>
    Task<long> UpsertAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates a batch of entities using the supplied filter to detect existing records.
    /// </summary>
    Task<long[]> UpsertAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TEntity[] entity,
        TFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to update the supplied entity, returning <c>true</c> only when a record was actually changed.
    /// </summary>
    Task<bool> TryUpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default);
}
