using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

/// <summary>
/// Contains the output for the Modified Superposition Method.
/// </summary>
public sealed class ModifiedSuperpositionMethodOutput : ViscoelasticModelOutput
{
    /// <summary>
    /// Initial Young's modulus.
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Relaxation)]
    public double? InitialYoungModulus { get; set; }

    /// <summary>
    /// Strain-dependent rate of stress relaxation.
    /// Unit: dimensionless.
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Relaxation)]
    public double? StressRelaxationRate { get; set; }
}
