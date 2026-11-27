using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;

namespace MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;

/// <summary>
/// Base record for user-scoped entities stored in PostgreSQL tables protected by Row-Level Security (RLS).
/// A <see langword="null"/> <see cref="UserId"/> represents a public record (generated anonymously or published
/// publicly by a superuser), whereas a positive value isolates the record to that owning user.
/// </summary>
public abstract record UserScopedEntityBase : EntityBase
{
    /// <summary>
    /// Identifier of the owning user, or <see langword="null"/> for public records.
    /// For non-superusers, <c>PostgresRepository</c> automatically enforces this value from the active
    /// <see cref="ICurrentUserProvider"/> before executing write statements.
    /// </summary>
    [Column]
    public long? UserId { get; init; }
}
