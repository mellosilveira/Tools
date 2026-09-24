using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

/// <summary>
/// Contains the output for the Modified Superposition Method.
/// </summary>
public sealed record ModifiedSuperpositionMethodOutput : ViscoelasticModelOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModifiedSuperpositionMethodOutput"/> class.
    /// </summary>
    public ModifiedSuperpositionMethodOutput()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModifiedSuperpositionMethodOutput"/> class from a viscoelastic base output.
    /// </summary>
    /// <param name="baseOutput">The base viscoelastic output.</param>
    public ModifiedSuperpositionMethodOutput(ViscoelasticModelOutput baseOutput) : base(baseOutput)
    {
        if (baseOutput is ModifiedSuperpositionMethodOutput msm)
        {
            InitialYoungModulus = msm.InitialYoungModulus;
            StressRelaxationRate = msm.StressRelaxationRate;
        }
    }

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

    /// <inheritdoc />
    public override MechanicalModelOutput CalculateDelta(MechanicalModelOutput initial)
    {
        ModifiedSuperpositionMethodOutput initialMsm = (ModifiedSuperpositionMethodOutput)initial;
        return new ModifiedSuperpositionMethodOutput((ViscoelasticModelOutput)base.CalculateDelta(initial)) with
        {
            InitialYoungModulus = this.InitialYoungModulus - initialMsm.InitialYoungModulus,
            StressRelaxationRate = this.StressRelaxationRate - initialMsm.StressRelaxationRate
        };
    }

    /// <inheritdoc />
    public override MechanicalModelOutput CalculatePercentageDelta(MechanicalModelOutput initial)
    {
        ModifiedSuperpositionMethodOutput initialMsm = (ModifiedSuperpositionMethodOutput)initial;
        return new ModifiedSuperpositionMethodOutput((ViscoelasticModelOutput)base.CalculatePercentageDelta(initial)) with
        {
            InitialYoungModulus = this.InitialYoungModulus.PercentageDifference(initialMsm.InitialYoungModulus),
            StressRelaxationRate = this.StressRelaxationRate.PercentageDifference(initialMsm.StressRelaxationRate)
        };
    }
}
