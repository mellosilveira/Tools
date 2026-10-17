using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Exposes HTTP endpoints for standalone constitutive model curve fitting operations.
/// </summary>
[ApiController]
[Route("api/v1/curve-fitting")]
public class CurveFittingController(FitCurveCommand command) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Result<FitCurveResultData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<FitCurveResultData>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<FitCurveResultData>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<FitCurveResultData>>> FitCurveAsync(
        [FromBody] FitCurveRequest request,
        CancellationToken cancellationToken = default)
    {
        return await command.ExecuteAsync(request with { CancellationToken = cancellationToken }).BuildHttpResponseAsync().ConfigureAwait(false);
    }
}