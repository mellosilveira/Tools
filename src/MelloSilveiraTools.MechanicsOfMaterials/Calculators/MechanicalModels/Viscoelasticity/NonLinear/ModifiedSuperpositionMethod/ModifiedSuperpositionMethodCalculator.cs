using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integrals;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

/// <summary>
/// Implements the calculator for the Modified Superposition Method (MSM).
/// Resolves non-linear viscoelastic responses by evaluating strain-dependent relaxation functions through numerical integration.
/// </summary>
/// <param name="integration">See reference at <see cref="IIntegration"/>.</param>
/// <param name="parameterConverter">The utility used to convert structural metrics into material metrics.</param>
public sealed class ModifiedSuperpositionMethodCalculator(IIntegration integration, IMechanicalParameterConverter parameterConverter) : IModifiedSuperpositionMethodCalculator
{
    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(ModifiedSuperpositionMethodOutput.InitialYoungModulus), MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Relaxation)]
    public double CalculateInitialYoungModulus(MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> input, double strain)
        => input.ConstitutiveParameters.InitialYoungModulus!.Calculate(strain);

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(ModifiedSuperpositionMethodOutput.StressRelaxationRate), MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Relaxation)]
    public double CalculateStressRelaxationRate(MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> input, double strain)
        => input.ConstitutiveParameters.StressRelaxationRate!.Calculate(strain);

    /// <inheritdoc/>
    public double CalculateCreepCompliance(MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> input, double time, double? stress = null)
    {
        throw new NotImplementedException($"The method '{nameof(CalculateCreepCompliance)}' is not implemented for '{GetType().Name}'.");
    }

    /// <inheritdoc/>
    /// <remarks>Formula: G(t,ε) = A(ε) · t^(B(ε)) (Projeto Final, Eq. 55).</remarks>
    public double CalculateRelaxationFunction(MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> input, double time, double? strain = null)
    {
        strain ??= input.Strain!.CalculateValue(time);
        return CalculateInitialYoungModulus(input, strain.Value) * Math.Pow(time, CalculateStressRelaxationRate(input, strain.Value));
    }

    /// <inheritdoc/>
    public double CalculateForce(MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> input, double time, double? displacement = null)
    {
        double stress = 0;

        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            displacement ??= input.Displacement!.InitialValue;
            double strain = parameterConverter.CalculateStrainFromDisplacement(input.Specimen!, displacement.Value);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                stress = CalculateStressWhenDisregardRampTime(input, time, strain);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic to calculate force while disregarding the ramp time and considering creep is not implemented in '{GetType().Name}'.");
        }
        else if (input.RampTimeConsideration == RampTimeConsideration.ConsiderWithViscoelasticEffect && time > MathematicConstants.Tolerance)
        {
            stress = integration.Calculate((integrationTime) =>
            {
                (double integrationDisplacement, double integrationDisplacementDerivative) = input.Displacement!.CalculateValueAndDerivative(integrationTime);
                double currentStrain = parameterConverter.CalculateStrainFromDisplacement(input.Specimen!, integrationDisplacement);
                double strainDerivative = parameterConverter.CalculateStrainDerivativeFromDisplacement(input.Specimen!, integrationDisplacement, integrationDisplacementDerivative);

                return CalculateRelaxationFunction(input, time - integrationTime, currentStrain) * strainDerivative;
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
    public double CalculateDisplacement(MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> input, double time, double? force = null)
    {
        throw new NotImplementedException($"The method '{nameof(CalculateDisplacement)}' is not implemented for '{GetType().Name}'.");
    }

    /// <inheritdoc/>
    /// <remarks>Formula: σ(ε,t) = ∫₀ᵗ G(t-τ, ε(τ)) · dε(τ)/dτ dτ (Projeto Final, Eq. 53).</remarks>
    public double CalculateStress(MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> input, double time, double? strain = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            strain ??= input.Strain!.InitialValue;

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                return CalculateStressWhenDisregardRampTime(input, time, strain.Value);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic to calculate stress while disregarding the ramp time and considering creep is not implemented in '{GetType().Name}'.");
        }

        if (time <= MathematicConstants.Tolerance)
            return 0;

        return integration.Calculate((integrationTime) =>
        {
            (double integrationStrain, double integrationStrainDerivative) = input.Strain!.CalculateValueAndDerivative(integrationTime);
            return CalculateRelaxationFunction(input, time - integrationTime, integrationStrain) * integrationStrainDerivative;
        },
        new IntegralInput
        {
            InitialPoint = MathematicConstants.InitialTime,
            Step = input.TimeStep,
            FinalPoint = time
        });
    }

    /// <inheritdoc/>
    public double CalculateStrain(MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> input, double time, double? stress = null)
    {
        throw new NotImplementedException($"The method '{nameof(CalculateStrain)}' is not implemented for '{GetType().Name}'.");
    }

    /// <summary>
    /// Calculates the stress when the ramp time is disregarded (instantaneous loading).
    /// Formula: σ(εᵢ,t) = G(t, εᵢ) · εᵢ (Projeto Final, Eq. 54).
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    private double CalculateStressWhenDisregardRampTime(MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> input, double time, double strain)
        => CalculateRelaxationFunction(input, time, strain) * strain;
}