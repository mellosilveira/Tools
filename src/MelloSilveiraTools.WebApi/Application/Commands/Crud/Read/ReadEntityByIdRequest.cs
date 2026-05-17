using MelloSilveiraTools.WebApi.Application.Commands;

namespace MelloSilveiraTools.WebApi.Application.Commands.Crud.Read;

/// <summary>
/// Request consumed by <see cref="ReadEntityById{TEntity}"/>.
/// </summary>
public sealed record ReadEntityByIdRequest : RequestBase
{
    /// <summary>Identifier of the entity to load.</summary>
    public long Id { get; init; }

    /// <summary>Human-readable resource name used to build localized error messages.</summary>
    public string ResourceName { get; init; } = string.Empty;
}
