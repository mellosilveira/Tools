using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.Derivative;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integral;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear;

/// <inheritdoc cref="IQuasiLinearModelCalculator{TInput, TReducedRelaxationFunction}"/>
/// <param name="integration">See reference at <see cref="IIntegration"/>.</param>
/// <param name="derivative">See reference at <see cref="IDerivative"/>.</param>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public abstract class QuasiLinearModelCalculator<TInput, TReducedRelaxationFunction>(
    IIntegration integration,
    IDerivative derivative,
    IMechanicalParameterConverter parameterConverter)
    : MechanicalModelCalculatorBase<TInput>(parameterConverter), IQuasiLinearModelCalculator<TInput, TReducedRelaxationFunction>
    where TInput : QuasiLinearModelInput<TReducedRelaxationFunction>, new()
    where TReducedRelaxationFunction : class
{
    /// <inheritdoc cref="IIntegration"/>
    protected IIntegration Integration { get; } = integration;

    /// <inheritdoc cref="IDerivative"/>
    private readonly IDerivative _derivative = derivative;

    #region Calculate mechanical model's parameters.

    /// <inheritdoc/>
    public double CalculateCreepCompliance(TInput input, double time, double? stress = null)
    {
        throw new NotImplementedException($"The logic for calculate the creep compliance was not implemented on '{GetType().Name}'.");
    }

    /// <inheritdoc/>
    /// <remarks>G(t) · σᵉ(t), where G(t) is the reduced relaxation function and σᵉ(t) is the elastic response.</remarks>
    public double CalculateRelaxationFunction(TInput input, double time, double? strain = null)
    {
        return CalculateReducedRelaxationFunction(input, time) * CalculateElasticResponse(input, time, strain);
    }

    /// <inheritdoc/>
    public override double CalculateForce(TInput input, double time, double? displacement = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
            {
                displacement ??= input.Displacement.InitialValue;
                return CalculateForceWhenDisregardRampTime(input, time, displacement.Value);
            }

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculate the stress while disregarding the ramp time and considering creep was not implemented on '{GetType().Name}'.");
        }

        return Integration.Calculate(
            (integrationTime) => CalculateReducedRelaxationFunction(input, time - integrationTime) * CalculateElasticForceResponseDerivative(input, integrationTime),
            new IntegralInput
            {
                InitialPoint = MathematicConstants.InitialTime,
                FinalPoint = time,
                Step = input.TimeStep
            });
    }

    /// <inheritdoc/>
    public override double CalculateDisplacement(TInput input, double time, double? force = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
            {
                force ??= input.Force.InitialValue;
                double stress = ParameterConverter.CalculateStressFromForce(input.Specimen, force.Value);
                double reducedRelaxationFunction = CalculateReducedRelaxationFunction(input, time);
                return Math.Log(stress / (input.ElasticStressConstant * reducedRelaxationFunction) + 1) / input.ElasticPowerConstant;
            }

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculate the displacement while disregarding the ramp time and considering creep was not implemented on '{GetType().Name}'.");
        }

        throw new NotImplementedException($"The logic for calculate the displacement while considering the ramp time was not implemented on '{GetType().Name}'.");
    }

    /// <inheritdoc/>
    /// <remarks>σ(t) = ∫₀ᵗ G(t-τ) · dσᵉ(τ)/dτ dτ (Projeto Final, Eq. 34).</remarks>
    public override double CalculateStress(TInput input, double time, double? strain = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
            {
                strain ??= input.Strain.InitialValue;
                return CalculateStressWhenDisregardRampTime(input, time, strain.Value);
            }

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculate the stress while disregarding the ramp time and considering creep was not implemented on '{GetType().Name}'.");
        }

        if (time <= MathematicConstants.Tolerance)
            return 0;

        return Integration.Calculate(
            (integrationTime) =>
            {
                double reducedRelaxationFunction = CalculateReducedRelaxationFunction(input, time - integrationTime);
                double elasticResponseDerivative = CalculateElasticResponseDerivative(input, integrationTime);
                return reducedRelaxationFunction * elasticResponseDerivative;
            },
            new IntegralInput
            {
                InitialPoint = MathematicConstants.InitialTime,
                FinalPoint = time,
                Step = input.TimeStep
            });
    }

    /// <inheritdoc/>
    public override double CalculateStrain(TInput input, double time, double? stress = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
            {
                stress ??= input.Stress.InitialValue;
                double reducedRelaxationFunction = CalculateReducedRelaxationFunction(input, time);
                return Math.Log(stress.Value / (input.ElasticStressConstant * reducedRelaxationFunction) + 1) / input.ElasticPowerConstant;
            }

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculate the strain while disregarding the ramp time and considering creep was not implemented on '{GetType().Name}'.");
        }

        throw new NotImplementedException($"The logic for calculate the strain while considering the ramp time was not implemented on '{GetType().Name}'.");
    }

    /// <inheritdoc/>
    /// <remarks>σ(t) = σᵉ(t)·G(0) + ∫₀ᵗ σᵉ(t-τ)·dG(τ)/dτ dτ (Projeto Final, Eq. 35).</remarks>
    [MechanicalModelParameterCalculation(nameof(QuasiLinearModelResult.StressByReducedRelaxationFunctionDerivative), MechanicalRelationship.StressStrain, ViscoelasticEffect.Relaxation)]
    public double CalculateStressByReducedRelaxationFunctionDerivative(TInput input, double time)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                return CalculateStressWhenDisregardRampTime(input, time);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculate the stress while disregarding the ramp time and considering creep was not implemented on '{GetType().Name}'.");
        }

        double elasticResponse = CalculateElasticResponse(input, time);
        double reducedRelaxationFunction = CalculateReducedRelaxationFunction(input, MathematicConstants.InitialTime);
        return elasticResponse * reducedRelaxationFunction
            + Integration.Calculate(
                (integrationTime) =>
                {
                    double integrationElasticResponse = CalculateElasticResponse(input, time - integrationTime);
                    double integrationReducedRelaxationFunctionDerivative = CalculateReducedRelaxationFunctionDerivative(input, integrationTime);
                    return integrationElasticResponse * integrationReducedRelaxationFunctionDerivative;
                },
                new IntegralInput
                {
                    InitialPoint = MathematicConstants.InitialTime,
                    FinalPoint = time,
                    Step = input.TimeStep
                });
    }

    /// <inheritdoc/>
    /// <remarks>σ(t) = d/dt ∫₀ᵗ σᵉ(t-τ)·G(τ) dτ (Projeto Final, Eq. 36).</remarks>
    [MechanicalModelParameterCalculation(nameof(QuasiLinearModelResult.StressByConvolutionDerivative), MechanicalRelationship.StressStrain, ViscoelasticEffect.Relaxation)]
    public double CalculateStressByConvolutionDerivative(TInput input, double time)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                return CalculateStressWhenDisregardRampTime(input, time);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculate the stress while disregarding the ramp time and considering creep was not implemented on '{GetType().Name}'.");
        }

        return _derivative.Calculate(
            (derivativeTime) => Integration.Calculate(
                (integrationTime) =>
                {
                    double elasticResponse = CalculateElasticResponse(input, derivativeTime - integrationTime);
                    double reducedRelaxationFunction = CalculateReducedRelaxationFunction(input, integrationTime);
                    return elasticResponse * reducedRelaxationFunction;
                },
                new IntegralInput
                {
                    InitialPoint = MathematicConstants.InitialTime,
                    FinalPoint = derivativeTime,
                    Step = input.TimeStep
                }),
            input.TimeStep,
            time);
    }

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(QuasiLinearModelResult.ElasticForceResponse), MechanicalRelationship.ForceDisplacement, ViscoelasticEffect.Relaxation)]
    public double CalculateElasticForceResponse(TInput input, double time, double? displacement = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard && displacement is null)
            return input.Force.InitialValue;

        displacement ??= input.Displacement.CalculateValue(time);
        double strain = ParameterConverter.CalculateStrainFromDisplacement(input.Specimen, displacement.Value);
        double elasticResponse = CalculateElasticResponse(input, time, strain);

        return ParameterConverter.CalculateForceFromStress(input.Specimen, elasticResponse);
    }

    /// <inheritdoc/>
    /// <remarks>σᵉ(t) = A · (e^(B·ε(t)) - 1) (Projeto Final, Eq. 37).</remarks>
    [MechanicalModelParameterCalculation(nameof(QuasiLinearModelResult.ElasticResponse), MechanicalRelationship.StressStrain, ViscoelasticEffect.Relaxation)]
    public double CalculateElasticResponse(TInput input, double time, double? strain = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard && strain is null)
            return input.Stress.InitialValue;

        strain ??= input.Strain.CalculateValue(time);
        return input.ElasticStressConstant * (Math.Exp(input.ElasticPowerConstant * strain.Value) - 1);
    }

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(QuasiLinearModelResult.ReducedRelaxationFunction), ViscoelasticEffect.Relaxation)]
    public abstract double CalculateReducedRelaxationFunction(TInput input, double time);

    /// <inheritdoc/>
    protected abstract double CalculateReducedRelaxationFunctionDerivative(TInput input, double time);

    /// <inheritdoc/>
    private double CalculateElasticForceResponseDerivative(TInput input, double time, double? displacement = null, double? displacementDerivative = null)
    {
        // If the ramp time is disregarded, the elastic force response is constant during the time and its derivative is always zero.
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
            return 0;

        displacement ??= input.Displacement.CalculateValue(time);
        displacementDerivative ??= input.Displacement.CalculateDerivative(time);

        double strain = ParameterConverter.CalculateStrainFromDisplacement(input.Specimen, displacement.Value);
        double strainDerivative = ParameterConverter.CalculateStrainDerivativeFromDisplacement(input.Specimen, displacement.Value, displacementDerivative.Value);

        double elasticResponseDerivative = CalculateElasticResponseDerivative(input, time, strain, strainDerivative);
        return ParameterConverter.CalculateForceFromStress(input.Specimen, elasticResponseDerivative);
    }

    /// <remarks>dσᵉ/dt = A · B · e^(B·ε(t)) · dε/dt (Projeto Final, Eq. 39).</remarks>
    private double CalculateElasticResponseDerivative(TInput input, double time, double? strain = null, double? strainDerivative = null)
    {
        // If the ramp time is disregarded, the elastic response is constant during the time and its derivative is always zero.
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
            return 0;

        strain ??= input.Strain.CalculateValue(time);
        strainDerivative ??= input.Strain.CalculateDerivative(time);

        return input.ElasticStressConstant * input.ElasticPowerConstant * strainDerivative.Value * Math.Exp(input.ElasticPowerConstant * strain.Value);
    }

    /// <summary>
    /// Calculates the stress when the ramp time is disregarded.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    private double CalculateStressWhenDisregardRampTime(TInput input, double time, double? strain = null)
    {
        double reducedRelaxationFunction = CalculateReducedRelaxationFunction(input, time);
        double elasticResponse = strain is null ? input.Stress.InitialValue : CalculateElasticResponse(input, time, strain);
        return elasticResponse * reducedRelaxationFunction;
    }

    /// <summary>
    /// Calculates the force when the ramp time is disregarded.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="displacement">Unit: m (meter).</param>
    /// <returns>Unit: N (Newton).</returns>
    private double CalculateForceWhenDisregardRampTime(TInput input, double time, double? displacement)
    {
        double reducedRelaxationFunction = CalculateReducedRelaxationFunction(input, time);
        double elasticForce = displacement is null ? input.Force.InitialValue : CalculateElasticForceResponse(input, time, displacement);
        return elasticForce * reducedRelaxationFunction;
    }

    #endregion
}