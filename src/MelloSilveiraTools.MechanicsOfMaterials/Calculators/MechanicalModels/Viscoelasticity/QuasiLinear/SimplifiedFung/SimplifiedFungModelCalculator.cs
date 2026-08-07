using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integral;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;

/// <inheritdoc cref="ISimplifiedFungModelCalculator"/>
/// <param name="integration">See reference at <see cref="IIntegration"/>.</param>
/// <param name="differentiation">See reference at <see cref="IDifferentiation"/>.</param>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public sealed class SimplifiedFungModelCalculator(
    IIntegration integration,
    IDifferentiation differentiation,
    IMechanicalParameterConverter parameterConverter)
    : QuasiLinearModelCalculator<SimplifiedFungConstitutiveParameters, PronySeries>(integration, differentiation, parameterConverter), ISimplifiedFungModelCalculator
{
    /// <inheritdoc/>
    public override double CalculateReducedRelaxationFunction(MechanicalModelInput<SimplifiedFungConstitutiveParameters> input, double time)
    {
        // Forces time to zero if it falls within the mathematical tolerance. 
        // This prevents floating-point precision errors (residual decimals of the double type) from interfering with the exact t=0 boundary evaluation.
        if (time <= MathematicConstants.Tolerance)
            time = 0;

        return input.ConstitutiveParameters.ReducedRelaxationFunction!.Calculate(time);
    }

    /// <inheritdoc/>
    protected override double CalculateReducedRelaxationFunctionDerivative(MechanicalModelInput<SimplifiedFungConstitutiveParameters> input, double time)
    {
        // Forces time to zero if it falls within the mathematical tolerance. 
        // This prevents floating-point precision errors (residual decimals of the double type) from interfering with the exact t=0 boundary evaluation.
        if (time <= MathematicConstants.Tolerance)
            time = 0;

        return input.ConstitutiveParameters.ReducedRelaxationFunction!.Derivative.Calculate(time);
    }
}