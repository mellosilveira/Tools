using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.NumericalMethods.Derivative;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integral;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;

/// <inheritdoc cref="IFungModelCalculator"/>
/// <param name="integration">See reference at <see cref="IIntegration"/>.</param>
/// <param name="derivative">See reference at <see cref="IDerivative"/>.</param>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public sealed class FungModelCalculator(
    IIntegration integration,
    IDerivative derivative,
    IMechanicalParameterConverter parameterConverter)
    : QuasiLinearModelCalculator<FungConstitutiveParameters, ReducedRelaxationFunction>(integration, derivative, parameterConverter), IFungModelCalculator
{
    private const double EquationE1MaximumFinalTime = 11.4;

    /// <inheritdoc/>
    /// <remarks>
    /// Formula: I(t) = ∫[t/τ₂, t/τ₁] e^(-x)/x dx (Projeto Final, Eq. 43).
    /// When t → 0, the integrand e^(-x)/x ≈ 1/x, so I(0) = ln(τ₂/τ₁).
    /// </remarks>
    public double CalculateI(double slowRelaxationTime, double fastRelaxationTime, double timeStep, double time)
    {
        if (time <= MathematicConstants.Tolerance)
            return Math.Log(slowRelaxationTime / fastRelaxationTime);

        double initialTime = time / slowRelaxationTime;
        double step = SetIntegrationStep(initialTime, timeStep);
        double finalTime = Math.Min(time / fastRelaxationTime, EquationE1MaximumFinalTime);

        #region Here a custom Simpson Integration rule is used to adapt to this specific numerical case.

        int numberOfDivisions = Convert.ToInt32((finalTime - initialTime) / step);
        double value = 0;

        for (int index = 0; index <= numberOfDivisions; index++)
        {
            double integrationTime = initialTime + index * step;

            double equationResult = Math.Exp(-integrationTime) / integrationTime;
            if (equationResult == 0)
                continue;

            int factor = (index == 0 || index == numberOfDivisions) ? 1 : (index % 2 != 0 ? 4 : 2);
            value += factor * equationResult * step / 3;

            step = SetIntegrationStep(integrationTime, step);
        }

        #endregion

        return value;
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

    /// <summary>
    /// Sets the dynamic time step for numerical integration based on the current integration time.
    /// </summary>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="timeStep">Unit: s (second).</param>
    /// <returns>Unit: s (second).</returns>
    private static double SetIntegrationStep(double time, double timeStep) => time switch
    {
        <= 0.5 => timeStep > 1e-3 ? 1e-3 : timeStep,
        > 0.5 and <= 1 => timeStep > 1e-2 ? 1e-2 : timeStep,
        _ => timeStep > 1e-1 ? 1e-1 : timeStep,
    };
}