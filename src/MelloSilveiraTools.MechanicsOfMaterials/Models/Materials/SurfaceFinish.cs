namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Materials
{
    /// <summary>
    /// It contain the surface finish for fatigue analysis.
    /// </summary>
    public enum SurfaceFinish
    {
        /// <summary>
        /// Ground/rectified surface. Produces the highest surface factor (least reduction of the endurance limit).
        /// </summary>
        Rectified,

        /// <summary>
        /// Machined or cold-drawn surface.
        /// </summary>
        Machined,

        /// <summary>
        /// Cold-rolled surface (treated identically to machined/cold-drawn in the Marin surface factor).
        /// </summary>
        ColdRolled,

        /// <summary>
        /// Hot-rolled surface. Produces a lower surface factor than machined due to its rougher finish and oxide scale.
        /// </summary>
        HotRolled,

        /// <summary>
        /// As-forged / wrought surface. Produces the lowest surface factor (largest endurance-limit reduction).
        /// </summary>
        Wrought
    }
}
