using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;

/// <summary>
/// Contains the output for a elastic model.
/// </summary>
public record ElasticModelOutput : MechanicalModelOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticModelOutput"/> class.
    /// </summary>
    public ElasticModelOutput()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticModelOutput"/> class from a base output.
    /// </summary>
    /// <param name="baseOutput">The base output instance.</param>
    public ElasticModelOutput(MechanicalModelOutput baseOutput) : base(baseOutput)
    {
        if (baseOutput is ElasticModelOutput elastic)
        {
            Stiffness = elastic.Stiffness;
        }
    }

    /// <summary>
    /// Unit: N/m (Newton per meter).
    /// </summary>
    [MechanicalModelParameter]
    public double Stiffness { get; set; }

    /// <inheritdoc />
    public override MechanicalModelOutput CalculateDelta(MechanicalModelOutput initial)
    {
        ElasticModelOutput initialElastic = (ElasticModelOutput)initial;
        return new ElasticModelOutput(base.CalculateDelta(initial)) with
        {
            Stiffness = this.Stiffness - initialElastic.Stiffness
        };
    }

    /// <inheritdoc />
    public override MechanicalModelOutput CalculatePercentageDelta(MechanicalModelOutput initial)
    {
        ElasticModelOutput initialElastic = (ElasticModelOutput)initial;
        return new ElasticModelOutput(base.CalculatePercentageDelta(initial)) with
        {
            Stiffness = this.Stiffness.PercentageDifference(initialElastic.Stiffness)
        };
    }
}
