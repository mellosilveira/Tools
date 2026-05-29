using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear
{
    /// <summary>
    /// Contains the output for quasi-linear Viscoelastic Model.
    /// </summary>
    public sealed class QuasiLinearModelOutput : ViscoelasticModelOutput
    {
        /// <summary>
        /// Unit: dimensionless.
        /// </summary>
        [MechanicalModelParameter(ViscoelasticEffect.Relaxation)]
        public double? ReducedRelaxationFunction { get; set; }

        /// <summary>
        /// Unit: MPa (Mega-Pascal).
        /// </summary>
        [MechanicalModelParameter(MechanicalRelationship.StressStrain, ViscoelasticEffect.Relaxation)]
        public double? ElasticResponse { get; set; }

        /// <summary>
        /// Unit: MPa (Mega-Pascal).
        /// </summary>
        [MechanicalModelParameter(MechanicalRelationship.ForceDisplacement, ViscoelasticEffect.Relaxation)]
        public double? ElasticForceResponse { get; set; }

        /// <summary>
        /// Unit: MPa (Mega-Pascal).
        /// </summary>
        [MechanicalModelParameter(MechanicalRelationship.StressStrain, ViscoelasticEffect.Relaxation)]
        public double? StressByReducedRelaxationFunctionDerivative { get; set; }

        /// <summary>
        /// Unit: MPa (Mega-Pascal).
        /// </summary>
        [MechanicalModelParameter(MechanicalRelationship.StressStrain, ViscoelasticEffect.Relaxation)]
        public double? StressByConvolutionDerivative { get; set; }
    }
}
