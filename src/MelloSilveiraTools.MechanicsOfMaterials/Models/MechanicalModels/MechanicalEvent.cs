namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

/// <summary>
/// Represents the mechanical event during the experiment.
/// Uses [Flags] to allow combinations of multiple events (e.g., Ramp | Relaxation).
/// </summary>
[Flags]
public enum MechanicalEvent
{
    None = 0,
    Ramp = 1 << 0,
    Relaxation = 1 << 1,
    Descent = 1 << 2,
    Recovery = 1 << 3,

    /// <summary>
    /// Shortcut flag that encompasses all mechanical events.
    /// </summary>
    All = Ramp | Relaxation | Descent | Recovery
}