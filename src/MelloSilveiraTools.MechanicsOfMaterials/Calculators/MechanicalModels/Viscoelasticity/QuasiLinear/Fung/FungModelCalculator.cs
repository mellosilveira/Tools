using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integrals;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;

/// <inheritdoc cref="IFungModelCalculator"/>
/// <param name="integration">See reference at <see cref="IIntegration"/>.</param>
/// <param name="differentiation">See reference at <see cref="IDifferentiation"/>.</param>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public sealed class FungModelCalculator(
    IIntegration integration,
    IDifferentiation differentiation,
    IMechanicalParameterConverter parameterConverter)
    : QuasiLinearModelCalculator<FungConstitutiveParameters, ReducedRelaxationFunction>(integration, differentiation, parameterConverter), IFungModelCalculator
{
    private readonly ExponentialIntegralFunction _exponentialIntegral = new();

    /// <inheritdoc/>
    public double CalculateI(double slowRelaxationTime, double fastRelaxationTime, double timeStep, double time)
    {
        if (time <= MathematicConstants.Tolerance)
        {
            return Math.Log(slowRelaxationTime / fastRelaxationTime);
        }

        return _exponentialIntegral.Calculate(time / slowRelaxationTime, time / fastRelaxationTime);
    }

    /// <inheritdoc/>
    /// <remarks>Formula: G(t) = (1 + C · I(t)) / (1 + C · ln(τ₂/τ₁)) (Projeto Final, Eq. 43). G(0) = 1.</remarks>
    public override double CalculateReducedRelaxationFunction(MechanicalModelInput<FungConstitutiveParameters> input, double time)
    {
        if (time <= MathematicConstants.Tolerance)
            return 1;

        ReducedRelaxationFunction reducedRelaxationFunction = input.ConstitutiveParameters.ReducedRelaxationFunction!;

        // The original equation was simplified, since it has two integrals whose domains could be unified into a single integral (I).
        // For more details, see the "Bibliographies" section in the "README.md" file.
        return (1 + reducedRelaxationFunction.RelaxationStiffness * CalculateI(reducedRelaxationFunction.SlowRelaxationTime, reducedRelaxationFunction.FastRelaxationTime, input.TimeStep, time))
            / CalculateReducedRelaxationFunctionDenominator(reducedRelaxationFunction);
    }

    /// <inheritdoc/>
    /// <remarks>Formula: dG/dt = C · [e^(-t/τ₁) - e^(-t/τ₂)] / [t · (1 + C · ln(τ₂/τ₁))] (Projeto Final, Eq. 45).</remarks>
    protected override double CalculateReducedRelaxationFunctionDerivative(MechanicalModelInput<FungConstitutiveParameters> input, double time)
    {
        ReducedRelaxationFunction reducedRelaxationFunction = input.ConstitutiveParameters.ReducedRelaxationFunction!;
        double denominator = CalculateReducedRelaxationFunctionDenominator(reducedRelaxationFunction);

        if (time <= MathematicConstants.Tolerance)
            return reducedRelaxationFunction.RelaxationStiffness
                * (-1 / reducedRelaxationFunction.FastRelaxationTime + 1 / reducedRelaxationFunction.SlowRelaxationTime)
                / denominator;

        return reducedRelaxationFunction.RelaxationStiffness
            * (Math.Exp(-time / reducedRelaxationFunction.FastRelaxationTime) - Math.Exp(-time / reducedRelaxationFunction.SlowRelaxationTime))
            / (time * denominator);
    }

    private static double CalculateReducedRelaxationFunctionDenominator(ReducedRelaxationFunction reducedRelaxationFunction)
        => 1 + reducedRelaxationFunction.RelaxationStiffness * Math.Log(reducedRelaxationFunction.SlowRelaxationTime / reducedRelaxationFunction.FastRelaxationTime);
}