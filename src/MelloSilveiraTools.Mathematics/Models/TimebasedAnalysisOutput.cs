namespace MelloSilveiraTools.MechanicsOfMaterials.Models;

/// <summary>
/// Contains the output for a generic timebased analysis.
/// </summary>
public abstract class TimebasedAnalysisOutput
{
    /// <summary>
    /// Unit: s (second).
    /// </summary>
    public double Time { get; set; }
}
