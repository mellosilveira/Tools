namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

/// <summary>
/// Defines the parameters for the continuous spectrum reduced relaxation function used in Fung's Quasi-Linear Viscoelastic (QLV) model.
/// </summary>
/// <param name="RelaxationStiffness">
/// The relaxation stiffness constant (often denoted as C). 
/// Represents the amplitude of the viscous effects relative to the elastic response. Unit: dimensionless.
/// </param>
/// <param name="FastRelaxationTime">
/// The fast relaxation time constant (often denoted as τ2). 
/// Governs the short-term viscous response immediately after loading. Unit: s (second).
/// </param>
/// <param name="SlowRelaxationTime">
/// The slow relaxation time constant (often denoted as τ1). 
/// Governs the long-term viscous response, dictating when the material reaches equilibrium. Unit: s (second).
/// </param>
public sealed record ReducedRelaxationFunction(double RelaxationStiffness, double FastRelaxationTime, double SlowRelaxationTime);