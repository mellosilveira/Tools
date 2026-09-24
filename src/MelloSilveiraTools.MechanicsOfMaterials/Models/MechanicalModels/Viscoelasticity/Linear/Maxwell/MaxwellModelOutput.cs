using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;

/// <summary>
/// Contains the output for Maxwell's model.
/// </summary>
public sealed record MaxwellModelOutput : ViscoelasticModelOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaxwellModelOutput"/> class.
    /// </summary>
    public MaxwellModelOutput()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxwellModelOutput"/> class from a viscoelastic base output.
    /// </summary>
    /// <param name="baseOutput">The base viscoelastic output.</param>
    public MaxwellModelOutput(ViscoelasticModelOutput baseOutput) : base(baseOutput)
    {
        if (baseOutput is MaxwellModelOutput maxwell)
        {
            RelaxationTime = maxwell.RelaxationTime;
        }
    }

    /// <summary>
    /// Unit: s (second).
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Relaxation)]
    public double? RelaxationTime { get; set; }

    /// <inheritdoc />
    public override MechanicalModelOutput CalculateDelta(MechanicalModelOutput initial)
    {
        MaxwellModelOutput initialMaxwell = (MaxwellModelOutput)initial;
        return new MaxwellModelOutput((ViscoelasticModelOutput)base.CalculateDelta(initial)) with
        {
            RelaxationTime = this.RelaxationTime - initialMaxwell.RelaxationTime
        };
    }

    /// <inheritdoc />
    public override MechanicalModelOutput CalculatePercentageDelta(MechanicalModelOutput initial)
    {
        MaxwellModelOutput initialMaxwell = (MaxwellModelOutput)initial;
        return new MaxwellModelOutput((ViscoelasticModelOutput)base.CalculatePercentageDelta(initial)) with
        {
            RelaxationTime = this.RelaxationTime.PercentageDifference(initialMaxwell.RelaxationTime)
        };
    }
}
