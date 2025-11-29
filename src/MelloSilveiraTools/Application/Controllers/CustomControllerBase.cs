using MelloSilveiraTools.Application.Models;
using MelloSilveiraTools.Application.Operations.Add;
using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Database.Models.Entities;
using MelloSilveiraTools.Infrastructure.Database.Repositories;
using MelloSilveiraTools.Infrastructure.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace MelloSilveiraTools.Application.Controllers;

public class CustomControllerBase(ILogger logger) : Controller
{
    protected ILogger Logger { get; } = logger;

    protected async Task<ActionResult<AddResponse>> Create<TEntity>(IDatabaseRepository repository, TEntity entity, string resourceName) where TEntity : EntityBase
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
