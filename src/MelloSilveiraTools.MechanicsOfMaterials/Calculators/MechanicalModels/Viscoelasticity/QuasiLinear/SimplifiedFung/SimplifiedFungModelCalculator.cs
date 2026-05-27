using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.NumericalMethods.Derivative;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integral;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;

/// <inheritdoc cref="ISimplifiedFungModelCalculator"/>
/// <param name="simpsonRuleIntegration">See reference at <see cref="IIntegration"/>.</param>
/// <param name="derivative">See reference at <see cref="IDerivative"/>.</param>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public sealed class SimplifiedFungModelCalculator(
    IIntegration simpsonRuleIntegration,
    IDerivative derivative,
    IMechanicalParameterConverter parameterConverter)
    : QuasiLinearModelCalculator<SimplifiedFungModelInput, PronySeries>(simpsonRuleIntegration, derivative, parameterConverter), ISimplifiedFungModelCalculator
{
    #region Calculate mechanical model's parameters.

    /// <inheritdoc/>
    public override double CalculateReducedRelaxationFunction(SimplifiedFungModelInput input, double time)
    {
        // TODO: explicar que isso é feito para evitar que decimais do double interfiram no valor.
        if (time <= MathematicConstants.Tolerance)
            time = 0;

        return input.ReducedRelaxationFunction!.Calculate(time);
    }

    /// <inheritdoc/>
    protected override double CalculateReducedRelaxationFunctionDerivative(SimplifiedFungModelInput input, double time)
    {
        if (time <= MathematicConstants.Tolerance)
            time = 0;

        return input.ReducedRelaxationFunction!.Derivative.Calculate(time);
    }

    #endregion
}
