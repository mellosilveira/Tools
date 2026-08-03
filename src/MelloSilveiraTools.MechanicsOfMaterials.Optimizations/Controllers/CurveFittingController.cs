using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.CurveFitting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

[Route("api/v1/[controller]")]
public class CurveFittingController : Controller
{
    //[HttpPost("fit")]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
    //public Task<ActionResult<AddResult>> FitCurve(
    //    [FromBody] CurveFitRequest request)
    //{
    //    if (!ModelState.IsValid)
    //        return BadRequest(ModelState);

    //    // The Application Service handles mapping from DTOs to Domain Records 
    //    // and routes to the correct IModelParameterMapper based on request.ModelType.
    //    var result = curveFittingService.ProcessCurveFit(request);

    //    if (!result.IsSuccessful)
    //    {
    //        return UnprocessableEntity(result);
    //    }

    //    return Ok(result);
    //}
}
