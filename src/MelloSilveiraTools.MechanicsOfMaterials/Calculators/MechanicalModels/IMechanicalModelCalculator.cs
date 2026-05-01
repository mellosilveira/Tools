using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;

// TODO: usar ref readonly para variáveis de tempo para otimizações de memória.

/// <summary>
/// A generic mechanical model.
/// Establish an approach for stress-strain and force-displacement relationship which approximate from reality.
/// </summary>
/// <typeparam name="TInput">Type of mechanical model's input.</typeparam>
public interface IMechanicalModelCalculator<TInput>
    where TInput : MechanicalModelInput, new()
{
    #region Calculate mechanical model's parameters.

    /// <summary>
    /// Calculates the force, represented by F. 
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// This method is only useful for load sharing analysis.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="displacement">
    /// Unit: m (meter).
    /// If not informed, this is calculated from the displacement parameters on mechanical model's input.
    /// </param>
    /// <returns>Unit: N (Newton).</returns>
    double CalculateForce(TInput input, double time, double? displacement = null);

    /// <summary>
    /// Calculates the displacement, represented by δ. 
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// This method is only useful for load sharing analysis.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="force">
    /// Unit: N (Newton).
    /// If not informed, this is calculated from the force parameters on mechanical model's input.
    /// </param>
    /// <returns>Unit: m (meter).</returns>
    double CalculateDisplacement(TInput input, double time, double? force = null);

    /// <summary>
    /// Calculates the stress, represented by σ. 
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="strain">
    /// Unit: dimensionless. 
    /// If not informed, this is calculated from the strain parameters on mechanical model's input.
    /// </param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateStress(TInput input, double time, double? strain = null);

    /// <summary>
    /// Calculates the strain, represented by ε. 
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="stress">
    /// Unit: MPa (Mega-Pascal).
    /// If not informed, this is calculated from the stress parameters on mechanical model's input.
    /// </param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateStrain(TInput input, double time, double? stress = null);

    #endregion
}
