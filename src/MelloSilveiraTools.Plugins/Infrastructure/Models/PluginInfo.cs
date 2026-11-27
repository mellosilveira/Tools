using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.Plugins.Infrastructure.Models;

/// <summary>
/// Unified plugin representation shared across public catalog discovery (<c>GET /api/v1/plugins</c>),
/// community submission and moderation workflows (<c>/submissions</c>, <c>/upload</c>),
/// and superuser storage inspection (<c>GET /api/v1/plugins/inspect</c>).
/// Context-specific properties are omitted from JSON when <see langword="null"/> so public discovery
/// never leaks internal server storage paths (<see cref="File"/>) or moderation internals.
/// </summary>
public sealed record PluginInfo
{
    /// <summary>Logical plugin name without version suffix (or filename stem when an inspected filename is invalid).</summary>
    public required string Name { get; init; }

    /// <summary>Parsed semantic version of the plugin, or <see langword="null"/> when an inspected filename does not match the versioned naming pattern.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PluginVersion? Version { get; init; }

    /// <summary>Indicates whether every processable type in the plugin has been registered in runtime DI.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsFullyLoaded { get; init; }

    /// <summary>UTC timestamp captured when runtime registration completed, if loaded.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? FullyLoadedAt { get; init; }

    /// <summary>Map of discovered processable type full names to their runtime DI registration status.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, bool>? TypesLoadedStatus { get; init; }

    /// <summary>Canonical plugin storage partition where the inspected file resides (e.g., <c>loaded</c>, <c>incoming</c>, <c>pending</c>, <c>archive</c>, <c>quarantine</c>, <c>rejected</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Partition { get; init; }

    /// <summary>Physical storage metadata (<see cref="StorageEntryMetadata"/>) for the inspected plugin binary.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StorageEntryMetadata? File { get; init; }

    /// <summary>Indicates whether the inspected file passed naming, managed PE/CLI metadata, IL-only policy, and host architecture validation.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsValidAssembly { get; init; }

    /// <summary>Detailed validation or load error message when <see cref="IsValidAssembly"/> is <see langword="false"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ValidationError { get; init; }

    /// <summary>Unique submission database identifier.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? EntityId { get; init; }

    /// <summary>Author user identifier, or <see langword="null"/> for system-owned plugins.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? UserEntityId { get; init; }

    /// <summary>Foreign key identifier of the corresponding <c>storage_entry_metadata</c> record.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? StorageEntryMetadataEntityId { get; init; }

    /// <summary>Lowercase hex SHA-256 digest of the uploaded plugin binary.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sha256Hash { get; init; }

    /// <summary>Current moderation lifecycle status.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PluginSubmissionStatus? Status { get; init; }

    /// <summary>UTC timestamp when the submission record was created.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CreationTimestamp { get; init; }

    /// <summary>Identifier of the superuser who approved or rejected the submission, if reviewed.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ReviewedByUserEntityId { get; init; }

    /// <summary>UTC timestamp when the submission was reviewed, if reviewed.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? ReviewedAt { get; init; }

    /// <summary>Optional reviewer audit notes recorded on approval or rejection.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReviewNotes { get; init; }

    /// <summary>
    /// Creates a sanitized public runtime summary from a cached <see cref="RegisteredPlugin"/>.
    /// Intentionally omits <see cref="File"/> and all internal storage paths.
    /// </summary>
    /// <param name="registered">Cached registered plugin state.</param>
    /// <returns>A sanitized <see cref="PluginInfo"/> for public catalog discovery.</returns>
    public static PluginInfo FromRegistered(RegisteredPlugin registered)
    {
        Dictionary<string, bool> typesStatus = new(registered.TypesLoadedStatus.Count);
        foreach (KeyValuePair<Type, bool> entry in registered.TypesLoadedStatus)
            typesStatus[entry.Key.FullName ?? entry.Key.Name] = entry.Value;

        return new PluginInfo
        {
            Name = registered.Name,
            Version = registered.Version,
            IsFullyLoaded = registered.IsFullyLoaded,
            FullyLoadedAt = registered.FullyLoadedAt,
            TypesLoadedStatus = typesStatus
        };
    }

    /// <summary>
    /// Projects a <see cref="PluginSubmissionEntity"/> database record into a unified <see cref="PluginInfo"/>.
    /// </summary>
    /// <param name="entity">Database submission record.</param>
    /// <returns>A <see cref="PluginInfo"/> populated with submission and moderation metadata.</returns>
    public static PluginInfo FromSubmissionEntity(PluginSubmissionEntity entity) => new()
    {
        EntityId = entity.Id,
        UserEntityId = entity.UserId,
        StorageEntryMetadataEntityId = entity.StorageEntryMetadataId,
        Name = entity.PluginName,
        Version = PluginVersion.SafeParse(entity.PluginVersion),
        Sha256Hash = entity.Sha256Hash,
        Status = entity.Status,
        CreationTimestamp = entity.CreationTimestamp,
        ReviewedByUserEntityId = entity.ReviewedByUserId,
        ReviewedAt = entity.ReviewedAt,
        ReviewNotes = entity.ReviewNotes
    };

    /// <summary>
    /// Creates a superuser storage inspection snapshot from raw <see cref="StorageEntryMetadata"/>,
    /// static PE/architecture validation results, and optional runtime registration state.
    /// </summary>
    /// <param name="partition">Canonical plugin storage partition.</param>
    /// <param name="name">Logical plugin name (or filename stem when malformed).</param>
    /// <param name="version">Parsed semantic version, if valid.</param>
    /// <param name="file">Physical storage metadata of the plugin binary.</param>
    /// <param name="isValidAssembly">Whether filename, PE/CLI metadata, IL-only policy, and architecture checks passed.</param>
    /// <param name="validationError">Validation or load error message when invalid.</param>
    /// <param name="registered">Optional runtime registration state for active partitions.</param>
    /// <returns>A <see cref="PluginInfo"/> populated with storage and runtime diagnostic metadata.</returns>
    public static PluginInfo FromInspection(
        string partition,
        string name,
        PluginVersion? version,
        StorageEntryMetadata file,
        bool isValidAssembly,
        string? validationError = null,
        RegisteredPlugin? registered = null)
    {
        Dictionary<string, bool> typesStatus = [];
        if (registered is not null)
        {
            foreach (KeyValuePair<Type, bool> entry in registered.TypesLoadedStatus)
                typesStatus[entry.Key.FullName ?? entry.Key.Name] = entry.Value;
        }

        return new PluginInfo
        {
            Partition = partition,
            Name = registered?.Name ?? name,
            Version = registered?.Version ?? version,
            File = file,
            IsValidAssembly = isValidAssembly,
            ValidationError = validationError,
            IsFullyLoaded = registered?.IsFullyLoaded ?? false,
            FullyLoadedAt = registered?.FullyLoadedAt,
            TypesLoadedStatus = typesStatus
        };
    }
}
