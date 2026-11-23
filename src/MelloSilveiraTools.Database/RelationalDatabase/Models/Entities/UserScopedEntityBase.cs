using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.Security;

namespace MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;

/// <summary>
/// Base record for user-scoped entities stored in PostgreSQL tables partitioned by <c>user_id</c>
/// and protected by Row-Level Security (RLS).
/// </summary>
public abstract record UserScopedEntityBase : EntityBase
{
    /// <summary>
    /// Identifier of the owning user. When left as <c>0</c>, <c>PostgresRepository</c> automatically populates 
    /// it from the active <see cref="ICurrentUserProvider"/> before executing write statements.
    /// </summary>
    [Column]
    public long UserId { get; init; }
}
