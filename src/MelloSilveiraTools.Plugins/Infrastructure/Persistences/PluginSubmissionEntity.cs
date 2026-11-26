using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Plugins.Infrastructure.Models;

namespace MelloSilveiraTools.Plugins.Infrastructure.Persistences;

/// <summary>
/// Database entity representing a submitted plugin assembly referencing its file record in
/// <see cref="StorageEntryMetadataEntity"/> (<c>storage_entry_metadata</c>), with SHA-256 integrity,
/// per-plugin author ownership, and moderation metadata.
/// </summary>
[Table("plugin_submissions")]
public record PluginSubmissionEntity : UserScopedEntityBase
{
    /// <summary>Foreign key referencing <c>storage_entry_metadata.id</c> (<see cref="StorageEntryMetadataEntity"/>).</summary>
    [Column]
    public long StorageEntryMetadataId { get; init; }

    /// <summary>Logical plugin name parsed from <c>{name}.v{major}.{minor}.{patch}.dll</c>.</summary>
    [Column]
    public string PluginName { get; init; } = string.Empty;

    /// <summary>Semantic version string (e.g. <c>v1.0.0</c>).</summary>
    [Column]
    public string PluginVersion { get; init; } = string.Empty;

    /// <summary>Lowercase hex-encoded SHA-256 digest of the uploaded assembly binary.</summary>
    [Column]
    public string Sha256Hash { get; init; } = string.Empty;

    /// <summary>Current moderation lifecycle status.</summary>
    [Column]
    public PluginSubmissionStatus Status { get; init; }

    /// <summary>Identifier of the superuser who approved or rejected the submission, if reviewed.</summary>
    [Column]
    public long? ReviewedByUserId { get; init; }

    /// <summary>UTC timestamp when a superuser reviewed the submission, if reviewed.</summary>
    [Column]
    public DateTimeOffset? ReviewedAt { get; init; }

    /// <summary>Optional reviewer notes explaining approval or rejection.</summary>
    [Column]
    public string? ReviewNotes { get; init; }
}
