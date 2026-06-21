using MelloSilveiraTools.Mathematics.NumericalMethods.Integral;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.Linear.Maxwell;

/// <inheritdoc cref="IMaxwellModelCalculator"/>
public sealed class MaxwellModelCalculator(
    IIntegration integration,
    IMechanicalParameterConverter parameterConverter)
    : LinearModelCalculator<MaxwellConstitutiveParameters>(integration, parameterConverter), IMaxwellModelCalculator
{
    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(MaxwellModelOutput.RelaxationTime), ViscoelasticEffect.Relaxation)]
    public double CalculateRelaxationTime(MechanicalModelInput<MaxwellConstitutiveParameters> input) => input.ConstitutiveParameters.Viscosity / input.ConstitutiveParameters.Stiffness;

    /// <inheritdoc/>
    /// <remarks>G(t) = μ · e^(-t/τ) (Projeto Final, Eq. 26).</remarks>
    public override double CalculateRelaxationFunction(MechanicalModelInput<MaxwellConstitutiveParameters> input, double time, double? strain = null)
    {
        return input.ConstitutiveParameters.Stiffness * Math.Exp(-time / CalculateRelaxationTime(input));
    }

    /// <inheritdoc/>
    /// <remarks>J(t) = 1/μ + t/η (Projeto Final, Eq. 23).</remarks>
    public override double CalculateCreepCompliance(MechanicalModelInput<MaxwellConstitutiveParameters> input, double time, double? stress = null)
    {
        return 1 / input.ConstitutiveParameters.Stiffness + time / input.ConstitutiveParameters.Viscosity;
    }
}
