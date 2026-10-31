using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;

/// <summary>
/// Defines a calculator for the classic Fung Quasi-Linear Viscoelastic (QLV) model.
/// </summary>
/// <remarks>
/// This calculator implements the exact numerical evaluation of Fung's continuous relaxation spectrum, 
/// avoiding the discrete approximations used in the simplified model.
/// </remarks>
public interface IFungModelCalculator : IQuasiLinearModelCalculator<FungConstitutiveParameters, ReducedRelaxationFunction>;