using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

/// <summary>
/// Contains the output for a generic viscoelastic model.
/// </summary>
public record ViscoelasticModelOutput : MechanicalModelOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ViscoelasticModelOutput"/> class.
    /// </summary>
    public ViscoelasticModelOutput()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViscoelasticModelOutput"/> class from a base output.
    /// </summary>
    /// <param name="baseOutput">The base output instance.</param>
    public ViscoelasticModelOutput(MechanicalModelOutput baseOutput) : base(baseOutput)
    {
        if (baseOutput is ViscoelasticModelOutput visco)
        {
            CreepCompliance = visco.CreepCompliance;
            RelaxationFunction = visco.RelaxationFunction;
        }
    }

    /// <summary>
    /// Unit: /MPa (per Mega-Pascal) or m/N (meter per Newton).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Creep)]
    public double? CreepCompliance { get; set; }

    /// <summary>
    /// Unit: MPa (Mega-Pascal) or N/m (Newton per meter).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Relaxation)]
    public double? RelaxationFunction { get; set; }

    /// <inheritdoc />
    public override MechanicalModelOutput CalculateDelta(MechanicalModelOutput initial)
    {
        ViscoelasticModelOutput initialVisco = (ViscoelasticModelOutput)initial;
        return new ViscoelasticModelOutput(base.CalculateDelta(initial)) with
        {
            CreepCompliance = this.CreepCompliance - initialVisco.CreepCompliance,
            RelaxationFunction = this.RelaxationFunction - initialVisco.RelaxationFunction
        };
    }

    /// <inheritdoc />
    public override MechanicalModelOutput CalculatePercentageDelta(MechanicalModelOutput initial)
    {
        ViscoelasticModelOutput initialVisco = (ViscoelasticModelOutput)initial;
        return new ViscoelasticModelOutput(base.CalculatePercentageDelta(initial)) with
        {
            CreepCompliance = this.CreepCompliance.PercentageDifference(initialVisco.CreepCompliance),
            RelaxationFunction = this.RelaxationFunction.PercentageDifference(initialVisco.RelaxationFunction)
        };
    }
}
