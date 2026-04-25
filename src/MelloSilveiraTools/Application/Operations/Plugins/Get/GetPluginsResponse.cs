using MelloSilveiraTools.Infrastructure.Plugins.Models;

namespace MelloSilveiraTools.Application.Operations.Plugins.Get;

/// <summary>
/// Response returned by the <c>GetPlugins</c> operation, listing the plugins that match the request filter.
/// </summary>
public record GetPluginsResponse : OperationListResponseBase<RegisteredPlugin>;
