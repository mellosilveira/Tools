namespace MelloSilveiraTools.WebApi.Application.Operations.Crud;

/// <summary>
/// Request consumed by <see cref="ReadEntityById{TEntity}"/>.
/// </summary>
public sealed record ReadEntityByIdRequest : OperationRequestBase
{
    /// <summary>Identifier of the entity to load.</summary>
    public long Id { get; init; }

    /// <summary>Human-readable resource name used to build localized error messages.</summary>
    public string ResourceName { get; init; } = string.Empty;
}
