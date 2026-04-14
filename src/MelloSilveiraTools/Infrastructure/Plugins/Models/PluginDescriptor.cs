namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

/// <summary>
/// Metadata about a discovered plugin DLL, parsed from the filename pattern: {name}.v{major}.{minor}.{patch}.dll.
/// </summary>
/// <param name="Name">Plugin name without version (e.g., "SoftTissue.Plugins").</param>
/// <param name="Version">Parsed semantic version.</param>
/// <param name="FullPath">Absolute path to the DLL file.</param>
/// <param name="DiscoveredAt">Timestamp when the plugin was first discovered.</param>
public record PluginDescriptor(string Name, PluginVersion Version, string FullPath, DateTimeOffset DiscoveredAt);

// GUARDAR OS METADADOS NO BANCO DE DADOS.
// APLICAÇÃO FICA VERIFICANDO SE A VERSÃO NO BANCO DE DADOS É A MESMA QUE
// A VERSÃO CARREGADA NA APLICAÇÃO.
// - Facilidade em rollback para que nós possamos apenas mudar a versão
//   no banco de dados e a aplicação atualiza tudo automaticamente.

// Tabela com Nome, versão e "onde obter o plugin" (dfs, blob storage)
// -> geral para todas as aplicações
// Tabela com informações da máquina
