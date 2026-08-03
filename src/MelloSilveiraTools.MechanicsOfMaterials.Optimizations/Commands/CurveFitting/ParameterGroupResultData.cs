using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.CurveFitting;

public record ParameterGroupResultData
{
    /// <summary>
    /// Optimized constitutive parameters.
    /// </summary>
    public required ConstitutiveParameters OptimizedParameters { get; init; }

    /// <summary>
    /// The strain interval where these mathematical parameters remain physically valid.
    /// </summary>
    public required AcceptedRange AcceptedStrainRange { get; init; }

    /// <summary>
    /// The specific experimental phases where this parameter group applies.
    /// </summary>
    public required MechanicalEvent ValidEvents { get; init; }
}