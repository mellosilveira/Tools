using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear;

/// <summary>
/// A quasi-linear viscoelastic model, initially proposed by Fung. Establish a non-linear stress-strain relation, divided in 
/// two parts: the reduced relaxation function, which depends only on time, and the elastic response, which depends on strain.
/// For more details, see on section "Bibliographies" on file "README.MD".
/// </summary>
/// <typeparam name="TInput">The type of mechanical model input.</typeparam>
/// <typeparam name="TReducedRelaxationFunction">The type of reduced relaxation function.</typeparam>
public interface IQuasiLinearModelCalculator<TInput, TReducedRelaxationFunction> : IViscoelasticModelCalculator<TInput>
    where TInput : QuasiLinearModelInput<TReducedRelaxationFunction>, new()
    where TReducedRelaxationFunction : class
{
    /// <summary>
    /// Calculates the elastic force response, the elastic part of equation while calculating the force-displacement relation.
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="displacement">Unit: m (meter).</param>
    /// <returns>Unit: N (Newton).</returns>
    double CalculateElasticForceResponse(TInput input, double time, double? displacement = null);

    /// <summary>
    /// Calculates the elastic response, the elastic part of equation while calculating the stress-stress relation.
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="strain">
    /// Unit: dimensionless. 
    /// If not informed, this is calculated from the strain parameters on mechanical model's input.
    /// </param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateElasticResponse(TInput input, double time, double? strain = null);

    /// <summary>
    /// Calculates the reduced relaxation function, a normalized function of time, the viscous part of equation. 
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateReducedRelaxationFunction(TInput input, double time);

    /// <summary>
    /// Calculates the stress using a non convencional equation that uses the derivative of reduced relaxation function.
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateStressByReducedRelaxationFunctionDerivative(TInput input, double time);

    /// <summary>
    /// Calculates the stress using a non convencional equation that uses the derivative of convolution.
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateStressByConvolutionDerivative(TInput input, double time);
}
