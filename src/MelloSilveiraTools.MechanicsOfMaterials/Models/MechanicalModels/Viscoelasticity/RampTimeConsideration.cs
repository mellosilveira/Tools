namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity
{
    /// <summary>
    /// Contains the ramp time considerations for analysis.
    /// </summary>
    public enum RampTimeConsideration
    {
        /// <summary>
        /// Consider ramp time at analysis with viscoelastic effect.
        /// </summary>
        ConsiderWithViscoelasticEffect = 1,

        // TODO: This consideration was removed because is necessary to investigate an error while processing the operation.
        ///// <summary>
        ///// Consider ramp time at analysis without viscoelastic effect.
        ///// The viscoelastic effect just begins after the ramp time.
        ///// </summary>
        //ConsiderWithoutViscoelasticEffect = 2,

        /// <summary>
        /// The ramp time is disregarded. It means that the strain is constant at the whole experiment.
        /// </summary>
        Disregard = 3,
    }
}