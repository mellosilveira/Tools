namespace MelloSilveiraTools.MechanicsOfMaterials.Models.LoadSharing
{
    /// <summary>
    /// Contains the considerations that can be done for load sharing analysis.
    /// </summary>
    public enum LoadSharingConsideration
    {
        /// <summary>
        /// A force is applied vertically (z axis) in the mechanical system.
        /// It can be used for any creep analysis and for relaxation analysis only when ramp time is disregard.
        /// </summary>
        VerticalForce = 1,

        /// <summary>
        /// A displacement is applied vertically (z axis) in the mechanical system.
        /// It can be used for any relaxation analysis and for creep analysis only when ramp time is disregard.
        /// </summary>
        VerticalDisplacement = 2,
    }
}
