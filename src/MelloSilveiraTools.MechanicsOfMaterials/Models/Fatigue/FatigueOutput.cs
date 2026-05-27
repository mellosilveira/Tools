namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Fatigue;

/// <summary>
/// Contains the output for fatigue analysis.
/// </summary>
/// <param name="SafetyFactor">The fatigue safety factor based on Modified Goodman. Dimensionless.</param>
/// <param name="StressAmplitude">Unit: MPa (Mega Pascal).</param>
/// <param name="MeanStress">Unit: MPa (Mega Pascal).</param>
/// <param name="EquivalentStress">Unit: MPa (Mega Pascal).</param>
/// <param name="NumberOfCycles">Dimensionless.</param>
public record FatigueOutput(double SafetyFactor, double StressAmplitude, double MeanStress, double EquivalentStress, double NumberOfCycles);
