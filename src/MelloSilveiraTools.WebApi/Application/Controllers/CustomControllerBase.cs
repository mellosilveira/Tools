using MelloSilveiraTools.Core.Infrastructure.Logger;
using MelloSilveiraTools.Database.Domain.Repositories;
using MelloSilveiraTools.Database.Infrastructure.Database.Models.Entities;
using MelloSilveiraTools.WebApi.Application.Models;
using MelloSilveiraTools.WebApi.Application.Operations.Add;
using MelloSilveiraTools.WebApi.ExtensionMethods;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace MelloSilveiraTools.WebApi.Application.Controllers;

/// <summary>
/// Base controller providing shared behavior (logging, entity creation, NDJSON streaming) for the project's controllers.
/// </summary>
public class CustomControllerBase(ILogger logger) : Controller
{
    /// <summary>
    /// Logger used to report failures and diagnostics from controller actions.
    /// </summary>
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// Creates a new entity through the provided repository and maps the outcome to an HTTP response.
    /// </summary>
    /// <param name="repository">Repository used to persist the entity.</param>
    /// <param name="entity">Entity instance to be inserted.</param>
    /// <param name="resourceName">Human-readable resource name used when building error messages.</param>
    protected async Task<ActionResult<AddResponse>> Create<TEntity>(IRepository repository, TEntity entity, string resourceName) where TEntity : EntityBase
    {
        try
        {
            // TODO: ALTERAR PARA RETORNAR CONFLICT EM CASO DE CONFLITO. ALTERAR TAMBEM RETORNO DO REPOSITORIO.
            long id = await repository.InsertAsync(entity).ConfigureAwait(false);
            return AddResponse.CreateSuccessCreated(id).BuildHttpResponse();
        }
        catch (Exception ex)
        {
            string message = $"Falha ao adicionar um(a) {resourceName}.";

            Dictionary<string, object?> logAdditionalData = new() { { "Entity", entity } };
            Logger.Error(message, ex, logAdditionalData);

            return AddResponse.CreateInternalServerError(message).BuildHttpResponse();
        }
    }

    /// <summary>
    /// Streams a sequence of entities to the response as newline-delimited JSON (NDJSON).
    /// </summary>
    /// <param name="entities">Asynchronous sequence of items to be streamed.</param>
    /// <param name="resourceName">Human-readable resource name used when reporting streaming failures.</param>
    protected async Task Stream<T>(IAsyncEnumerable<T> entities, string resourceName)
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
            await foreach (T entity in entities)
            {
                await Response.WriteLineAsNdJsonAsync(entity, HttpContext.RequestAborted);
            }

            Response.AppendTrailer(ApplicationConstants.StreamStatusTrailerName, ApplicationConstants.StreamSuccessfullyStatus);
        }
        catch (OperationCanceledException ex)
        {
            Logger.Warn("A conexão foi fechada pelo cliente durante o streaming.", ex);
        }
        catch (Exception ex)
        {
            Logger.Error($"Falha durante o streaming de {resourceName}.", ex);
        }
    }
}
