namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

/// <summary>
/// A <see cref="LoadedPlugin"/> whose processable types have been registered in the DI container.
/// Tracks which types have been loaded and whether the plugin is fully registered.
/// </summary>
public record RegisteredPlugin : DiscoveredPlugin
{
    private readonly Dictionary<Type, bool> _typeLoadedStatus;

    public RegisteredPlugin(LoadedPlugin loaded) : base(loaded)
    {
        _typeLoadedStatus = loaded.ProcessableTypes.ToDictionary(t => t, _ => false);
    }

    public IReadOnlyDictionary<Type, bool> TypesLoadedStatus => _typeLoadedStatus;
    public bool IsFullyLoaded => TypesLoadedStatus.Count > 0 && TypesLoadedStatus.Values.All(v => v);
    public DateTimeOffset? FullyLoadedAt { get; private set; }

    public void MarkTypeLoaded(Type type)
    {
        _typeLoadedStatus[type] = true;

        if (IsFullyLoaded)
            FullyLoadedAt = DateTimeOffset.UtcNow;
    }
}
