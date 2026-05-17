namespace MelloSilveiraTools.WebApi.Application.Commands.Crud.Delete;

/// <summary>
/// Request consumed by <see cref="DeleteEntity{TEntity}"/>.
/// </summary>
public sealed record DeleteEntityRequest : RequestBase
{
    /// <summary>Identifier of the entity to delete.</summary>
    public long Id { get; init; }

    /// <summary>Human-readable resource name used to build localized error messages.</summary>
    public string ResourceName { get; init; } = string.Empty;
}
