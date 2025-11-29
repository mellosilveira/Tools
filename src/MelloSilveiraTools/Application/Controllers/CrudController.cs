using MelloSilveiraTools.Application.Operations;
using MelloSilveiraTools.Application.Operations.Add;
using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Database.Models.Entities;
using MelloSilveiraTools.Infrastructure.Database.Models.Filters;
using MelloSilveiraTools.Infrastructure.Database.Repositories;
using MelloSilveiraTools.Infrastructure.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MelloSilveiraTools.Application.Controllers;

public abstract class CrudController<TEntity, TFilter>(ILogger logger) : CustomControllerBase(logger)
    where TEntity : EntityBase, new()
    where TFilter : FilterBase
{
    protected abstract string ResourceName { get; }

    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost]
    public Task<ActionResult<AddResponse>> Create(
        [FromServices] IDatabaseRepository repository,
        [FromBody] TEntity entity)
    {
        return Create(repository, entity, ResourceName);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<OperationResponse>> Read(
        [FromServices] IDatabaseRepository repository,
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

            return AddResponse.CreateInternalServerError(message);
        }
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    public async Task<ActionResult<OperationPagedResponseBase<TEntity>>> Read(
        [FromServices] IDatabaseRepository repository,
        [FromQuery] TFilter filter,
        [FromQuery] Pagination pagination)
    {
        try
        {
            long totalCount = await repository.CountAsync<TEntity, TFilter>(filter).ConfigureAwait(false);
            TEntity[] entities = await repository.GetAsync<TEntity, TFilter>(filter, pagination).ToArrayAsync().ConfigureAwait(false);

            OperationPagedResponseBase<TEntity> pagedResponse = new()
            {
                StatusCode = HttpStatusCode.OK,
                Data = entities,
                TotalCount = totalCount,
                PageSize = entities.LongLength,
                PageNumber = (pagination.Offset ?? 0) / entities.LongLength + 1,
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

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<OperationResponse>> Update(
        [FromServices] IDatabaseRepository repository,
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

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<OperationResponse>> Delete(
        [FromServices] IDatabaseRepository repository,
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

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("stream")]
    public async Task Stream(
        [FromServices] IDatabaseRepository repository,
        [FromQuery] TFilter filter)
    {
        var entities = repository.GetAsync<TEntity, TFilter>(filter, cancellationToken: HttpContext.RequestAborted);
        await Stream(entities, ResourceName).ConfigureAwait(false);
    }
}
