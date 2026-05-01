namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity
{
    /// <summary>
    /// Contains the viscoelasticy effects.
    /// </summary>
    public enum ViscoelasticEffect
    {
        /// <summary>
        /// Relaxation is a gradual reduction in stress with time at constant strain.
        /// </summary>
        Relaxation = 1,

        /// <summary>
        /// Creep is a gradual reduction in strain with time at constant stress.
        /// </summary>
        Creep = 2
    }
}
