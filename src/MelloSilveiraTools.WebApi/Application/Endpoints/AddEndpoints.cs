using MelloSilveiraTools.Database.Infrastructure.Database.Models.Entities;
using MelloSilveiraTools.WebApi.Application.Operations.Add;
using MelloSilveiraTools.WebApi.ExtensionMethods;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MelloSilveiraTools.WebApi.Application.Endpoints;

/// <summary>
/// Maps a single insert (POST) endpoint backed by the <see cref="AddEntity{TEntity}"/> operation.
/// Available standalone (for hosts that only need the create path) or composed by <c>CrudEndpoints.MapCrud</c>.
/// </summary>
public static class AddEndpoints
{
    /// <summary>
    /// Maps a <c>POST</c> endpoint at <paramref name="pattern"/> that delegates to <see cref="AddEntity{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to be inserted.</typeparam>
    /// <param name="builder">Endpoint route builder used to register the endpoint.</param>
    /// <param name="pattern">Route pattern (e.g. <c>"/"</c> when called from a route group, or a full path).</param>
    /// <param name="resourceName">Human-readable resource name used to build localized error messages.</param>
    /// <returns>The <see cref="RouteHandlerBuilder"/> so the caller can chain metadata (<c>.RequireAuthorization()</c>, <c>.WithTags(...)</c>, etc.).</returns>
    public static RouteHandlerBuilder MapAdd<TEntity>(
        this IEndpointRouteBuilder builder,
        string pattern,
        string resourceName)
        where TEntity : EntityBase, new()
        => builder
            .MapPost(pattern, async (AddEntity<TEntity> operation, TEntity entity) => await operation
                .ProcessAsync(new AddEntityRequest<TEntity> { Entity = entity, ResourceName = resourceName })
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<AddResponse>(StatusCodes.Status201Created)
            .Produces<AddResponse>(StatusCodes.Status500InternalServerError)
            .WithName($"Add_{resourceName}")
            .WithSummary($"Persists a new {resourceName}.");
}
