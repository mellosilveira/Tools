namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

/// <summary>
/// Represents the phases of the viscoelastic experimental test.
/// </summary>
public enum SegmentType : int
{
    Unknown = 0,
    Ramp = 1,
    Relaxation = 2,
    Descent = 3,
    Recovery = 4
}