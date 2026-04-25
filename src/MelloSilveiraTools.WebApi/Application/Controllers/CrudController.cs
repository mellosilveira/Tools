using MelloSilveiraTools.Core.Infrastructure.Logger;
using MelloSilveiraTools.Database.Domain.Repositories;
using MelloSilveiraTools.Database.Infrastructure.Database.Models.Entities;
using MelloSilveiraTools.Database.Infrastructure.Database.Models.Filters;
using MelloSilveiraTools.WebApi.Application.Operations;
using MelloSilveiraTools.WebApi.Application.Operations.Add;
using MelloSilveiraTools.WebApi.ExtensionMethods;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MelloSilveiraTools.WebApi.Application.Controllers;

/// <summary>
/// Generic controller exposing standard CRUD and streaming endpoints for an entity and its filter.
/// </summary>
/// <typeparam name="TEntity">The entity type handled by the controller.</typeparam>
/// <typeparam name="TFilter">The filter type used to query the entity.</typeparam>
/// <param name="logger">Logger used to record failures raised while handling CRUD requests.</param>
public abstract class CrudController<TEntity, TFilter>(ILogger logger) : CustomControllerBase(logger)
    where TEntity : EntityBase, new()
    where TFilter : FilterBase
{
    /// <summary>
    /// Human-readable name of the resource, used when building log and error messages.
    /// </summary>
    protected abstract string ResourceName { get; }

    /// <summary>
    /// Creates a new entity.
    /// </summary>
    /// <param name="repository">Repository used to persist the entity.</param>
    /// <param name="entity">Entity instance to be created.</param>
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost]
    public Task<ActionResult<AddResponse>> Create(
        [FromServices] IRepository repository,
        [FromBody] TEntity entity)
        => Create(repository, entity, ResourceName);

    /// <summary>
    /// Retrieves a single entity by its identifier.
    /// </summary>
    /// <param name="repository">Repository used to load the entity.</param>
    /// <param name="id">Identifier of the entity to be returned.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<OperationResponseBase<TEntity>>> Read(
        [FromServices] IRepository repository,
        [FromRoute] long id)
    {
        try
        {
            TEntity? entity = await repository.GetAsync<TEntity>(id).ConfigureAwait(false);
            OperationResponseBase<TEntity> response = new() { StatusCode = HttpStatusCode.OK, Data = entity };
            return response.BuildHttpResponse();
        }
        catch (Exception ex)
        {
            string message = $"Falha ao buscar {ResourceName} pelo identificador.";

            Dictionary<string, object?> logAdditionalData = new() { { "Id", id } };
            Logger.Error(message, ex, logAdditionalData);

            return OperationResponse.CreateInternalServerError(message).BuildHttpResponse();
        }
    }

    /// <summary>
    /// Retrieves a paginated list of entities that match the supplied filter.
    /// </summary>
    /// <param name="repository">Repository used to query the entities.</param>
    /// <param name="filter">Filter criteria applied to the query.</param>
    /// <param name="pagination">Pagination parameters (offset and limit).</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    public async Task<ActionResult<OperationPagedResponseBase<TEntity>>> Read(
        [FromServices] IRepository repository,
        [FromQuery] TFilter filter,
        [FromQuery] Pagination pagination)
    {
        try
        {
            long totalCount = await repository.CountAsync<TEntity, TFilter>(filter).ConfigureAwait(false);
            TEntity[] entities = await repository.GetAsync<TEntity, TFilter>(filter, pagination).ToArrayAsync(HttpContext.RequestAborted).ConfigureAwait(false);

            OperationPagedResponseBase<TEntity> pagedResponse = new()
            {
                StatusCode = HttpStatusCode.OK,
                Data = entities,
                TotalCount = totalCount,
                PageSize = entities.LongLength,
                PageNumber = entities.LongLength > 0 ? (pagination.Offset ?? 0) / entities.LongLength + 1 : 1,
            };
            return pagedResponse.BuildHttpResponse();
        }
        catch (Exception ex)
        {
            string message = $"Falha ao buscar {ResourceName}.";

            Dictionary<string, object?> logAdditionalData = new() { { "Filter", filter } };
            Logger.Error(message, ex, logAdditionalData);

            return OperationResponse.CreateInternalServerError(message).BuildHttpResponse();
        }
    }

    /// <summary>
    /// Updates an existing entity identified by the route parameter.
    /// </summary>
    /// <param name="repository">Repository used to persist changes.</param>
    /// <param name="id">Identifier of the entity to be updated.</param>
    /// <param name="entity">Updated entity payload.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<OperationResponse>> Update(
        [FromServices] IRepository repository,
        [FromRoute] long id,
        [FromBody] TEntity entity)
    {
        try
        {
            TEntity entityToUpdate = entity with { Id = id };
            return await repository.TryUpdateAsync(entityToUpdate).ConfigureAwait(false)
                ? OperationResponse.CreateSuccessCreated().BuildHttpResponse()
                : OperationResponse.CreateNoContent().BuildHttpResponse();
        }
        catch (Exception ex)
        {
            string message = $"Falha ao atualizar um(a) {ResourceName}.";

            Dictionary<string, object?> logAdditionalData = new() { { "Id", id }, { "Entity", entity } };
            Logger.Error(message, ex, logAdditionalData);

            return OperationResponse.CreateInternalServerError(message).BuildHttpResponse();
        }
    }

    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="repository">Repository used to delete the entity.</param>
    /// <param name="id">Identifier of the entity to be deleted.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<OperationResponse>> Delete(
        [FromServices] IRepository repository,
        [FromRoute] long id)
    {
        try
        {
            await repository.DeleteAsync<TEntity>(id).ConfigureAwait(false);
            return OperationResponse.CreateSuccessOk().BuildHttpResponse();
        }
        catch (Exception ex)
        {
            string message = $"Falha ao deletar um(a) {ResourceName}.";

            Dictionary<string, object> logAdditionalData = new() { { "Id", id } };
            Logger.Error(message, ex, logAdditionalData);

            return OperationResponse.CreateInternalServerError(message).BuildHttpResponse();
        }
    }

    /// <summary>
    /// Streams entities that match the supplied filter as newline-delimited JSON (NDJSON).
    /// </summary>
    /// <param name="repository">Repository used to source the entities.</param>
    /// <param name="filter">Filter criteria applied to the query.</param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("stream")]
    public async Task Stream(
        [FromServices] IRepository repository,
        [FromQuery] TFilter filter)
    {
        var entities = repository.GetAsync<TEntity, TFilter>(filter, cancellationToken: HttpContext.RequestAborted);
        await Stream(entities, ResourceName).ConfigureAwait(false);
    }
}
