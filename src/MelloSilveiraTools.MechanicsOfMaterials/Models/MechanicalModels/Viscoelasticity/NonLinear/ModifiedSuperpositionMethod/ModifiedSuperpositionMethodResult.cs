using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod
{
    /// <summary>
    /// Contains the results for the Modified Superposition Method.
    /// </summary>
    public sealed class ModifiedSuperpositionMethodResult : ViscoelasticModelResult
    {
        /// <summary>
        /// Initial Young's modulus.
        /// Unit: MPa (Mega-Pascal).
        /// </summary>
        [MechanicalModelParameter(MechanicalRelationship.StressStrain, ViscoelasticEffect.Relaxation)]
        public double? InitialYoungModulus { get; set; }

        /// <summary>
        /// Strain-dependent rate of stress relaxation.
        /// Unit: dimensionless.
        /// </summary>
        [MechanicalModelParameter(MechanicalRelationship.StressStrain, ViscoelasticEffect.Relaxation)]
        public double? StressRelaxationRate { get; set; }
    }
}
