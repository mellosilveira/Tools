using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.CurveFitting;
using MelloSilveiraTools.WebApi.ExtensionMethods;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Exposes HTTP endpoints for performing curve fitting optimizations on mechanical models.
/// </summary>
[Route("api/V1/curve-fitting")]
[ApiController]
public class CurveFittingController : ControllerBase
{
    /// <summary>
    /// Performs curve fitting optimization using experimental data provided via a CSV file.
    /// </summary>
    /// <param name="command">Command that handles the curve fitting execution.</param>
    /// <param name="request">The form-data request containing the model configurations and the CSV file.</param>
    [Consumes(MediaTypeNames.Multipart.FormData)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("fit")]
    public async Task<ActionResult<Result<FitCurveResultData>>> FitCurve(
        [FromServices] FitCurve command,
        [FromForm] FitCurveRequest request)
        => await command.ExecuteAsync(request).BuildHttpResponseAsync().ConfigureAwait(false);
}