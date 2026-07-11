namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations
{
    /// <summary>
    /// Maps a parameter (e.g., "N" or "A[0]") to its specific physical boundaries.
    /// </summary>
    public record MorrisParameterBoundary(string ParameterPath, RangeParameters Range);
}