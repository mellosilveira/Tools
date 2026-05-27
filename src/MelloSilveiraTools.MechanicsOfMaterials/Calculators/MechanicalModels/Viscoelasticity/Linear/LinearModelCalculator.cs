using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integral;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.Linear;

/// <inheritdoc cref="ILinearModelCalculator{TInput}"/>
/// <param name="simpsonRuleIntegration">See reference at <see cref="IIntegration"/>.</param>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public abstract class LinearModelCalculator<TInput>(
    IIntegration simpsonRuleIntegration,
    IMechanicalParameterConverter parameterConverter) :
    MechanicalModelCalculatorBase<TInput>(parameterConverter), ILinearModelCalculator<TInput>
    where TInput : MechanicalModelInput, new()
{
    private readonly IIntegration _integration = simpsonRuleIntegration;

    #region Calculate mechanical model's parameters.

    /// <inheritdoc/>
    public override double CalculateForce(TInput input, double time, double? displacement = null)
    {
        double stress = 0;

        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            displacement ??= input.Displacement!.InitialValue;
            var strain = ParameterConverter.CalculateStrainFromDisplacement(input.Specimen!, displacement.Value);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                stress = CalculateStressWhenDisregardRampTime(input, time, strain);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculate the force while disregarding the ramp time and considering creep was not implemented on '{GetType().Name}'.");
        }
        else if (input.RampTimeConsideration == RampTimeConsideration.ConsiderWithViscoelasticEffect && time > MathematicConstants.Tolerance)
        {
            stress = _integration
                .Calculate((integrationTime) =>
                {
                    (double integralDisplacement, double integralDisplacementDerivative) = input.Displacement!.CalculateValueAndDerivative(integrationTime);
                    double strainDerivative = ParameterConverter.CalculateStrainDerivativeFromDisplacement(input.Specimen!, integralDisplacement, integralDisplacementDerivative);
                    return CalculateRelaxationFunction(input, time - integrationTime) * strainDerivative;
                },
                new IntegralInput
                {
                    InitialPoint = MathematicConstants.InitialTime,
                    Step = input.TimeStep,
                    FinalPoint = time
                });
        }

        return ParameterConverter.CalculateForceFromStress(input.Specimen!, stress);
    }

    /// <inheritdoc/>
    public override double CalculateDisplacement(TInput input, double time, double? force = null)
    {
        double strain = 0;

        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            force ??= input.Force!.InitialValue;
            var stress = ParameterConverter.CalculateStressFromForce(input.Specimen!, force.Value);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                strain = stress / CalculateRelaxationFunction(input, time);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                strain = stress * CalculateCreepCompliance(input, time);
        }
        else if (input.RampTimeConsideration == RampTimeConsideration.ConsiderWithViscoelasticEffect && time > MathematicConstants.Tolerance)
        {
            strain = _integration
                .Calculate((integrationTime) =>
                {
                    (double integralForce, double integralForceDerivative) = input.Force!.CalculateValueAndDerivative(integrationTime);
                    double stressDerivative = ParameterConverter.CalculateStressDerivativeFromForce(input.Specimen!, integralForce, integralForceDerivative);
                    return CalculateCreepCompliance(input, time - integrationTime) * stressDerivative;
                },
                new IntegralInput
                {
                    InitialPoint = MathematicConstants.InitialTime,
                    Step = input.TimeStep,
                    FinalPoint = time
                });
        }

        return ParameterConverter.CalculateDisplacementFromStrain(input.Specimen!, strain);
    }

    /// <inheritdoc/>
    public override double CalculateStress(TInput input, double time, double? strain = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            strain ??= input.Strain!.InitialValue;

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                return CalculateStressWhenDisregardRampTime(input, time, strain.Value);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculate the stress while disregarding the ramp time and considering creep was not implemented on '{GetType().Name}'.");
        }

        return _integration.Calculate(
            (integrationTime) => CalculateRelaxationFunction(input, time - integrationTime) * input.Strain!.CalculateDerivative(integrationTime),
            new IntegralInput
            {
                InitialPoint = MathematicConstants.InitialTime,
                Step = input.TimeStep,
                FinalPoint = time
            });
    }

    /// <inheritdoc/>
    public override double CalculateStrain(TInput input, double time, double? stress = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            stress ??= input.Stress!.InitialValue;

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                return stress.Value / CalculateRelaxationFunction(input, time);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                return stress.Value * CalculateCreepCompliance(input, time);
        }

        return _integration.Calculate(
            (integrationTime) => CalculateCreepCompliance(input, time - integrationTime) * input.Stress!.CalculateDerivative(integrationTime),
            new IntegralInput
            {
                InitialPoint = MathematicConstants.InitialTime,
                Step = input.TimeStep,
                FinalPoint = time
            });
    }

    /// <inheritdoc/>
    public abstract double CalculateRelaxationFunction(TInput input, double time, double? strain = null);

    /// <inheritdoc/>
    public abstract double CalculateCreepCompliance(TInput input, double time, double? stress = null);

    /// <summary>
    /// Calculates the stress when the ramp time is disregarded.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    private double CalculateStressWhenDisregardRampTime(TInput input, double time, double strain)
    {
        return CalculateRelaxationFunction(input, time) * strain;
    }

    #endregion
}
