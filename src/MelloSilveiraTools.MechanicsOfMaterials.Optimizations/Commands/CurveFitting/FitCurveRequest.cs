using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using Microsoft.AspNetCore.Http;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.CurveFitting;

public record FitCurveRequest : RequestBase
{
    public required string MechanicalModelName { get; init; }
    public required ConstitutiveParameters InitialParameters { get; init; }
    public required IFormFile ExperimentalData { get; init; }
    public OptimizationOptionsRequest? Options { get; init; }

    /// <summary>
    /// Specifies the temporal dependency of the material's response, determining the specific viscoelastic 
    /// phenomena to be integrated (e.g., Creep, Stress Relaxation, Constant Strain Rate).
    /// </summary>
    public ViscoelasticEffect ViscoelasticEffect { get; init; }

    /// <summary>
    /// Defines the integration strategy for the initial loading phase, determining whether the applied boundary 
    /// conditions are evaluated as an instantaneous Heaviside step or a finite-time linear ramp.
    /// </summary>
    public RampTimeConsideration RampTimeConsideration { get; init; }
}
