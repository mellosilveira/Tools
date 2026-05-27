using MelloSilveiraTools.Mathematics.NumericalMethods.Integral;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.Linear.Maxwell;

/// <inheritdoc cref="IMaxwellModelCalculator"/>
/// <param name="integration">See reference at <see cref="IIntegration"/>.</param>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public sealed class MaxwellModelCalculator(
    IIntegration integration,
    IMechanicalParameterConverter parameterConverter)
    : LinearModelCalculator<MaxwellModelInput>(integration, parameterConverter), IMaxwellModelCalculator
{
    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(MaxwellModelOutput.RelaxationTime), ViscoelasticEffect.Relaxation)]
    public double CalculateRelaxationTime(MaxwellModelInput input) => input.Viscosity / input.Stiffness;

    /// <inheritdoc/>
    /// <remarks>G(t) = μ · e^(-t/τ) (Projeto Final, Eq. 26).</remarks>
    public override double CalculateRelaxationFunction(MaxwellModelInput input, double time, double? strain = null)
    {
        return input.Stiffness * Math.Exp(-time / CalculateRelaxationTime(input));
    }

    /// <inheritdoc/>
    /// <remarks>J(t) = 1/μ + t/η (Projeto Final, Eq. 23).</remarks>
    public override double CalculateCreepCompliance(MaxwellModelInput input, double time, double? stress = null)
    {
        return 1 / input.Stiffness + time / input.Viscosity;
    }
}
