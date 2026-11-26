using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata;
using MelloSilveiraTools.WebApi.Host.Commands;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.WebApi.Host.Controllers;

/// <summary>
/// Exposes pre-bundled knee ligament experimental datasets stored in the application.
/// </summary>
[ApiController]
[Route("api/v1/samples")]
public class SamplesController : ControllerBase
{
    /// <summary>
    /// Lists all pre-bundled sample dataset files as <see cref="StorageEntryResultData"/> entries.
    /// </summary>
    /// <param name="command">Command that retrieves stored sample dataset entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stored sample entries wrapped in <see cref="ListedResult{T}"/>.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ListedResult<StorageEntryResultData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSamples([FromServices] GetSampleDatasetsCommand command, CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);
}
