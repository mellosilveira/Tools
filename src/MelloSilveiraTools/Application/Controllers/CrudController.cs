using MelloSilveiraTools.Application.Models;
using MelloSilveiraTools.Application.Operations;
using MelloSilveiraTools.Application.Operations.Add;
using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Database.Models.Entities;
using MelloSilveiraTools.Infrastructure.Database.Models.Filters;
using MelloSilveiraTools.Infrastructure.Database.Repositories;
using MelloSilveiraTools.Infrastructure.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace MelloSilveiraTools.Application.Controllers;

public abstract class CrudController<TEntity, TFilter>(ILogger logger) : Controller
    where TEntity : EntityBase, new()
    where TFilter : FilterBase
{
    protected abstract string ResourceName { get; }

    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost]
    public async Task<ActionResult<AddResponse>> Create(
        [FromServices] IDatabaseRepository repository,
        [FromBody] TEntity entity)
    {
        try
        {
            // TODO: ALTERAR PARA RETORNAR CONFLICT EM CASO DE CONFLITO. ALTERAR TAMBEM RETORNO DO REPOSITORIO.
            long id = await repository.InsertAsync(entity).ConfigureAwait(false);
            return AddResponse.CreateSuccessCreated(id).BuildHttpResponse();
        }
        catch (Exception ex)
        {
            string message = $"Falha ao adicionar um(a) {ResourceName}.";

            Dictionary<string, object?> logAdditionalData = new() { { "Entity", entity } };
            logger.Error(message, ex, logAdditionalData);

            return AddResponse.CreateInternalServerError(message);
        }
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
            logger.Error(message, ex, logAdditionalData);

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
            logger.Error(message, ex, logAdditionalData);

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
            await repository.UpdateAsync(entityToUpdate).ConfigureAwait(false);
            return OperationResponse.CreateSuccessCreated().BuildHttpResponse();
        }
        catch (Exception ex)
        {
            string message = $"Falha ao atualizar um(a) {ResourceName}.";

            Dictionary<string, object> logAdditionalData = new() { { "Id", id }, { "Entity", entity } };
            logger.Error(message, ex, logAdditionalData);

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
            logger.Error(message, ex, logAdditionalData);

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
        // The nosniff directive within the X-Content-Type-Options HTTP response header is a security measure designed to
        // prevent browsers from performing MIME type sniffing.
        // When the X-Content-Type-Options header is set to nosniff, it instructs the browser to:
        // - Strictly adhere to the declared Content-Type header: The browser will not attempt to guess or override the MIME
        //   type based on the content of the response.
        // - Block requests if MIME type mismatch:
        //   - If a resource is requested as a specific type (e.g., a script) but the declared Content-Type does not match
        //   a valid MIME type for that resource (e.g., not a JavaScript MIME type), the browser will block the request.
        Response.Headers.Append(HeaderNames.XContentTypeOptions, ApplicationConstants.NoSniffHeaderValue);
        Response.ContentType = ApplicationConstants.NdjsonContentType;

        try
        {
            var entities = repository.GetAsync<TEntity, TFilter>(filter, cancellationToken: HttpContext.RequestAborted);
            await foreach (var entity in entities)
            {
                await Response.WriteLineAsNdJsonAsync(entity, HttpContext.RequestAborted);
            }

            Response.AppendTrailer(ApplicationConstants.StreamStatusTrailerName, ApplicationConstants.StreamSuccessfullyStatus);
        }
        catch (OperationCanceledException ex)
        {
            logger.Warn("A conexão foi fechada pelo cliente durante o streaming.", ex);
        }
        catch (Exception ex)
        {
            logger.Error($"Falha durante o streaming de {ResourceName}.", ex);
        }
    }
}
