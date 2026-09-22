namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

/// <summary>
/// Defines the constitutive parameters for the classic Fung Quasi-Linear Viscoelastic (QLV) model.
/// </summary>
/// <remarks>
/// This formulation integrates the non-linear exponential elastic response with the continuous spectrum reduced relaxation function. 
/// It is highly accurate for modeling the time-dependent behavior of biological soft tissues over a broad continuous range of relaxation times.
/// </remarks>
public sealed record FungConstitutiveParameters : QuasiLinearConstitutiveParameters<ReducedRelaxationFunction>
{
    public FungConstitutiveParameters() { }

    public FungConstitutiveParameters(double elasticStressConstant, double elasticPowerConstant, ReducedRelaxationFunction reducedRelaxationFunction)
    {
        ElasticStressConstant = elasticStressConstant;
        ElasticPowerConstant = elasticPowerConstant;
        ReducedRelaxationFunction = reducedRelaxationFunction;
    }
}