namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

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