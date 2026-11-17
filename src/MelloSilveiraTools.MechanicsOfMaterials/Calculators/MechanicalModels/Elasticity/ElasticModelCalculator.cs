using MelloSilveiraTools.Mathematics.Converters;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity;

/// <inheritdoc cref="IElasticModelCalculator"/>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public class ElasticModelCalculator(IMechanicalParameterConverter parameterConverter) : IElasticModelCalculator
{
    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(ElasticModelOutput.Stiffness), LoadResponseRelationship.ForceDisplacement, ViscoelasticEffect.Relaxation)]
    public double CalculateStiffnessThroughDisplacement(MechanicalModelInput<ElasticConstitutiveParameters> input, double time)
    {
        double displacement = input.Displacement!.CalculateValue(time);
        return CalculateStiffness(input, time, displacement: displacement);
    }

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(ElasticModelOutput.Stiffness), LoadResponseRelationship.ForceDisplacement, ViscoelasticEffect.Creep)]
    public double CalculateStiffnessThroughForce(MechanicalModelInput<ElasticConstitutiveParameters> input, double time)
    {
        double force = input.Force!.CalculateValue(time);
        return CalculateStiffness(input, time, force: force);
    }

    /// <inheritdoc/>
    public double CalculateForce(MechanicalModelInput<ElasticConstitutiveParameters> input, double time, double? displacement = null)
    {
        displacement ??= input.Displacement!.CalculateValue(time);
        double strain = parameterConverter.CalculateStrainFromDisplacement(input.Specimen!, displacement.Value);
        double stress = CalculateStress(input, time, strain);
        return parameterConverter.CalculateForceFromStress(input.Specimen!, stress);
    }

    /// <inheritdoc/>
    public double CalculateDisplacement(MechanicalModelInput<ElasticConstitutiveParameters> input, double time, double? force = null)
    {
        force ??= input.Force!.CalculateValue(time);
        double stress = parameterConverter.CalculateStressFromForce(input.Specimen!, force.Value);
        double strain = CalculateStrain(input, time, stress);
        return parameterConverter.CalculateDisplacementFromStrain(input.Specimen!, strain);
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
        if (displacement is not null && Math.Abs(displacement.Value) > MathematicConstants.Tolerance)
            return CalculateForce(input, time, displacement.Value) / displacement.Value;

        if (force is not null && Math.Abs(force.Value) > MathematicConstants.Tolerance)
            return force.Value / CalculateDisplacement(input, time, force.Value);

        SpecimenParameter specimen = input.Specimen!;
        double length = specimen.ConsiderLargeDisplacement ? specimen.InitialLength : specimen.PreLoadLength;
        return specimen.Area * UnitConverter.ConvertMPaToPa(input.ConstitutiveParameters.YoungModulus) / length;
    }
}
