namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

/// <summary>
/// Contains the input data for Reduced Relaxation Function.
/// </summary>
/// <param name="RelaxationStiffness">Constant C. Unit: dimensionless.</param>
/// <param name="SlowRelaxationTime">Constant tau 1. Unit: s (second).</param>
/// <param name="FastRelaxationTime">Constant tau 2. Unit: s (second).</param>
public sealed record ReducedRelaxationFunction(double RelaxationStiffness, double FastRelaxationTime, double SlowRelaxationTime);
