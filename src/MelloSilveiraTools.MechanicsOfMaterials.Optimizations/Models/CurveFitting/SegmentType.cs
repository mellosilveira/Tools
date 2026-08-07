namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

/// <summary>
/// Represents the phases of the viscoelastic experimental test.
/// </summary>
public enum SegmentType
{
    Unknown,
    Ramp,
    Relaxation,
    Descent,
    Recovery
}