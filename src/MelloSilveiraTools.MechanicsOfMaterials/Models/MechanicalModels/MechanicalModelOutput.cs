using MelloSilveiraTools.Mathematics.Extensions;
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
    /// Initializes a new instance of the <see cref="MechanicalModelOutput"/> class.
    /// </summary>
    public MechanicalModelOutput() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MechanicalModelOutput"/> class by copying another instance.
    /// </summary>
    /// <param name="original">The original instance to copy from.</param>
    public MechanicalModelOutput(MechanicalModelOutput original) : base(original)
    {
        Time = original.Time;
        Strain = original.Strain;
        StrainDerivative = original.StrainDerivative;
        Stress = original.Stress;
        StressDerivative = original.StressDerivative;
        Displacement = original.Displacement;
        DisplacementDerivative = original.DisplacementDerivative;
        Force = original.Force;
        ForceDerivative = original.ForceDerivative;
    }

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

    /// <summary>
    /// Calculates the absolute variation (this - initial) across all output properties.
    /// </summary>
    /// <param name="initial">The initial output point.</param>
    /// <returns>A new <see cref="MechanicalModelOutput"/> containing the calculated differences.</returns>
    public virtual MechanicalModelOutput CalculateDelta(MechanicalModelOutput initial)
    {
        return new MechanicalModelOutput
        {
            Time = this.Time - initial.Time,
            Strain = this.Strain - initial.Strain,
            StrainDerivative = this.StrainDerivative - initial.StrainDerivative,
            Stress = this.Stress - initial.Stress,
            StressDerivative = this.StressDerivative - initial.StressDerivative,
            Displacement = this.Displacement - initial.Displacement,
            DisplacementDerivative = this.DisplacementDerivative - initial.DisplacementDerivative,
            Force = this.Force - initial.Force,
            ForceDerivative = this.ForceDerivative - initial.ForceDerivative
        };
    }

    /// <summary>
    /// Calculates the percentage variation across all output properties using <see cref="DoubleExtensions.PercentageDifference(double?, double?)"/>.
    /// </summary>
    /// <param name="initial">The initial output point.</param>
    /// <returns>A new <see cref="MechanicalModelOutput"/> containing the calculated percentage differences.</returns>
    public virtual MechanicalModelOutput CalculatePercentageDelta(MechanicalModelOutput initial)
    {
        return new MechanicalModelOutput
        {
            Time = this.Time.PercentageDifference(initial.Time),
            Strain = this.Strain.PercentageDifference(initial.Strain),
            StrainDerivative = this.StrainDerivative.PercentageDifference(initial.StrainDerivative),
            Stress = this.Stress.PercentageDifference(initial.Stress),
            StressDerivative = this.StressDerivative.PercentageDifference(initial.StressDerivative),
            Displacement = this.Displacement.PercentageDifference(initial.Displacement),
            DisplacementDerivative = this.DisplacementDerivative.PercentageDifference(initial.DisplacementDerivative),
            Force = this.Force.PercentageDifference(initial.Force),
            ForceDerivative = this.ForceDerivative.PercentageDifference(initial.ForceDerivative)
        };
    }
}
