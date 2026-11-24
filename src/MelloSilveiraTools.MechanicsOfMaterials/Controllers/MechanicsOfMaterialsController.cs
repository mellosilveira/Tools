using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.Fatigue;
using MelloSilveiraTools.MechanicsOfMaterials.Commands.MechanicalModels.Get;
using MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;
using MelloSilveiraTools.MechanicsOfMaterials.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Fatigue;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.MechanicsOfMaterials.Controllers;

/// <summary>
/// Exposes <c>MelloSilveiraTools.MechanicsOfMaterials</c> capabilities via MVC controller endpoints,
/// providing full parity with <see cref="MechanicsOfMaterialsEndpoints"/>.
/// </summary>
[ApiController]
[Route("api/v1/mechanics")]
public class MechanicsOfMaterialsController : ControllerBase
{
    /// <summary>
    /// Lists every registered mechanical model resolver with its calculator type
    /// and JSON structures for constitutive parameters and output.
    /// </summary>
    /// <param name="command">Command that resolves registered mechanical model metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registered model metadata wrapped in <see cref="Result{T}"/>.</returns>
    [HttpGet("models")]
    [ProducesResponseType(typeof(ListedResult<MechanicalModelMetadata>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModelsAsync(
        [FromServices] GetMechanicalModelsCommand command,
        CancellationToken cancellationToken = default)
        => await command.ExecuteAsync(cancellationToken).BuildHttpResponseAsync().ConfigureAwait(false);

    /// <summary>
    /// Executes a single deterministic specimen mechanical analysis for any registered or plugin-loaded constitutive model
    /// and streams the generated CSV file for download, deleting the temporary artifact upon completion.
    /// </summary>
    /// <param name="command">Command that validates, runs time-marching integration, and opens the temporary CSV stream.</param>
    /// <param name="request">Unified specimen analysis request with dynamic JSON constitutive parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Downloadable CSV file stream, or a <see cref="Result"/> error payload.</returns>
    [HttpPost("specimen-analysis/execute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteSpecimenAnalysisAsync(
        [FromServices] ExecuteSpecimenAnalysisCommand command,
        [FromBody] ExecuteSpecimenAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<ExecuteSpecimenAnalysisResultData> result = await command.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        return result.Success
            ? File(result.Data!.FileStream, result.Data.ContentType, result.Data.FileName)
            : result.BuildHttpResponse();
    }

    /// <summary>
    /// Runs a complete high-cycle Goodman/Marin fatigue analysis.
    /// </summary>
    /// <param name="calculator">Fatigue calculator service.</param>
    /// <param name="input">Fatigue calculation input parameters.</param>
    /// <returns>Fatigue analysis output wrapped in <see cref="Result{T}"/>.</returns>
    [HttpPost("fatigue/calculate")]
    [ProducesResponseType(typeof(Result<FatigueOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public IActionResult CalculateFatigue(
        [FromServices] IFatigueCalculator calculator,
        [FromBody] FatigueInput input)
        => Result.CreateSuccessOk(calculator.Calculate(input)).BuildHttpResponse();

    /// <summary>
    /// Calculates the Marin-corrected endurance limit Se (MPa).
    /// </summary>
    /// <param name="calculator">Fatigue calculator service.</param>
    /// <param name="input">Fatigue calculation input parameters.</param>
    /// <returns>Modified endurance limit wrapped in <see cref="Result{T}"/>.</returns>
    [HttpPost("fatigue/calculate/endurance-limit")]
    [ProducesResponseType(typeof(Result<double>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public IActionResult CalculateEnduranceLimit(
        [FromServices] IFatigueCalculator calculator,
        [FromBody] FatigueInput input)
        => Result.CreateSuccessOk(calculator.CalculateModifiedFatigueStress(input)).BuildHttpResponse();
}
