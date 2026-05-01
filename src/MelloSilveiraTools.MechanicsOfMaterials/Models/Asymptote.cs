namespace MelloSilveiraTools.MechanicsOfMaterials.Models;

/// <summary>
/// Contains the asymptote data for analysis.
/// </summary>
/// <param name="Time">Unit: s (second).</param>
/// <param name="Strain">Unit: dimensionless.</param>
/// <param name="Displacement">Unit: m (meter).</param>
/// <param name="Stress">Unit: MPa (Mega-Pascal).</param>
/// <param name="Force">Unit: N (Newton).</param>
public record Asymptote(double Time, double? Strain, double? Displacement, double? Stress, double? Force);
