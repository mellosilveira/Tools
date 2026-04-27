namespace SoftTissue.Domain.Models;

/// <summary>
/// Contains the results for a generic analysis.
/// </summary>
public abstract class AnalysisResult
{
    /// <summary>
    /// Unit: s (second).
    /// </summary>
    public double Time { get; set; }
}
