namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

/// <summary>
/// Metadata about a discovered plugin DLL, parsed from the filename pattern: {name}.v{major}.{minor}.{patch}.dll.
/// </summary>
public record PluginBaseInfo
{
    public PluginBaseInfo() { }

    public PluginBaseInfo(string name, PluginVersion version, string fullPath, DateTimeOffset discoveredAt)
    {
        Name = name;
        Version = version;
        FullPath = fullPath;
        DiscoveredAt = discoveredAt;
    }

    protected PluginBaseInfo(PluginBaseInfo pluginBaseInfo)
    {
        Name = pluginBaseInfo.Name;
        Version = pluginBaseInfo.Version;
        FullPath = pluginBaseInfo.FullPath;
        DiscoveredAt = pluginBaseInfo.DiscoveredAt;
    }

    /// <summary>
    /// Plugin name without version (e.g., "SoftTissue.Plugins").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Parsed semantic version.
    /// </summary>
    public PluginVersion Version { get; }

    /// <summary>
    /// Absolute path to the DLL file.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Timestamp when the plugin was first discovered.
    /// </summary>
    public DateTimeOffset DiscoveredAt { get; }
}

// GUARDAR OS METADADOS NO BANCO DE DADOS.
// APLICAÇÃO FICA VERIFICANDO SE A VERSÃO NO BANCO DE DADOS É A MESMA QUE
// A VERSÃO CARREGADA NA APLICAÇÃO.
// - Facilidade em rollback para que nós possamos apenas mudar a versão
//   no banco de dados e a aplicação atualiza tudo automaticamente.

// Tabela com Nome, versão e "onde obter o plugin" (dfs, blob storage)
// -> geral para todas as aplicações
// Tabela com informações da máquina
