using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Read;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Update;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Reflection;
using System.Text;

namespace MelloSilveiraTools.WebApi.Application.Endpoints;

/// <summary>
/// Maps the same CRUD + NDJSON-streaming surface as <see cref="Controllers.CrudController{TEntity,TFilter}"/>
/// onto a minimal-API <see cref="RouteGroupBuilder"/>. Hosts can choose between inheriting from the controller
/// and calling <see cref="MapCrud{TEntity,TFilter}"/> — both delegate to the same operations under
/// <c>MelloSilveiraTools.WebApi.Application.Commands.Crud</c>.
/// </summary>
public static class CrudEndpoints
{
    private const string EntitySuffix = "Entity";

    /// <summary>
    /// Maps the standard CRUD endpoints (add, read by id, paged read, update, delete and NDJSON stream)
    /// for the supplied entity / filter pair under <paramref name="pattern"/>. When <paramref name="pattern"/>,
    /// <paramref name="resourceName"/>, or <paramref name="tag"/> are omitted, they are inferred automatically
    /// from <typeparamref name="TEntity"/>'s <see cref="TableAttribute"/> and CLR type name.
    /// </summary>
    /// <typeparam name="TEntity">The entity type handled by the endpoints.</typeparam>
    /// <typeparam name="TFilter">The filter type used to query the entity.</typeparam>
    /// <param name="builder">Endpoint route builder used to register the endpoints.</param>
    /// <param name="pattern">
    /// Optional route prefix (e.g. <c>"/api/v1/things"</c>). When <see langword="null"/> or whitespace,
    /// inferred as <c>"/api/v1/{kebab-case-table-or-entity-name}"</c>.
    /// </param>
    /// <param name="resourceName">
    /// Optional human-readable resource name used in log, summary, and error messages. When <see langword="null"/>
    /// or whitespace, inferred from <typeparamref name="TEntity"/> without the <c>Entity</c> suffix.
    /// </param>
    /// <param name="requireAuthForMutations">
    /// When <see langword="true"/>, attaches <c>.RequireAuthorization()</c> to destructive mutation routes
    /// (<c>PUT</c> and <c>DELETE</c>) while leaving read, stream, and append-only creation accessible to anonymous callers.
    /// </param>
    /// <param name="tag">
    /// Optional OpenAPI tag attached to the route group. When <see langword="null"/> or whitespace,
    /// inferred in PascalCase from <typeparamref name="TEntity"/>'s <see cref="TableAttribute"/> or CLR type name.
    /// </param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> so callers can chain <c>.RequireAuthorization(...)</c>, <c>.RequireRateLimiting(...)</c>, etc.</returns>
    public static RouteGroupBuilder MapCrud<TEntity, TFilter>(
        this IEndpointRouteBuilder builder,
        string? pattern = null,
        string? resourceName = null,
        bool requireAuthForMutations = false,
        string? tag = null)
        where TEntity : EntityBase, new()
        where TFilter : FilterBase, new()
    {
        string effectivePattern = ResolveRoutePattern<TEntity>(pattern);
        string effectiveResourceName = ResolveResourceName<TEntity>(resourceName);
        string effectiveTag = ResolveTag<TEntity>(tag);

        RouteGroupBuilder group = builder.MapGroup(effectivePattern).WithTags(effectiveTag);

        group.MapAdd<TEntity>("/", effectiveResourceName);

        group
            .MapGet("/{id:long}", async (HttpContext httpContext, [FromServices] ReadEntityById<TEntity> operation, long id) => await operation
                .ExecuteAsync(new ReadEntityByIdRequest { Id = id, ResourceName = effectiveResourceName }, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<Result<TEntity>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName($"ReadById_{effectiveResourceName}")
            .WithSummary($"Retrieves a single {effectiveResourceName} by its identifier.");

        group
            .MapGet("/", async (HttpContext httpContext, [FromServices] ReadEntityPaged<TEntity, TFilter> operation,
                [AsParameters] TFilter filter, [AsParameters] Pagination pagination) => await operation
                .ExecuteAsync(new ReadEntityPagedRequest<TFilter>
                {
                    Filter = filter,
                    Pagination = pagination,
                    ResourceName = effectiveResourceName
                }, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<PagedResult<TEntity>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName($"ReadPaged_{effectiveResourceName}")
            .WithSummary($"Retrieves a paginated list of {effectiveResourceName}.");

        RouteHandlerBuilder updateEndpoint = group
            .MapPut("/{id:long}", async (HttpContext httpContext, [FromServices] UpdateEntity<TEntity> operation, long id, [FromBody] TEntity entity) => await operation
                .ExecuteAsync(new UpdateEntityRequest<TEntity> { Id = id, Entity = entity, ResourceName = effectiveResourceName }, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status204NoContent)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName($"Update_{effectiveResourceName}")
            .WithSummary($"Updates an existing {effectiveResourceName}.");

        RouteHandlerBuilder deleteEndpoint = group
            .MapDelete("/{id:long}", async (HttpContext httpContext, [FromServices] DeleteEntity<TEntity> operation, long id) => await operation
                .ExecuteAsync(new DeleteEntityRequest { Id = id, ResourceName = effectiveResourceName }, httpContext.RequestAborted)
                .ToHttpResultAsync()
                .ConfigureAwait(false))
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status500InternalServerError)
            .WithName($"Delete_{effectiveResourceName}")
            .WithSummary($"Deletes a {effectiveResourceName} by its identifier.");

        if (requireAuthForMutations)
        {
            updateEndpoint.RequireAuthorization();
            deleteEndpoint.RequireAuthorization();
        }

        group.MapStream<TEntity, TFilter>("/stream", effectiveResourceName);

        return group;
    }

    private static string ResolveRoutePattern<TEntity>(string? explicitPattern)
    {
        if (!string.IsNullOrWhiteSpace(explicitPattern))
            return explicitPattern;

        TableAttribute? tableAttr = typeof(TEntity).GetCustomAttribute<TableAttribute>();
        if (tableAttr is not null && !string.IsNullOrWhiteSpace(tableAttr.Name))
            return $"/api/v1/{tableAttr.Name.Replace('_', '-')}";

        string baseName = StripEntitySuffix(typeof(TEntity).Name);
        return $"/api/v1/{ConvertPascalToKebabCase(baseName)}";
    }

    private static string ResolveResourceName<TEntity>(string? explicitResourceName)
    {
        if (!string.IsNullOrWhiteSpace(explicitResourceName))
            return explicitResourceName;

        string baseName = StripEntitySuffix(typeof(TEntity).Name);
        StringBuilder builder = new(baseName.Length + 8);
        for (int i = 0; i < baseName.Length; i++)
        {
            char c = baseName[i];
            if (i > 0 && char.IsUpper(c))
            {
                builder.Append(' ');
                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string ResolveTag<TEntity>(string? explicitTag)
    {
        if (!string.IsNullOrWhiteSpace(explicitTag))
            return explicitTag;

        TableAttribute? tableAttr = typeof(TEntity).GetCustomAttribute<TableAttribute>();
        if (tableAttr is not null && !string.IsNullOrWhiteSpace(tableAttr.Name))
            return ConvertSnakeToPascalCase(tableAttr.Name);

        return StripEntitySuffix(typeof(TEntity).Name);
    }

    private static string StripEntitySuffix(string typeName) =>
        typeName.EndsWith(EntitySuffix, StringComparison.Ordinal) && typeName.Length > EntitySuffix.Length
            ? typeName[..^EntitySuffix.Length]
            : typeName;

    private static string ConvertPascalToKebabCase(string pascal)
    {
        StringBuilder builder = new(pascal.Length + 8);
        for (int i = 0; i < pascal.Length; i++)
        {
            char c = pascal[i];
            if (i > 0 && char.IsUpper(c))
                builder.Append('-');

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static string ConvertSnakeToPascalCase(string snake)
    {
        StringBuilder builder = new(snake.Length);
        bool capitalizeNext = true;
        foreach (char c in snake)
        {
            if (c is '_' or '-')
            {
                capitalizeNext = true;
                continue;
            }

            builder.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
            capitalizeNext = false;
        }

        return builder.ToString();
    }
}
