using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear;

/// <summary>
/// Defines a calculator for the Quasi-Linear Viscoelastic (QLV) model, initially proposed by Y.C. Fung.
/// </summary>
/// <remarks>
/// This formulation establishes a non-linear stress-strain relationship divided into two mathematically independent parts: 
/// the reduced relaxation function (which depends only on time) and the elastic response (which depends only on strain).
/// For more details, see the "Bibliographies" section in the "README.md" file.
/// </remarks>
/// <typeparam name="TConstitutiveParameters">The specific type of QLV constitutive parameters.</typeparam>
/// <typeparam name="TReducedRelaxationFunction">The specific type of the reduced relaxation function formulation.</typeparam>
public interface IQuasiLinearModelCalculator<TConstitutiveParameters, TReducedRelaxationFunction> : IViscoelasticModelCalculator<TConstitutiveParameters>
    where TConstitutiveParameters : QuasiLinearConstitutiveParameters<TReducedRelaxationFunction>
    where TReducedRelaxationFunction : class
{
    /// <summary>
    /// Calculates the elastic force response. 
    /// This represents the purely elastic component of the equation when computing the force-displacement relationship.
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="displacement">Unit: m (meter).</param>
    /// <returns>Unit: N (Newton).</returns>
    double CalculateElasticForceResponse(MechanicalModelInput<TConstitutiveParameters> input, double time, double? displacement = null);

    /// <summary>
    /// Calculates the elastic response ($T^{(e)}$). 
    /// This represents the purely elastic component of the equation when computing the stress-strain relationship.
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="strain">
    /// Unit: dimensionless. 
    /// If not provided, this is calculated from the strain parameters on the mechanical model's input.
    /// </param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateElasticResponse(MechanicalModelInput<TConstitutiveParameters> input, double time, double? strain = null);

    /// <summary>
    /// Calculates the reduced relaxation function ($G(t)$), which is a normalized function of time representing the viscous decay.
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateReducedRelaxationFunction(MechanicalModelInput<TConstitutiveParameters> input, double time);

    /// <summary>
    /// Calculates the stress using a non-conventional numerical approach that relies on the derivative of the reduced relaxation function.
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateStressByReducedRelaxationFunctionDerivative(MechanicalModelInput<TConstitutiveParameters> input, double time);

    /// <summary>
    /// Calculates the stress using a non-conventional numerical approach that relies on the derivative of the convolution integral.
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateStressByConvolutionDerivative(MechanicalModelInput<TConstitutiveParameters> input, double time);
}