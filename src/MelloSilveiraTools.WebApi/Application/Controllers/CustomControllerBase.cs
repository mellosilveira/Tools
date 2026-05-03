using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using MelloSilveiraTools.WebApi.Application.Operations.Crud.Add;
using MelloSilveiraTools.WebApi.ExtensionMethods;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.WebApi.Application.Controllers;

/// <summary>
/// Base controller providing shared behavior (logging, generic add helper, NDJSON streaming) for the project's controllers.
/// </summary>
public class CustomControllerBase(ILogger logger) : Controller
{
    /// <summary>
    /// Logger used to report failures and diagnostics from controller actions.
    /// </summary>
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// Persists <paramref name="entity"/> through the supplied <see cref="AddEntity{TEntity}"/> operation and
    /// projects the result onto an <see cref="ActionResult{T}"/>. Available to every controller that derives
    /// from <see cref="CustomControllerBase"/> — not only the generic <c>CrudController</c>.
    /// </summary>
    /// <typeparam name="TEntity">Entity type being persisted.</typeparam>
    /// <param name="operation">Operation that performs the insert (resolved from DI).</param>
    /// <param name="entity">Entity payload received from the request.</param>
    /// <param name="resourceName">Human-readable resource name used to build localized error messages.</param>
    protected async Task<ActionResult<AddResponse>> Add<TEntity>(AddEntity<TEntity> operation, TEntity entity, string resourceName) where TEntity : EntityBase, new()
        => await operation
            .ProcessAsync(new AddEntityRequest<TEntity> { Entity = entity, ResourceName = resourceName })
            .BuildHttpResponseAsync()
            .ConfigureAwait(false);

    /// <summary>
    /// Streams a sequence of entities to the response as newline-delimited JSON (NDJSON).
    /// Delegates to <see cref="StreamEndpoints.WriteNdjsonAsync{T}"/> so controllers and minimal-API endpoints
    /// share the exact same writer (headers, trailers, cancellation handling).
    /// </summary>
    /// <param name="entities">Asynchronous sequence of items to be streamed.</param>
    /// <param name="resourceName">Human-readable resource name used when reporting streaming failures.</param>
    protected Task Stream<T>(IAsyncEnumerable<T> entities, string resourceName)
        => entities.WriteNdjsonAsync(HttpContext, Logger, resourceName);
}
