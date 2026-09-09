using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integrals;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

/// <inheritdoc cref="ISchaperyModelCalculator"/>
/// <param name="integration">See reference at <see cref="IIntegration"/>.</param>
/// <param name="differentiation">See reference at <see cref="IDifferentiation"/>.</param>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public sealed class SchaperyModelCalculator(
    IIntegration integration,
    IDifferentiation differentiation,
    IMechanicalParameterConverter parameterConverter)
    : ISchaperyModelCalculator
{
    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(SchaperyModelOutput.TransientRelaxationFunction), ViscoelasticEffect.Relaxation)]
    public double CalculateTransientRelaxationFunction(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time)
    {
        return input.ConstitutiveParameters.TransientRelaxationFunction!.Calculate(time);

        // TODO: Revisar, porque está errado.
        //if (time <= Constants.Precision)
        //{
        //    if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        //    {
        //        double initialStress = input.Stress?.InitialValue ?? parameterConverter.CalculateStressFromForce(input.Specimen!, input.Force!.InitialValue);
        //        return (initialStress - input.ConstitutiveParameters.He!.Calculate(strain.Value) * input.ConstitutiveParameters.Ge * strain.Value)
        //            / (input.ConstitutiveParameters.H2!.Calculate(strain.Value) * strain.Value);
        //    }
        //
        //    return 0;
        //}
    }

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(SchaperyModelOutput.TransientCreepCompliance), ViscoelasticEffect.Creep)]
    public double CalculateTransientCreepCompliance(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time)
    {
        return input.ConstitutiveParameters.TransientCreepCompliance!.Calculate(time);
    }

    /// <inheritdoc/>
    public double CalculateReducedTimeFunction(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time)
    {
        // For soft tissue, it always returns the time because the shift factor for that case is always 1.
        return time;

        // TODO: Implementar validação que distingue tecidos moles de outros materiais
        // para que este modelo possa ser aplicado em diferentes materiais.

        //if (time <= Constants.Precision)
        //    return 0;
        //
        //return SimpsonRuleIntegration.Calculate(
        //    (integrationTime) => 1 / CalculateStressShiftFactor(input, integrationTime),
        //    new IntegralInput
        //    {
        //        InitialPoint = 0,
        //        FinalPoint = time,
        //        Step = input.TimeStep
        //    });
    }

    /// <inheritdoc/>
    public double CalculateRetardationTimeFunction(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time)
    {
        // For soft tissue, it always returns the time because the shift factor for that case is always 1.
        return time;

        // TODO: Implementar validação que distingue tecidos moles de outros materiais
        // para que este modelo possa ser aplicado em diferentes materiais.

        //if (time <= Constants.Precision)
        //    return 0;
        //
        //return SimpsonRuleIntegration.Calculate(
        //    (integrationTime) => 1 / CalculateTemperatureShiftFactor(input, integrationTime),
        //    new IntegralInput
        //    {
        //        InitialPoint = 0,
        //        FinalPoint = time,
        //        Step = input.TimeStep
        //    });
    }

    /// <inheritdoc/>
    public double CalculateStressShiftFactor(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time)
    {
        throw new NotImplementedException(
            $"The method '{nameof(CalculateStressShiftFactor)}' was not implemented because for soft tissue analysis it is not necessary.");
    }

    /// <inheritdoc/>
    public double CalculateTemperatureShiftFactor(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time)
    {
        throw new NotImplementedException(
            $"The method '{nameof(CalculateTemperatureShiftFactor)}' was not implemented because for soft tissue analysis it is not necessary.");
    }

    /// <inheritdoc/>
    public double CalculateRelaxationFunction(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time, double? strain = null)
    {
        return CalculateTransientRelaxationFunction(input, time) + input.ConstitutiveParameters.Ge;
    }

    /// <inheritdoc/>
    public double CalculateCreepCompliance(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time, double? stress = null)
    {
        return CalculateTransientCreepCompliance(input, time) + input.ConstitutiveParameters.J0;
    }

    /// <inheritdoc/>
    public double CalculateForce(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time, double? displacement = null)
    {
        double stress = 0;

        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            displacement ??= input.Displacement!.InitialValue;
            double strain = parameterConverter.CalculateStrainFromDisplacement(input.Specimen!, displacement.Value);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                stress = CalculateStressWhenDisregardRampTime(input, time, strain);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculate the stress while disregarding the ramp time and considering creep was not implemented on '{GetType().Name}'.");
        }
        else if (input.RampTimeConsideration == RampTimeConsideration.ConsiderWithViscoelasticEffect && time > MathematicConstants.Tolerance)
        {
            displacement ??= input.Displacement!.CalculateValue(time);
            double strain = parameterConverter.CalculateStrainFromDisplacement(input.Specimen!, displacement.Value);

            // σ(ε,t) = hₑ(ε)·Gₑ·ε(t) + h₁(ε)·∫₀ᵗ ΔG(ρ(t)-ρ(τ))·d[h₂(ε)·ε(τ)]/dτ dτ (Projeto Final, Eq. 47).
            stress = input.ConstitutiveParameters.He!.Calculate(strain) * input.ConstitutiveParameters.Ge * strain
                + input.ConstitutiveParameters.H1!.Calculate(strain) * integration.Calculate((integrationTime) =>
                    CalculateTransientRelaxationFunction(input, CalculateReducedTimeFunction(input, time) - CalculateReducedTimeFunction(input, integrationTime))
                    * differentiation.Calculate((derivativeTime) =>
                    {
                        double derivativeDisplacement = input.Displacement!.CalculateValue(derivativeTime);
                        double derivativeStrain = parameterConverter.CalculateStrainFromDisplacement(input.Specimen!, derivativeDisplacement);
                        return input.ConstitutiveParameters.H2!.Calculate(derivativeStrain) * derivativeStrain;
                    },
                    input.TimeStep,
                    integrationTime),
                new IntegralInput
                {
                    InitialPoint = MathematicConstants.InitialTime,
                    Step = input.TimeStep,
                    FinalPoint = time
                });
        }

        return parameterConverter.CalculateForceFromStress(input.Specimen!, stress);
    }

    /// <inheritdoc/>
    public double CalculateDisplacement(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time, double? force = null)
    {
        throw new NotImplementedException($"The method '{nameof(CalculateDisplacement)}' was not implemented for '{GetType().Name}'.");
    }

    /// <inheritdoc/>
    /// <remarks>σ(ε,t) = hₑ(ε)·Gₑ·ε(t) + h₁(ε)·∫₀ᵗ ΔG(ρ(t)-ρ(τ))·d[h₂(ε)·ε(τ)]/dτ dτ (Projeto Final, Eq. 47/48).</remarks>
    public double CalculateStress(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time, double? strain = null)
    {
        strain ??= input.Strain!.CalculateValue(time);

        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                return CalculateStressWhenDisregardRampTime(input, time, strain.Value);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculate the stress while disregarding the ramp time and considering creep was not implemented on '{GetType().Name}'.");
        }

        if (time <= MathematicConstants.Tolerance)
            return 0;

        return input.ConstitutiveParameters.He!.Calculate(strain.Value) * input.ConstitutiveParameters.Ge * strain.Value
            + input.ConstitutiveParameters.H1!.Calculate(strain.Value) * integration.Calculate((integrationTime) =>
                CalculateTransientRelaxationFunction(input, CalculateReducedTimeFunction(input, time) - CalculateReducedTimeFunction(input, integrationTime))
                * differentiation.Calculate((derivativeTime) =>
                {
                    double experimentalStrain = input.Strain!.CalculateValue(derivativeTime);
                    return input.ConstitutiveParameters.H2!.Calculate(experimentalStrain) * experimentalStrain;
                },
                input.TimeStep,
                integrationTime),
            new IntegralInput
            {
                InitialPoint = MathematicConstants.InitialTime,
                Step = input.TimeStep,
                FinalPoint = time
            });
    }

    /// <inheritdoc/>
    public double CalculateStrain(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time, double? stress = null)
    {
        throw new NotImplementedException($"The method '{nameof(CalculateStrain)}' was not implemented for '{GetType().Name}'.");
    }

    /// <summary>
    /// Calculates the stress when the ramp time is disregarded.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    private double CalculateStressWhenDisregardRampTime(MechanicalModelInput<SchaperyConstitutiveParameters> input, double time, double strain)
    {
        return input.ConstitutiveParameters.He!.Calculate(strain) * input.ConstitutiveParameters.Ge * strain
            + input.ConstitutiveParameters.H2!.Calculate(strain) * strain * CalculateTransientRelaxationFunction(input, time);
    }
}