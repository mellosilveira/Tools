using MelloSilveiraTools.Database.Infrastructure.Database.Models.Entities;

namespace MelloSilveiraTools.WebApi.Application.Operations.Add;

/// <summary>
/// Request consumed by <see cref="AddEntity{TEntity}"/>.
/// </summary>
/// <typeparam name="TEntity">Entity type being persisted.</typeparam>
public sealed record AddEntityRequest<TEntity> : OperationRequestBase
    where TEntity : EntityBase, new()
{
    /// <summary>Entity to insert.</summary>
    public TEntity Entity { get; init; } = new();

    /// <summary>Human-readable resource name used to build localized error messages.</summary>
    public string ResourceName { get; init; } = string.Empty;
}
