using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Exposes HTTP endpoints for processing experimental raw data streams and streaming real-time pipeline progress.
/// File artifact queries and downloads are centralized in <see cref="StoredFilesController"/>.
/// </summary>
[ApiController]
[Route("api/v1/experimental-data")]
public class ExperimentalDataController : ControllerBase
{
    // TODO: PPRIMEIRA RESPOSTA DEVE RETORNAR O IDENTIFICADOR DA ANALISE PARA QUE USUARIO POSSA FAZER POOLING NO BANCO DE DADOS.

    // TODO: DEVE RETORNAR 202 E NOTIFICAR EM ALGUMA FILA
    [HttpPost("process")]
    [ProducesResponseType(typeof(Result<ProcessExperimentalDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ProcessExperimentalDataResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ProcessExperimentalDataResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<ProcessExperimentalDataResponse>>> ProcessAsync(
        [FromServices] ProcessExperimentalDataCommand command,
        [FromForm] ProcessExperimentalDataRequest request,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(request, cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    // TODO: DEVE EXECUTAR EM BACKGROUND E NOTIFICAR O PROGRESSO VIA STREAMING DE DADOS.
    [HttpPost("process/stream")]
    [Produces("application/x-ndjson")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task ProcessStreamAsync(
        [FromServices] ProcessExperimentalDataCommand command,
        [FromForm] ProcessExperimentalDataRequest request,
        CancellationToken cancellationToken = default)
    {
        await PipelineProgressStreamWriter.StreamProgressAsync(
            HttpContext,
            (progress, ct) => command.ExecuteAsync(request with { Progress = progress }, ct),
            cancellationToken).ConfigureAwait(false);
    }
}
