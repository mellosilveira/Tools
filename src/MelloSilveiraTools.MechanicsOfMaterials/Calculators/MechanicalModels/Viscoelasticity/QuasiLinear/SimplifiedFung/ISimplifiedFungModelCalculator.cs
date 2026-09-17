using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;

/// <summary>
/// Defines a calculator for the Simplified Fung Quasi-Linear Viscoelastic (QLV) model.
/// </summary>
/// <remarks>
/// It is characterized by using a discrete reduced relaxation function based on a <see cref="PronySeries"/>, 
/// which significantly accelerates numerical integration compared to the classic continuous spectrum model.
/// </remarks>
public interface ISimplifiedFungModelCalculator : IQuasiLinearModelCalculator<SimplifiedFungConstitutiveParameters, PronySeries>;