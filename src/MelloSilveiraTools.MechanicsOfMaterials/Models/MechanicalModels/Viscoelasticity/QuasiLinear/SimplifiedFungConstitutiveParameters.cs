using MelloSilveiraTools.Mathematics.Expressions;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

/// <summary>
/// Defines the constitutive parameters for the Simplified Fung Quasi-Linear Viscoelastic (QLV) model.
/// </summary>
/// <remarks>
/// The Simplified Fung model retains the non-linear exponential elastic response (J-curve) of the original theory, 
/// but replaces the complex continuous relaxation spectrum with a discrete <see cref="PronySeries"/>. 
/// This substitution significantly improves the computational efficiency for numerical integration and makes parameter fitting easier for experimental data.
/// </remarks>
public record SimplifiedFungConstitutiveParameters : QuasiLinearConstitutiveParameters<PronySeries>;