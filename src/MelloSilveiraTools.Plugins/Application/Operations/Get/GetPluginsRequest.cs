using MelloSilveiraTools.WebApi.Application.Operations;

namespace MelloSilveiraTools.Plugins.Application.Operations.Get
{
    /// <summary>
    /// Request used to filter plugins returned by the <c>GetPlugins</c> operation.
    /// </summary>
    public record GetPluginsRequest : OperationRequestBase
    {
        /// <summary>
        /// Optional plugin name to filter by.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Optional plugin version (in <c>PluginVersion</c> string form) to filter by.
        /// </summary>
        public string? Version { get; init; }

        /// <summary>
        /// When set, restricts the result to plugins that are fully loaded (<c>true</c>) or not fully loaded (<c>false</c>).
        /// </summary>
        public bool? FullyLoaded { get; init; }
    }
}
