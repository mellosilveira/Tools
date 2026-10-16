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
public class CurveFittingController : ControllerBase
{
    /// <summary>
    /// Executes curve fitting over one or more experimental curve segments for the specified mechanical model.
    /// </summary>
    /// <param name="command">The curve fitting application command.</param>
    /// <param name="request">The curve fitting request payload.</param>
    /// <returns>An <see cref="ActionResult{TValue}"/> containing the optimized parameter groups and fit metrics.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Result<FitCurveResultData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<FitCurveResultData>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<FitCurveResultData>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<FitCurveResultData>>> FitCurveAsync(
        [FromServices] FitCurve command,
        [FromBody] FitCurveRequest request)
    {
        return await command.ExecuteAsync(request).BuildHttpResponseAsync().ConfigureAwait(false);
    }
}