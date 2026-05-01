using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;

/// <inheritdoc cref="IMechanicalModelCalculator{TInput}"/>
/// <param name="parameterConverter">See reference at <see cref="IMechanicalParameterConverter"/>.</param>
public abstract class MechanicalModelCalculatorBase<TInput>(IMechanicalParameterConverter parameterConverter) : IMechanicalModelCalculator<TInput>
    where TInput : MechanicalModelInput, new()
{
    /// <inheritdoc cref="IMechanicalParameterConverter"/>
    public IMechanicalParameterConverter ParameterConverter { get; } = parameterConverter;

    #region Calculate mechanical model's parameters.

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(MechanicalModelResult.Force), MechanicalRelationship.ForceDisplacement, ViscoelasticEffect.Relaxation)]
    public abstract double CalculateForce(TInput input, double time, double? displacement = null);

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(MechanicalModelResult.Displacement), MechanicalRelationship.ForceDisplacement, ViscoelasticEffect.Creep)]
    public abstract double CalculateDisplacement(TInput input, double time, double? force = null);

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(MechanicalModelResult.Stress), MechanicalRelationship.StressStrain, ViscoelasticEffect.Relaxation)]
    public abstract double CalculateStress(TInput input, double time, double? strain = null);

    /// <inheritdoc/>
    [MechanicalModelParameterCalculation(nameof(MechanicalModelResult.Strain), MechanicalRelationship.StressStrain, ViscoelasticEffect.Creep)]
    public abstract double CalculateStrain(TInput input, double time, double? stress = null);

    #endregion
}
