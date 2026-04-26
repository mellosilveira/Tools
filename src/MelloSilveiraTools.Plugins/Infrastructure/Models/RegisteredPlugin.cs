namespace MelloSilveiraTools.Plugins.Infrastructure.Models;

/// <summary>
/// A <see cref="LoadedPlugin"/> whose processable types have been registered in the DI container.
/// Tracks which types have been loaded and whether the plugin is fully registered.
/// </summary>
public record RegisteredPlugin : DiscoveredPlugin
{
    private readonly Dictionary<Type, bool> _typeLoadedStatus;

    /// <summary>
    /// Initializes a new <see cref="RegisteredPlugin"/> from a <paramref name="loaded"/> plugin, with all processable types marked as not yet registered.
    /// </summary>
    public RegisteredPlugin(LoadedPlugin loaded) : base(loaded)
    {
        _typeLoadedStatus = loaded.ProcessableTypes.ToDictionary(t => t, _ => false);
    }

    /// <summary>
    /// Per-type flag indicating whether each processable type has been registered in the DI container.
    /// </summary>
    public IReadOnlyDictionary<Type, bool> TypesLoadedStatus => _typeLoadedStatus;

    /// <summary>
    /// Returns <see langword="true"/> when all processable types have been registered.
    /// </summary>
    public bool IsFullyLoaded => TypesLoadedStatus.Count > 0 && TypesLoadedStatus.Values.All(v => v);

    /// <summary>
    /// Timestamp captured when the plugin transitioned to <see cref="IsFullyLoaded"/>; <see langword="null"/> while still in progress.
    /// </summary>
    public DateTimeOffset? FullyLoadedAt { get; private set; }

    /// <summary>
    /// Marks the given <paramref name="type"/> as registered and updates <see cref="FullyLoadedAt"/> once all types are done.
    /// </summary>
    public void MarkTypeLoaded(Type type)
    {
        _typeLoadedStatus[type] = true;

        if (IsFullyLoaded)
            FullyLoadedAt = DateTimeOffset.UtcNow;
    }
}
