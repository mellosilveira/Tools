using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using Microsoft.AspNetCore.Http;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.CurveFitting;

public record FitCurveRequest : RequestBase
{
    public required string MechanicalModelName { get; init; }
    public required GenericMechanicalModelInput InitialInput { get; init; }
    public required IFormFile ExperimentalData { get; init; }
    public OptimizationOptionsRequest? Options { get; init; }
}
