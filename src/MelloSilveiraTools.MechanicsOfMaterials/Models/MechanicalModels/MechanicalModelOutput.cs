using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

/// <summary>
/// Contains the output for a generic mechanical model.
/// </summary>
public record MechanicalModelOutput : TimebasedAnalysisOutput
{
    /// <summary>
    /// Unit: dimensionless.
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.StressStrain)]
    public double? Strain { get; set; }

    /// <summary>
    /// Unit: /s (Per second).
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Relaxation)]
    public double? StrainDerivative { get; set; }

    /// <summary>
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.StressStrain)]
    public double? Stress { get; set; }

    /// <summary>
    /// Unit: MPa/s (Mega-Pascal per second).
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Creep)]
    public double? StressDerivative { get; set; }

    /// <summary>
    /// Unit: m (Meter).
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.ForceDisplacement)]
    public double? Displacement { get; set; }

    /// <summary>
    /// Unit: m/s (Meter per second).
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.ForceDisplacement, ViscoelasticEffect.Relaxation)]
    public double? DisplacementDerivative { get; set; }

    /// <summary>
    /// Unit: N (Newton).
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.ForceDisplacement)]
    public double? Force { get; set; }

    /// <summary>
    /// Unit: N/s (Newton per second).
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.ForceDisplacement, ViscoelasticEffect.Creep)]
    public double? ForceDerivative { get; set; }
}
