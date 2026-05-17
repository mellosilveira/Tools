using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Operations;
using MelloSilveiraTools.Plugins.Application.Operations.Cache;
using MelloSilveiraTools.Plugins.Application.Operations.Get;
using MelloSilveiraTools.Plugins.Application.Operations.Load;
using MelloSilveiraTools.Plugins.Application.Operations.Reload;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.WebApi.ExtensionMethods;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MelloSilveiraTools.Plugins.Application.Endpoints;

/// <summary>
/// Maps the same surface as <see cref="Controllers.PluginController"/> using minimal APIs.
/// Hosts that don't wire <c>app.MapControllers()</c> can reach the plugin runtime through this extension instead.
/// </summary>
public static class PluginEndpoints
{
    /// <summary>
    /// Maps every plugin-management endpoint (list, load, reload, cache lifecycle) under <paramref name="pattern"/>.
    /// </summary>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">Route prefix; defaults to <c>"/api/v1/plugins"</c>.</param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> so callers can chain <c>.RequireAuthorization(...)</c>, <c>.WithTags(...)</c>, etc.</returns>
    public static RouteGroupBuilder MapPluginEndpoints(
        this IEndpointRouteBuilder builder,
        string pattern = "/api/v1/plugins")
    {
        RouteGroupBuilder group = builder.MapGroup(pattern);

        group
            .MapGet("/", async (GetPlugins operation, [AsParameters] GetPluginsRequest request) => await operation.ExecuteAsync(request).ToHttpResultAsync().ConfigureAwait(false))
            .Produces<ListedResult<RegisteredPlugin>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName("GetPlugins")
            .WithSummary("Returns the plugins currently known to the host.");

        group
            .MapPost("/load", async (LoadPlugins operation, [AsParameters] PluginsRequest request) => await operation.ExecuteAsync(request).ToHttpResultAsync().ConfigureAwait(false))
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName("LoadPlugins")
            .WithSummary("Loads plugins matching the supplied name and/or version.");

        group
            .MapPost("/load/all", async (LoadPlugins operation) => await operation.ExecuteAsync(new PluginsRequest()).ToHttpResultAsync().ConfigureAwait(false))
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName("LoadAllPlugins")
            .WithSummary("Loads every plugin known to the host.");

        group
            .MapPost("/reload", async (ReloadPlugins operation, [AsParameters] ReloadPluginsRequest request) => await operation.ExecuteAsync(request).ToHttpResultAsync().ConfigureAwait(false))
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName("ReloadPlugins")
            .WithSummary("Reloads plugins matching the supplied name and/or version.");

        group
            .MapPost("/reload/all", async (ReloadPlugins operation) => await operation.ExecuteAsync(new ReloadPluginsRequest()).ToHttpResultAsync().ConfigureAwait(false))
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName("ReloadAllPlugins")
            .WithSummary("Reloads every plugin known to the host.");

        group
            .MapDelete("/cache", async (ClearPluginCache operation) => await operation.ExecuteAsync().ToHttpResultAsync().ConfigureAwait(false))
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName("ClearPluginCache")
            .WithSummary("Clears the plugin cache.");

        group
            .MapPost("/cache/{target}/persist", async (PersistPluginCache operation, [AsParameters] PluginsRequest request) => await operation.ExecuteAsync(request).ToHttpResultAsync().ConfigureAwait(false))
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName("PersistPluginCache")
            .WithSummary("Persists the plugin cache to the target identified by the {target} route segment.");

        group
            .MapPost("/cache/{target}/restore", async (RestorePluginCache operation, [AsParameters] PluginsRequest request) => await operation.ExecuteAsync(request).ToHttpResultAsync().ConfigureAwait(false))
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName("RestorePluginCache")
            .WithSummary("Restores the plugin cache from the target identified by the {target} route segment.");

        return group;
    }
}
