using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.WebApi.Application.Operations;
using MelloSilveiraTools.WebApi.Application.Operations.Crud;
using MelloSilveiraTools.WebApi.Application.Operations.Crud.Delete;
using MelloSilveiraTools.WebApi.Application.Operations.Crud.Read;
using MelloSilveiraTools.WebApi.Application.Operations.Crud.Update;
using MelloSilveiraTools.WebApi.ExtensionMethods;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MelloSilveiraTools.WebApi.Application.Endpoints;

/// <summary>
/// Maps the same CRUD + NDJSON-streaming surface as <see cref="Controllers.CrudController{TEntity,TFilter}"/>
/// onto a minimal-API <see cref="RouteGroupBuilder"/>. Hosts can choose between inheriting from the controller
/// and calling <see cref="MapCrud{TEntity,TFilter}"/> — both delegate to the same operations under
/// <c>MelloSilveiraTools.WebApi.Application.Operations.Add</c> and <c>...Operations.Crud</c>.
/// </summary>
public static class CrudEndpoints
{
    /// <summary>
    /// Maps the standard CRUD endpoints (add, read by id, paged read, update, delete and NDJSON stream)
    /// for the supplied entity / filter pair under <paramref name="pattern"/>. The add and stream endpoints
    /// are delegated to <see cref="AddEndpoints.MapAdd{TEntity}"/> and <see cref="StreamEndpoints.MapStream{TEntity, TFilter}"/>
    /// so hosts can also use those individually.
    /// </summary>
    /// <typeparam name="TEntity">The entity type handled by the endpoints.</typeparam>
    /// <typeparam name="TFilter">The filter type used to query the entity.</typeparam>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">Route prefix (e.g. <c>"/api/v1/things"</c>).</param>
    /// <param name="resourceName">Human-readable resource name used when building log and error messages.</param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> so callers can chain <c>.RequireAuthorization(...)</c>, <c>.WithTags(...)</c>, etc.</returns>
    public static RouteGroupBuilder MapCrud<TEntity, TFilter>(
        this IEndpointRouteBuilder builder,
        string pattern,
        string resourceName)
        where TEntity : EntityBase, new()
        where TFilter : FilterBase, new()
    {
        RouteGroupBuilder group = builder.MapGroup(pattern);

        group.MapAdd<TEntity>("/", resourceName);

        group
            .MapGet("/{id:long}", async (ReadEntityById<TEntity> operation, long id) => await operation
                .ProcessAsync(new ReadEntityByIdRequest { Id = id, ResourceName = resourceName })
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<OperationResponse<TEntity>>(StatusCodes.Status200OK)
            .Produces<OperationResponse>(StatusCodes.Status404NotFound)
            .Produces<OperationResponse>(StatusCodes.Status500InternalServerError)
            .WithName($"ReadById_{resourceName}")
            .WithSummary($"Retrieves a single {resourceName} by its identifier.");

        group
            .MapGet("/", async (HttpContext httpContext, ReadEntityPaged<TEntity, TFilter> operation,
                [AsParameters] TFilter filter, [AsParameters] Pagination pagination) => await operation
                .ProcessAsync(new ReadEntityPagedRequest<TFilter>
                {
                    Filter = filter,
                    Pagination = pagination,
                    ResourceName = resourceName,
                    CancellationToken = httpContext.RequestAborted,
                })
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<PagedOperationResponse<TEntity>>(StatusCodes.Status200OK)
            .Produces<OperationResponse>(StatusCodes.Status500InternalServerError)
            .WithName($"ReadPaged_{resourceName}")
            .WithSummary($"Retrieves a paginated list of {resourceName}.");

        group
            .MapPut("/{id:long}", async (UpdateEntity<TEntity> operation, long id, TEntity entity) => await operation
                .ProcessAsync(new UpdateEntityRequest<TEntity> { Id = id, Entity = entity, ResourceName = resourceName })
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<OperationResponse>(StatusCodes.Status200OK)
            .Produces<OperationResponse>(StatusCodes.Status204NoContent)
            .Produces<OperationResponse>(StatusCodes.Status500InternalServerError)
            .WithName($"Update_{resourceName}")
            .WithSummary($"Updates an existing {resourceName}.");

        group
            .MapDelete("/{id:long}", async (DeleteEntity<TEntity> operation, long id) => await operation
                .ProcessAsync(new DeleteEntityRequest { Id = id, ResourceName = resourceName })
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<OperationResponse>(StatusCodes.Status200OK)
            .Produces<OperationResponse>(StatusCodes.Status500InternalServerError)
            .WithName($"Delete_{resourceName}")
            .WithSummary($"Deletes a {resourceName} by its identifier.");

        group.MapStream<TEntity, TFilter>("/stream", resourceName);

        return group;
    }
}
