using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

/// <summary>
/// Contains the output for Schapery's model.
/// </summary>
public sealed record SchaperyModelOutput : ViscoelasticModelOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchaperyModelOutput"/> class.
    /// </summary>
    public SchaperyModelOutput()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchaperyModelOutput"/> class from a viscoelastic base output.
    /// </summary>
    /// <param name="baseOutput">The base viscoelastic output.</param>
    public SchaperyModelOutput(ViscoelasticModelOutput baseOutput) : base(baseOutput)
    {
        if (baseOutput is SchaperyModelOutput schapery)
        {
            TransientCreepCompliance = schapery.TransientCreepCompliance;
            TransientRelaxationFunction = schapery.TransientRelaxationFunction;
        }
    }

    /// <summary>
    /// Unit: /Mpa (per Mega-Pascal).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Creep)]
    public double? TransientCreepCompliance { get; set; }

    /// <summary>
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Relaxation)]
    public double? TransientRelaxationFunction { get; set; }

    /// <inheritdoc />
    public override MechanicalModelOutput CalculateDelta(MechanicalModelOutput initial)
    {
        SchaperyModelOutput initialSchapery = (SchaperyModelOutput)initial;
        return new SchaperyModelOutput((ViscoelasticModelOutput)base.CalculateDelta(initial)) with
        {
            TransientCreepCompliance = this.TransientCreepCompliance - initialSchapery.TransientCreepCompliance,
            TransientRelaxationFunction = this.TransientRelaxationFunction - initialSchapery.TransientRelaxationFunction
        };
    }

    /// <inheritdoc />
    public override MechanicalModelOutput CalculatePercentageDelta(MechanicalModelOutput initial)
    {
        SchaperyModelOutput initialSchapery = (SchaperyModelOutput)initial;
        return new SchaperyModelOutput((ViscoelasticModelOutput)base.CalculatePercentageDelta(initial)) with
        {
            TransientCreepCompliance = this.TransientCreepCompliance.PercentageDifference(initialSchapery.TransientCreepCompliance),
            TransientRelaxationFunction = this.TransientRelaxationFunction.PercentageDifference(initialSchapery.TransientRelaxationFunction)
        };
    }
}
