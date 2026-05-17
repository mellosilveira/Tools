using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.WebApi.Application.Commands;

namespace MelloSilveiraTools.WebApi.Application.Commands.Crud.Update;

/// <summary>
/// Request consumed by <see cref="UpdateEntity{TEntity}"/>.
/// </summary>
/// <typeparam name="TEntity">Entity type being updated.</typeparam>
public sealed record UpdateEntityRequest<TEntity> : RequestBase
    where TEntity : EntityBase, new()
{
    /// <summary>Identifier of the entity to update (overrides any value already on <see cref="Entity"/>).</summary>
    public long Id { get; init; }

    /// <summary>Updated entity payload.</summary>
    public TEntity Entity { get; init; } = new();

    /// <summary>Human-readable resource name used to build localized error messages.</summary>
    public string ResourceName { get; init; } = string.Empty;
}
