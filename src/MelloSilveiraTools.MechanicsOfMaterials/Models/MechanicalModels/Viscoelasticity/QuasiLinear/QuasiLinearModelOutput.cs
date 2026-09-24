using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

/// <summary>
/// Contains the output for quasi-linear Viscoelastic Model.
/// </summary>
public sealed record QuasiLinearModelOutput : ViscoelasticModelOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuasiLinearModelOutput"/> class.
    /// </summary>
    public QuasiLinearModelOutput()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuasiLinearModelOutput"/> class from a viscoelastic base output.
    /// </summary>
    /// <param name="baseOutput">The base viscoelastic output.</param>
    public QuasiLinearModelOutput(ViscoelasticModelOutput baseOutput) : base(baseOutput)
    {
        if (baseOutput is QuasiLinearModelOutput quasi)
        {
            ReducedRelaxationFunction = quasi.ReducedRelaxationFunction;
            ElasticResponse = quasi.ElasticResponse;
            ElasticForceResponse = quasi.ElasticForceResponse;
            StressByReducedRelaxationFunctionDerivative = quasi.StressByReducedRelaxationFunctionDerivative;
            StressByConvolutionDerivative = quasi.StressByConvolutionDerivative;
        }
    }

    /// <summary>
    /// Unit: dimensionless.
    /// </summary>
    [MechanicalModelParameter(ViscoelasticEffect.Relaxation)]
    public double? ReducedRelaxationFunction { get; set; }

    /// <summary>
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Relaxation)]
    public double? ElasticResponse { get; set; }

    /// <summary>
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.ForceDisplacement, ViscoelasticEffect.Relaxation)]
    public double? ElasticForceResponse { get; set; }

    /// <summary>
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Relaxation)]
    public double? StressByReducedRelaxationFunctionDerivative { get; set; }

    /// <summary>
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    [MechanicalModelParameter(MechanicalBehaviorType.StressStrain, ViscoelasticEffect.Relaxation)]
    public double? StressByConvolutionDerivative { get; set; }

    /// <inheritdoc />
    public override MechanicalModelOutput CalculateDelta(MechanicalModelOutput initial)
    {
        QuasiLinearModelOutput initialQuasi = (QuasiLinearModelOutput)initial;
        return new QuasiLinearModelOutput((ViscoelasticModelOutput)base.CalculateDelta(initial)) with
        {
            ReducedRelaxationFunction = this.ReducedRelaxationFunction - initialQuasi.ReducedRelaxationFunction,
            ElasticResponse = this.ElasticResponse - initialQuasi.ElasticResponse,
            ElasticForceResponse = this.ElasticForceResponse - initialQuasi.ElasticForceResponse,
            StressByReducedRelaxationFunctionDerivative = this.StressByReducedRelaxationFunctionDerivative - initialQuasi.StressByReducedRelaxationFunctionDerivative,
            StressByConvolutionDerivative = this.StressByConvolutionDerivative - initialQuasi.StressByConvolutionDerivative
        };
    }

    /// <inheritdoc />
    public override MechanicalModelOutput CalculatePercentageDelta(MechanicalModelOutput initial)
    {
        QuasiLinearModelOutput initialQuasi = (QuasiLinearModelOutput)initial;
        return new QuasiLinearModelOutput((ViscoelasticModelOutput)base.CalculatePercentageDelta(initial)) with
        {
            ReducedRelaxationFunction = this.ReducedRelaxationFunction.PercentageDifference(initialQuasi.ReducedRelaxationFunction),
            ElasticResponse = this.ElasticResponse.PercentageDifference(initialQuasi.ElasticResponse),
            ElasticForceResponse = this.ElasticForceResponse.PercentageDifference(initialQuasi.ElasticForceResponse),
            StressByReducedRelaxationFunctionDerivative = this.StressByReducedRelaxationFunctionDerivative.PercentageDifference(initialQuasi.StressByReducedRelaxationFunctionDerivative),
            StressByConvolutionDerivative = this.StressByConvolutionDerivative.PercentageDifference(initialQuasi.StressByConvolutionDerivative)
        };
    }
}
