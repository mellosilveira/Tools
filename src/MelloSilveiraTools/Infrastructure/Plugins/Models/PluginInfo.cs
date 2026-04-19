namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

public record PluginInfo: PluginBaseInfo
{
    private readonly Dictionary<Type, bool> _typeLoadedStatus;

    public PluginInfo(PluginAssemblyInfo assemblyInfo) : base(assemblyInfo)
    {
        _typeLoadedStatus = assemblyInfo.ProcessableTypes.ToDictionary(t => t, _ => false);
    }

    public IReadOnlyDictionary<Type, bool> TypesLoadedStatus => _typeLoadedStatus;
    public bool IsFullyLoaded => TypesLoadedStatus.Count > 0 && TypesLoadedStatus.Values.All(loaded => loaded);
    public DateTimeOffset? FullyLoadedAt { get; private set; }

    public void MarkTypeLoaded(Type type)
    {
        _typeLoadedStatus[type] = true;

        if (IsFullyLoaded)
            FullyLoadedAt = DateTimeOffset.UtcNow;
    }

    public void RegisterType(Type type)
    {
        _typeLoadedStatus.TryAdd(type, false);
    }
}
