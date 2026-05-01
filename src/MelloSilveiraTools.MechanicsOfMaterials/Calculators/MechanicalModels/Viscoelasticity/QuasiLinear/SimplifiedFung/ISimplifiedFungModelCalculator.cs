using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;

/// <summary>
/// Simplified Fung's quasi-linear viscoelastic model.
/// It is characterized by using a simplified Reduced Relaxation Function that is based on <see cref="PronySeries"/>.
/// </summary>
public interface ISimplifiedFungModelCalculator : IQuasiLinearModelCalculator<SimplifiedFungModelInput, PronySeries> { }