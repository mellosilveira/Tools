using MelloSilveiraTools.Infrastructure.Database.Attributes;

namespace MelloSilveiraTools.Infrastructure.Database.Models.Entities;

/// <summary>
/// Represents the base entity for database.
/// </summary>
public abstract record EntityBase
{
    /// <summary>
    /// Primary key that uniquely identifies the entity in its table.
    /// </summary>
    [PrimaryKeyColumn]
    public long Id { get; init; }

    /// <summary>
    /// Moment (UTC) in which the entity was created.
    /// </summary>
    [Column]
    public DateTimeOffset CreationTimestamp { get; init; } = DateTimeOffset.UtcNow;
}
