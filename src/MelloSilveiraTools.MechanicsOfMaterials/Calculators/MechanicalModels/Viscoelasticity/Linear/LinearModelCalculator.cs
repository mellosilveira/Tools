using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integral;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.Linear;

/// <summary>
/// Defines the foundational calculator for linear viscoelastic models, utilizing hereditary integrals to establish time-dependent stress-strain relationships.
/// For more details, see the "Bibliographies" section in the "README.md" file.
/// </summary>
/// <typeparam name="TConstitutiveParameters">The specific type of constitutive parameters governing the linear viscoelastic model.</typeparam>
/// <param name="integration">See reference at <see cref="IIntegration"/>.</param>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public abstract class LinearModelCalculator<TConstitutiveParameters>(
    IIntegration integration,
    IMechanicalParameterConverter parameterConverter)
    : IViscoelasticModelCalculator<TConstitutiveParameters> where TConstitutiveParameters : ConstitutiveParameters
{
    /// <inheritdoc/>
    public double CalculateForce(MechanicalModelInput<TConstitutiveParameters> input, double time, double? displacement = null)
    {
        double stress = 0;

        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            displacement ??= input.Displacement!.InitialValue;
            var strain = parameterConverter.CalculateStrainFromDisplacement(input.Specimen!, displacement.Value);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                stress = CalculateStressWhenDisregardRampTime(input, time, strain);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic to calculate force while disregarding the ramp time and considering creep is not implemented in '{GetType().Name}'.");
        }
        else if (input.RampTimeConsideration == RampTimeConsideration.ConsiderWithViscoelasticEffect && time > MathematicConstants.Tolerance)
        {
            stress = integration.Calculate(
                (integrationTime) =>
                {
                    (double integralDisplacement, double integralDisplacementDerivative) = input.Displacement!.CalculateValueAndDerivative(integrationTime);
                    double strainDerivative = parameterConverter.CalculateStrainDerivativeFromDisplacement(input.Specimen!, integralDisplacement, integralDisplacementDerivative);
                    return CalculateRelaxationFunction(input, time - integrationTime) * strainDerivative;
                },
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
    public double CalculateDisplacement(MechanicalModelInput<TConstitutiveParameters> input, double time, double? force = null)
    {
        double strain = 0;

        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            force ??= input.Force!.InitialValue;
            var stress = parameterConverter.CalculateStressFromForce(input.Specimen!, force.Value);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                strain = stress / CalculateRelaxationFunction(input, time);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                strain = stress * CalculateCreepCompliance(input, time);
        }
        else if (input.RampTimeConsideration == RampTimeConsideration.ConsiderWithViscoelasticEffect && time > MathematicConstants.Tolerance)
        {
            strain = integration.Calculate(
                (integrationTime) =>
                {
                    (double integralForce, double integralForceDerivative) = input.Force!.CalculateValueAndDerivative(integrationTime);
                    double stressDerivative = parameterConverter.CalculateStressDerivativeFromForce(input.Specimen!, integralForce, integralForceDerivative);
                    return CalculateCreepCompliance(input, time - integrationTime) * stressDerivative;
                },
                new IntegralInput
                {
                    InitialPoint = MathematicConstants.InitialTime,
                    Step = input.TimeStep,
                    FinalPoint = time
                });
        }

        return parameterConverter.CalculateDisplacementFromStrain(input.Specimen!, strain);
    }

    /// <inheritdoc/>
    public double CalculateStress(MechanicalModelInput<TConstitutiveParameters> input, double time, double? strain = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            strain ??= input.Strain!.InitialValue;

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                return CalculateStressWhenDisregardRampTime(input, time, strain.Value);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic to calculate stress while disregarding the ramp time and considering creep is not implemented in '{GetType().Name}'.");
        }

        return integration.Calculate(
            (integrationTime) => CalculateRelaxationFunction(input, time - integrationTime) * input.Strain!.CalculateDerivative(integrationTime),
            new IntegralInput
            {
                InitialPoint = MathematicConstants.InitialTime,
                Step = input.TimeStep,
                FinalPoint = time
            });
    }

    /// <inheritdoc/>
    public double CalculateStrain(MechanicalModelInput<TConstitutiveParameters> input, double time, double? stress = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            stress ??= input.Stress!.InitialValue;

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                return stress.Value / CalculateRelaxationFunction(input, time);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                return stress.Value * CalculateCreepCompliance(input, time);
        }

        return integration.Calculate(
            (integrationTime) => CalculateCreepCompliance(input, time - integrationTime) * input.Stress!.CalculateDerivative(integrationTime),
            new IntegralInput
            {
                InitialPoint = MathematicConstants.InitialTime,
                Step = input.TimeStep,
                FinalPoint = time
            });
    }

    /// <inheritdoc/>
    public abstract double CalculateRelaxationFunction(MechanicalModelInput<TConstitutiveParameters> input, double time, double? strain = null);

    /// <inheritdoc/>
    public abstract double CalculateCreepCompliance(MechanicalModelInput<TConstitutiveParameters> input, double time, double? stress = null);

    private double CalculateStressWhenDisregardRampTime(MechanicalModelInput<TConstitutiveParameters> input, double time, double strain) => CalculateRelaxationFunction(input, time) * strain;
}