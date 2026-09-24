namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Morris
{
    /// <summary>
    /// Represents a single completed simulation point within a trajectory.
    /// </summary>
    public record MorrisPoint(
        IReadOnlyDictionary<string, double> Parameters,
        IReadOnlyDictionary<string, double> Outputs);
}