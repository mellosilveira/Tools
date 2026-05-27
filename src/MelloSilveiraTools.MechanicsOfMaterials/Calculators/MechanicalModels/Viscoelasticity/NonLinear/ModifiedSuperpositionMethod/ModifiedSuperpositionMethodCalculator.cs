using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integral;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

/// <inheritdoc cref="IModifiedSuperpositionMethodCalculator"/>
/// <param name="simpsonRuleIntegration">See reference at <see cref="IIntegration"/>.</param>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public sealed class ModifiedSuperpositionMethodCalculator(
    IIntegration simpsonRuleIntegration,
    IMechanicalParameterConverter parameterConverter)
    : MechanicalModelCalculatorBase<ModifiedSuperpositionMethodInput>(parameterConverter), IModifiedSuperpositionMethodCalculator
{
    private readonly IIntegration _integration = simpsonRuleIntegration;

    #region Calculate mechanical model's parameters.

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(ModifiedSuperpositionMethodOutput.InitialYoungModulus), MechanicalRelationship.StressStrain, ViscoelasticEffect.Relaxation)]
    public double CalculateInitialYoungModulus(ModifiedSuperpositionMethodInput input, double strain) => input.InitialYoungModulus!.Calculate(strain);

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(ModifiedSuperpositionMethodOutput.StressRelaxationRate), MechanicalRelationship.StressStrain, ViscoelasticEffect.Relaxation)]
    public double CalculateStressRelaxationRate(ModifiedSuperpositionMethodInput input, double strain) => input.StressRelaxationRate!.Calculate(strain);

    /// <inheritdoc/>
    public double CalculateCreepCompliance(ModifiedSuperpositionMethodInput input, double time, double? stress = null)
    {
        throw new NotImplementedException($"The method '{nameof(CalculateCreepCompliance)}' was not implemented for '{GetType().Name}'.");
    }

    /// <inheritdoc/>
    /// <remarks>G(t,ε) = A(ε) · t^(B(ε)) (Projeto Final, Eq. 55).</remarks>
    public double CalculateRelaxationFunction(ModifiedSuperpositionMethodInput input, double time, double? strain = null)
    {
        strain ??= input.Strain!.CalculateValue(time);
        return CalculateInitialYoungModulus(input, strain.Value) * Math.Pow(time, CalculateStressRelaxationRate(input, strain.Value));
    }

    /// <inheritdoc/>
    public override double CalculateForce(ModifiedSuperpositionMethodInput input, double time, double? displacement = null)
    {
        double stress = 0;

        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            displacement ??= input.Displacement!.InitialValue;
            double strain = ParameterConverter.CalculateStrainFromDisplacement(input.Specimen!, displacement.Value);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                stress = CalculateStressWhenDisregardRampTime(input, time, strain);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculate the stress while disregarding the ramp time and considering creep was not implemented on '{GetType().Name}'.");
        }
        else if (input.RampTimeConsideration == RampTimeConsideration.ConsiderWithViscoelasticEffect && time > MathematicConstants.Tolerance)
        {
            stress = _integration.Calculate((integrationTime) =>
            {
                (double integrationDisplacement, double integrationDisplacementDerivative) = input.Displacement!.CalculateValueAndDerivative(integrationTime);
                double strain = ParameterConverter.CalculateStrainFromDisplacement(input.Specimen!, integrationDisplacement);
                double strainDerivative = ParameterConverter.CalculateStrainDerivativeFromDisplacement(input.Specimen!, integrationDisplacement, integrationDisplacementDerivative);
                return CalculateRelaxationFunction(input, time - integrationTime, strain) * strainDerivative;
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
    public override double CalculateDisplacement(ModifiedSuperpositionMethodInput input, double time, double? force = null)
    {
        throw new NotImplementedException($"The method '{nameof(CalculateDisplacement)}' was not implemented for '{GetType().Name}'.");
    }

    /// <inheritdoc/>
    /// <remarks>σ(ε,t) = ∫₀ᵗ G(t-τ, ε(τ)) · dε(τ)/dτ dτ (Projeto Final, Eq. 53).</remarks>
    public override double CalculateStress(ModifiedSuperpositionMethodInput input, double time, double? strain = null)
    {
        if (input.RampTimeConsideration == RampTimeConsideration.Disregard)
        {
            strain ??= input.Strain!.InitialValue;

            if (input.ViscoelasticEffect == ViscoelasticEffect.Relaxation)
                return CalculateStressWhenDisregardRampTime(input, time, strain.Value);

            if (input.ViscoelasticEffect == ViscoelasticEffect.Creep)
                throw new NotImplementedException($"The logic for calculate the stress while disregarding the ramp time and considering creep was not implemented on '{GetType().Name}'.");
        }

        if (time <= MathematicConstants.Tolerance)
            return 0;

        return _integration.Calculate((integrationTime) =>
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
    public override double CalculateStrain(ModifiedSuperpositionMethodInput input, double time, double? stress = null)
    {
        throw new NotImplementedException($"The method '{nameof(CalculateStrain)}' was not implemented for '{GetType().Name}'.");
    }

    /// <summary>
    /// Calculates the stress when the ramp time is disregarded.
    /// σ(εᵢ,t) = G(t, εᵢ) · εᵢ (Projeto Final, Eq. 54).
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    private double CalculateStressWhenDisregardRampTime(ModifiedSuperpositionMethodInput input, double time, double strain)
    {
        return CalculateRelaxationFunction(input, time, strain) * strain;
    }

    #endregion
}

