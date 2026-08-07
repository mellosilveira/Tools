using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integral;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear;

/// <inheritdoc cref="IQuasiLinearModelCalculator{TConstitutiveParameters, TReducedRelaxationFunction}"/>
/// <param name="integration">See reference at <see cref="IIntegration"/>.</param>
/// <param name="differentiation">See reference at <see cref="IDifferentiation"/>.</param>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public abstract class QuasiLinearModelCalculator<TConstitutiveParameters, TReducedRelaxationFunction>(
    IIntegration integration,
    IDifferentiation differentiation,
    IMechanicalParameterConverter parameterConverter)
    : IQuasiLinearModelCalculator<TConstitutiveParameters, TReducedRelaxationFunction>
    where TConstitutiveParameters : QuasiLinearConstitutiveParameters<TReducedRelaxationFunction>, new()
    where TReducedRelaxationFunction : class
{
    /// <inheritdoc cref="IIntegration"/>
    protected IIntegration Integration { get; } = integration;

    /// <inheritdoc/>
    public double CalculateCreepCompliance(MechanicalModelInput<TConstitutiveParameters> input, double time, double? stress = null)
    {
        throw new NotImplementedException($"The logic for calculating the creep compliance is not implemented in '{GetType().Name}'.");
    }

    /// <inheritdoc/>
    /// <remarks>Formula: G(t) · σᵉ(t), where G(t) is the reduced relaxation function and σᵉ(t) is the elastic response.</remarks>
    public double CalculateRelaxationFunction(MechanicalModelInput<TConstitutiveParameters> input, double time, double? strain = null)
    {
        return CalculateReducedRelaxationFunction(input, time) * CalculateElasticResponse(input, time, strain);
    }

    /// <inheritdoc/>
    public double CalculateForce(MechanicalModelInput<TConstitutiveParameters> input, double time, double? displacement = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
            {
                displacement ??= input.Displacement!.InitialValue;
                return CalculateForceWhenDisregardRampTime(input, time, displacement.Value);
            }

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculating the force while disregarding the ramp time and considering creep is not implemented in '{GetType().Name}'.");
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
    public double CalculateDisplacement(MechanicalModelInput<TConstitutiveParameters> input, double time, double? force = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
            {
                force ??= input.Force!.InitialValue;
                double stress = parameterConverter.CalculateStressFromForce(input.Specimen!, force.Value);
                double reducedRelaxationFunction = CalculateReducedRelaxationFunction(input, time);
                return Math.Log(stress / (input.ConstitutiveParameters.ElasticStressConstant * reducedRelaxationFunction) + 1) / input.ConstitutiveParameters.ElasticPowerConstant;
            }

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculating the displacement while disregarding the ramp time and considering creep is not implemented in '{GetType().Name}'.");
        }

        throw new NotImplementedException($"The logic for calculating the displacement while considering the ramp time is not implemented in '{GetType().Name}'.");
    }

    /// <inheritdoc/>
    /// <remarks>Formula: σ(t) = ∫₀ᵗ G(t-τ) · dσᵉ(τ)/dτ dτ (Projeto Final, Eq. 34).</remarks>
    public double CalculateStress(MechanicalModelInput<TConstitutiveParameters> input, double time, double? strain = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
            {
                strain ??= input.Strain!.InitialValue;
                return CalculateStressWhenDisregardRampTime(input, time, strain.Value);
            }

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculating the stress while disregarding the ramp time and considering creep is not implemented in '{GetType().Name}'.");
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
    public double CalculateStrain(MechanicalModelInput<TConstitutiveParameters> input, double time, double? stress = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
            {
                stress ??= input.Stress!.InitialValue;
                double reducedRelaxationFunction = CalculateReducedRelaxationFunction(input, time);
                return Math.Log(stress.Value / (input.ConstitutiveParameters.ElasticStressConstant * reducedRelaxationFunction) + 1) / input.ConstitutiveParameters.ElasticPowerConstant;
            }

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculating the strain while disregarding the ramp time and considering creep is not implemented in '{GetType().Name}'.");
        }

        throw new NotImplementedException($"The logic for calculating the strain while considering the ramp time is not implemented in '{GetType().Name}'.");
    }

    /// <inheritdoc/>
    /// <remarks>Formula: σ(t) = σᵉ(t)·G(0) + ∫₀ᵗ σᵉ(t-τ)·dG(τ)/dτ dτ (Projeto Final, Eq. 35).</remarks>
    [MechanicalModelParameterCalculation(nameof(QuasiLinearModelOutput.StressByReducedRelaxationFunctionDerivative), MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Relaxation)]
    public double CalculateStressByReducedRelaxationFunctionDerivative(MechanicalModelInput<TConstitutiveParameters> input, double time)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                return CalculateStressWhenDisregardRampTime(input, time);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculating the stress while disregarding the ramp time and considering creep is not implemented in '{GetType().Name}'.");
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
    /// <remarks>Formula: σ(t) = d/dt ∫₀ᵗ σᵉ(t-τ)·G(τ) dτ (Projeto Final, Eq. 36).</remarks>
    [MechanicalModelParameterCalculation(nameof(QuasiLinearModelOutput.StressByConvolutionDerivative), MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Relaxation)]
    public double CalculateStressByConvolutionDerivative(MechanicalModelInput<TConstitutiveParameters> input, double time)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                return CalculateStressWhenDisregardRampTime(input, time);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculating the stress while disregarding the ramp time and considering creep is not implemented in '{GetType().Name}'.");
        }

        return differentiation.Calculate(
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
    [MechanicalModelParameterCalculation(nameof(QuasiLinearModelOutput.ElasticForceResponse), MechanicalBehaviorType.ForceDisplacement, ViscoelasticEffect.Relaxation)]
    public double CalculateElasticForceResponse(MechanicalModelInput<TConstitutiveParameters> input, double time, double? displacement = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard && displacement is null)
            return input.Force!.InitialValue;

        displacement ??= input.Displacement!.CalculateValue(time);
        double strain = parameterConverter.CalculateStrainFromDisplacement(input.Specimen!, displacement.Value);
        double elasticResponse = CalculateElasticResponse(input, time, strain);

        return parameterConverter.CalculateForceFromStress(input.Specimen!, elasticResponse);
    }

    /// <inheritdoc/>
    /// <remarks>Formula: σᵉ(t) = A · (e^(B·ε(t)) - 1) (Projeto Final, Eq. 37).</remarks>
    [MechanicalModelParameterCalculation(nameof(QuasiLinearModelOutput.ElasticResponse), MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Relaxation)]
    public double CalculateElasticResponse(MechanicalModelInput<TConstitutiveParameters> input, double time, double? strain = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard && strain is null)
            return input.Stress!.InitialValue;

        strain ??= input.Strain!.CalculateValue(time);
        return input.ConstitutiveParameters.ElasticStressConstant * (Math.Exp(input.ConstitutiveParameters.ElasticPowerConstant * strain.Value) - 1);
    }

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(QuasiLinearModelOutput.ReducedRelaxationFunction), ViscoelasticEffect.Relaxation)]
    public abstract double CalculateReducedRelaxationFunction(MechanicalModelInput<TConstitutiveParameters> input, double time);

    /// <inheritdoc/>
    protected abstract double CalculateReducedRelaxationFunctionDerivative(MechanicalModelInput<TConstitutiveParameters> input, double time);

    /// <inheritdoc/>
    private double CalculateElasticForceResponseDerivative(MechanicalModelInput<TConstitutiveParameters> input, double time, double? displacement = null, double? displacementDerivative = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
            return 0;

        displacement ??= input.Displacement!.CalculateValue(time);
        displacementDerivative ??= input.Displacement!.CalculateDerivative(time);

        double strain = parameterConverter.CalculateStrainFromDisplacement(input.Specimen!, displacement.Value);
        double strainDerivative = parameterConverter.CalculateStrainDerivativeFromDisplacement(input.Specimen!, displacement.Value, displacementDerivative.Value);

        double elasticResponseDerivative = CalculateElasticResponseDerivative(input, time, strain, strainDerivative);
        return parameterConverter.CalculateForceFromStress(input.Specimen!, elasticResponseDerivative);
    }

    /// <remarks>Formula: dσᵉ/dt = A · B · e^(B·ε(t)) · dε/dt (Projeto Final, Eq. 39).</remarks>
    private static double CalculateElasticResponseDerivative(MechanicalModelInput<TConstitutiveParameters> input, double time, double? strain = null, double? strainDerivative = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
            return 0;

        strain ??= input.Strain!.CalculateValue(time);
        strainDerivative ??= input.Strain!.CalculateDerivative(time);

        return input.ConstitutiveParameters.ElasticStressConstant * input.ConstitutiveParameters.ElasticPowerConstant * strainDerivative.Value * Math.Exp(input.ConstitutiveParameters.ElasticPowerConstant * strain.Value);
    }

    /// <summary>
    /// Calculates the stress when the ramp time is disregarded.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    private double CalculateStressWhenDisregardRampTime(MechanicalModelInput<TConstitutiveParameters> input, double time, double? strain = null)
    {
        double reducedRelaxationFunction = CalculateReducedRelaxationFunction(input, time);
        double elasticResponse = strain is null ? input.Stress!.InitialValue : CalculateElasticResponse(input, time, strain);
        return elasticResponse * reducedRelaxationFunction;
    }

    /// <summary>
    /// Calculates the force when the ramp time is disregarded.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="displacement">Unit: m (meter).</param>
    /// <returns>Unit: N (Newton).</returns>
    private double CalculateForceWhenDisregardRampTime(MechanicalModelInput<TConstitutiveParameters> input, double time, double? displacement)
    {
        double reducedRelaxationFunction = CalculateReducedRelaxationFunction(input, time);
        double elasticForce = displacement is null ? input.Force!.InitialValue : CalculateElasticForceResponse(input, time, displacement);
        return elasticForce * reducedRelaxationFunction;
    }
}