using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.NumericalMethods.Derivative;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integral;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung
{
    /// <inheritdoc cref="IFungModelCalculator"/>
    /// <param name="simpsonRuleIntegration">See reference at <see cref="IIntegration"/>.</param>
    /// <param name="derivative">See reference at <see cref="IDerivative"/>.</param>
    /// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
    public sealed class FungModelCalculator(
        IIntegration simpsonRuleIntegration,
        IDerivative derivative,
        IMechanicalParameterConverter parameterConverter)
        : QuasiLinearModelCalculator<FungModelInput, ReducedRelaxationFunction>(simpsonRuleIntegration, derivative, parameterConverter), IFungModelCalculator
    {
        /// <inheritdoc/>
        /// <remarks>
        /// I(t) = ∫[t/τ₂, t/τ₁] e^(-x)/x dx (Projeto Final, Eq. 43).
        /// When t → 0, the integrand e^(-x)/x ≈ 1/x, so I(0) = ln(τ₂/τ₁).
        /// </remarks>
        public double CalculateI(double slowRelaxationTime, double fastRelaxationTime, double timeStep, double time)
        {
            if (time <= MathematicConstants.Tolerance)
                return Math.Log(slowRelaxationTime / fastRelaxationTime);

            double initialTime = time / slowRelaxationTime;
            double step = SetIntegrationStep(initialTime, timeStep);
            double finalTime = Math.Min(time / fastRelaxationTime, MechanicalModelConstants.EquationE1MaximumFinalTime);

            #region Here is used a Simpson Integration rule with some changes to addapt for this case.

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
        /// <remarks>G(t) = (1 + C · I(t)) / (1 + C · ln(τ₂/τ₁)) (Projeto Final, Eq. 43). G(0) = 1.</remarks>
        public override double CalculateReducedRelaxationFunction(FungModelInput input, double time)
        {
            if (time <= MathematicConstants.Tolerance)
                return 1;

            ReducedRelaxationFunction reducedRelaxationFunction = input.ReducedRelaxationFunction!;

            // The original equation was simplified, since it has two integrals which the domains could be unified in an unique integral (I).
            // For more details, see on section "Bibliographies" on file "README.MD".
            return (1 + reducedRelaxationFunction.RelaxationStiffness * CalculateI(reducedRelaxationFunction.SlowRelaxationTime, reducedRelaxationFunction.FastRelaxationTime, input.TimeStep, time))
                / CalculateReducedRelaxationFunctionDenominator(reducedRelaxationFunction);
        }

        /// <inheritdoc/>
        /// <remarks>dG/dt = C · [e^(-t/τ₁) - e^(-t/τ₂)] / [t · (1 + C · ln(τ₂/τ₁))] (Projeto Final, Eq. 45).</remarks>
        protected override double CalculateReducedRelaxationFunctionDerivative(FungModelInput input, double time)
        {
            ReducedRelaxationFunction reducedRelaxationFunction = input.ReducedRelaxationFunction!;
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
        /// Sets the step time for integration.
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
}
