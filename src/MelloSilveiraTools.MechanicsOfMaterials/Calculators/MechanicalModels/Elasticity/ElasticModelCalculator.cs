using MelloSilveiraTools.Mathematics.Converters;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity;

/// <inheritdoc cref="IElasticModelCalculator"/>
public class ElasticModelCalculator : IElasticModelCalculator
{
    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(ElasticModelOutput.Stiffness), MechanicalBehaviorType.ForceDisplacement, ViscoelasticEffect.Relaxation)]
    public double CalculateStiffnessThroughDisplacement(MechanicalModelInput<ElasticConstitutiveParameters> input, double time)
    {
        double displacement = input.Displacement!.CalculateValue(time);
        return CalculateStiffness(input, time, displacement: displacement);
    }

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(ElasticModelOutput.Stiffness), MechanicalBehaviorType.ForceDisplacement, ViscoelasticEffect.Creep)]
    public double CalculateStiffnessThroughForce(MechanicalModelInput<ElasticConstitutiveParameters> input, double time)
    {
        double force = input.Force!.CalculateValue(time);
        return CalculateStiffness(input, time, force: force);
    }

    /// <inheritdoc/>
    public double CalculateForce(MechanicalModelInput<ElasticConstitutiveParameters> input, double time, double? displacement = null)
    {
        displacement ??= input.Displacement!.CalculateValue(time);
        return displacement.Value * CalculateStiffness(input, time, displacement: displacement);
    }

    /// <inheritdoc/>
    public double CalculateDisplacement(MechanicalModelInput<ElasticConstitutiveParameters> input, double time, double? force = null)
    {
        force ??= input.Force!.CalculateValue(time);
        return force.Value / CalculateStiffness(input, time, force: force);
    }

    /// <inheritdoc/>
    public double CalculateStress(MechanicalModelInput<ElasticConstitutiveParameters> input, double time, double? strain = null)
    {
        strain ??= input.Strain!.CalculateValue(time);
        return input.ConstitutiveParameters.YoungModulus * strain.Value;
    }

    /// <inheritdoc/>
    public double CalculateStrain(MechanicalModelInput<ElasticConstitutiveParameters> input, double time, double? stress = null)
    {
        stress ??= input.Stress!.CalculateValue(time);
        return stress.Value / input.ConstitutiveParameters.YoungModulus;
    }

    private double CalculateStiffness(MechanicalModelInput<ElasticConstitutiveParameters> input, double time, double? force = null, double? displacement = null)
    {
        var specimen = input.Specimen!;
        if (!specimen.ConsiderLargeDisplacement)
            return specimen.Area * UnitConverter.ConvertMPaToPa(input.ConstitutiveParameters.YoungModulus) / specimen.PreLoadLength;

        force ??= CalculateForce(input, time);
        displacement ??= CalculateDisplacement(input, time);
        return force.Value / displacement.Value;
    }
}
