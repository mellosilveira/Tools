using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.WebApi.Application.Operations;

namespace MelloSilveiraTools.Plugins.Application.Operations.Get;

/// <summary>
/// Response returned by the <c>GetPlugins</c> operation, listing the plugins that match the request filter.
/// </summary>
public record GetPluginsResponse : OperationListResponseBase<RegisteredPlugin>;
