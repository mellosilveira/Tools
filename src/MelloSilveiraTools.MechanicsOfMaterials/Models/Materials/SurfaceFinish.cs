namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Materials;

/// <summary>
/// It contain the surface finish for fatigue analysis.
/// </summary>
public enum SurfaceFinish : int
{
    /// <summary>
    /// Ground/rectified surface. Produces the highest surface factor (least reduction of the endurance limit).
    /// </summary>
    Rectified = 1,

    /// <summary>
    /// Machined or cold-drawn surface.
    /// </summary>
    Machined = 2,

    /// <summary>
    /// Cold-rolled surface (treated identically to machined/cold-drawn in the Marin surface factor).
    /// </summary>
    ColdRolled = 3,

    /// <summary>
    /// Hot-rolled surface. Produces a lower surface factor than machined due to its rougher finish and oxide scale.
    /// </summary>
    HotRolled = 4,

    /// <summary>
    /// As-forged / wrought surface. Produces the lowest surface factor (largest endurance-limit reduction).
    /// </summary>
    Wrought = 5
}
