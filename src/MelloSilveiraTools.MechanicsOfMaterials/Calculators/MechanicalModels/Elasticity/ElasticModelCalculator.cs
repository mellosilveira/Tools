using MelloSilveiraTools.Mathematics.Converters;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity;

/// <inheritdoc cref="IElasticModelCalculator"/>
/// <summary>
/// Initializes a new instance of <see cref="ElasticModelCalculator"/>.
/// </summary>
/// <param name="parameterConverter"></param>
public class ElasticModelCalculator(IMechanicalParameterConverter parameterConverter) : MechanicalModelCalculatorBase<ElasticModelInput>(parameterConverter), IElasticModelCalculator
{
    #region Calculate mechanical model's parameters.

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(ElasticModelOutput.Stiffness), MechanicalRelationship.ForceDisplacement, ViscoelasticEffect.Relaxation)]
    public double CalculateStiffnessThroughDisplacement(ElasticModelInput input, double time)
    {
        double displacement = input.Displacement!.CalculateValue(time);
        return CalculateStiffness(input, time, displacement: displacement);
    }

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(ElasticModelOutput.Stiffness), MechanicalRelationship.ForceDisplacement, ViscoelasticEffect.Creep)]
    public double CalculateStiffnessThroughForce(ElasticModelInput input, double time)
    {
        double force = input.Force!.CalculateValue(time);
        return CalculateStiffness(input, time, force: force);
    }

    /// <inheritdoc/>
    public override double CalculateForce(ElasticModelInput input, double time, double? displacement = null)
    {
        displacement ??= input.Displacement!.CalculateValue(time);
        return displacement.Value * CalculateStiffness(input, time, displacement: displacement);
    }

    /// <inheritdoc/>
    public override double CalculateDisplacement(ElasticModelInput input, double time, double? force = null)
    {
        force ??= input.Force!.CalculateValue(time);
        return force.Value / CalculateStiffness(input, time, force: force);
    }

    /// <inheritdoc/>
    public override double CalculateStress(ElasticModelInput input, double time, double? strain = null)
    {
        strain ??= input.Strain!.CalculateValue(time);
        return input.ElasticModulus * strain.Value;
    }

    /// <inheritdoc/>
    public override double CalculateStrain(ElasticModelInput input, double time, double? stress = null)
    {
        stress ??= input.Stress!.CalculateValue(time);
        return stress.Value / input.ElasticModulus;
    }

    /// <summary>
    /// Calculates the stiffness.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (seconds).</param>
    /// <param name="displacement">Unit: m (meter).</param>
    /// <param name="force">Unit: N (Newton).</param>
    /// <returns>Unit: N/m (Newton per meter).</returns>
    private double CalculateStiffness(ElasticModelInput input, double time, double? force = null, double? displacement = null)
    {
        var specimen = input.Specimen!;
        if (!specimen.ConsiderLargeDisplacement)
            return specimen.Area * UnitConverter.ConvertMPaToPa(input.ElasticModulus) / specimen.PreLoadLength;

        force ??= CalculateForce(input, time);
        displacement ??= CalculateDisplacement(input, time);
        return force.Value / displacement.Value;
    }

    #endregion
}
