using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.WebApi.Application.Commands;

namespace MelloSilveiraTools.WebApi.Application.Commands.Crud.Read;

/// <summary>
/// Request consumed by <see cref="ReadEntityPaged{TEntity, TFilter}"/>.
/// </summary>
/// <typeparam name="TFilter">Filter type used to query the entity.</typeparam>
public sealed record ReadEntityPagedRequest<TFilter> : RequestBase
    where TFilter : FilterBase, new()
{
    /// <summary>Filter criteria applied to the query.</summary>
    public TFilter Filter { get; init; } = new();

    /// <summary>Pagination parameters (offset, limit, sort).</summary>
    public Pagination Pagination { get; init; } = new();

    /// <summary>Human-readable resource name used to build localized error messages.</summary>
    public string ResourceName { get; init; } = string.Empty;

    /// <summary>Cancellation token (typically <c>HttpContext.RequestAborted</c>).</summary>
    public CancellationToken CancellationToken { get; init; }
}
