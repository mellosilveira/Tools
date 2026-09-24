using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Abstract base class for curve fitting pipeline steps targeting mechanical constitutive models.
/// Implements <see cref="IMechanicalModelCurveFitterStep"/> and centralizes common pipeline metadata,
/// lifecycle disposal, and output creation.
/// </summary>
public abstract class MechanicalModelCurveFitterStepBase : IMechanicalModelCurveFitterStep
{
    /// <inheritdoc />
    public virtual string Name => GetType().Name;

    /// <summary>
    /// Gets the name of the mechanical model.
    /// </summary>
    protected abstract string MechanicalModelName { get; }

    /// <summary>
    /// Gets the mechanical behavior type for the model.
    /// </summary>
    protected abstract MechanicalBehaviorType MechanicalBehaviorType { get; }

    /// <summary>
    /// Gets the viscoelastic effect for the model.
    /// </summary>
    protected abstract ViscoelasticEffect ViscoelasticEffect { get; }

    /// <inheritdoc />
    public abstract IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment[] input, CancellationToken cancellationToken = default);

    protected MechanicalModelInput<TConstitutiveParameters> CreateModelInput<TConstitutiveParameters>(CurveSegment segment, TConstitutiveParameters constitutiveParameters, RampTimeConsideration rampTimeConsideration = RampTimeConsideration.Disregard, double? rampTime = null)
        where TConstitutiveParameters : ConstitutiveParameters
    {
        double initialStrain = segment.ExperimentalStrain[0];
        double finalStrain = segment.ExperimentalStrain[^1];
        double initialTime = segment.TimePoints[0];
        double finalTime = segment.TimePoints[^1];

        return new()
        {
            MechanicalModelName = MechanicalModelName,
            AcceptedStrainRange = new AcceptedRange(initialStrain, finalStrain),
            MechanicalBehaviorType = MechanicalBehaviorType,
            ViscoelasticEffect = ViscoelasticEffect,
            RampTimeConsideration = rampTimeConsideration,
            RampTime = rampTime,
            Strain = segment.Type switch
            {
                SegmentType.Ramp => new MechanicalParameter(initialStrain, new PolynomialFunction([0, (finalStrain - initialStrain) / (finalTime - initialTime)])),
                SegmentType.Relaxation => new MechanicalParameter(initialStrain),
                _ => throw new ArgumentOutOfRangeException(nameof(segment.Type), segment.Type, null)
            },
            Stress = new MechanicalParameter(segment.ExperimentalStress[0]),
            TimeStep = (finalTime - initialTime) / segment.TimePoints.Length,
            ConstitutiveParameters = constitutiveParameters
        };
    }

    /// <summary>
    /// Creates a new <see cref="MechanicalModelCurveFitOutput"/> with model metadata and fitting metrics.
    /// </summary>
    /// <param name="constitutiveParameters">The fitted constitutive parameters.</param>
    /// <param name="acceptedRange">The accepted strain range for the fit.</param>
    /// <param name="rampTimeConsideration">The ramp-time consideration mode.</param>
    /// <param name="finalError">The final fitting error.</param>
    /// <param name="iterations">The total iterations taken to converge.</param>
    /// <returns>A populated <see cref="MechanicalModelCurveFitOutput"/> instance.</returns>
    protected MechanicalModelCurveFitOutput CreateCurveFitOutput(ConstitutiveParameters constitutiveParameters, AcceptedRange acceptedRange, RampTimeConsideration rampTimeConsideration, double finalError, int iterations)
        => new(MechanicalModelName, constitutiveParameters, acceptedRange, MechanicalBehaviorType, ViscoelasticEffect, rampTimeConsideration, finalError, iterations);

    /// <summary>
    /// Creates a new <see cref="MechanicalModelCurveFitOutput"/> with model metadata and fitting metrics,
    /// defaulting the ramp time consideration to <see cref="RampTimeConsideration.Disregard"/>.
    /// </summary>
    /// <param name="constitutiveParameters">The fitted constitutive parameters.</param>
    /// <param name="acceptedRange">The accepted strain range for the fit.</param>
    /// <param name="finalError">The final fitting error.</param>
    /// <param name="iterations">The total iterations taken to converge.</param>
    /// <returns>A populated <see cref="MechanicalModelCurveFitOutput"/> instance.</returns>
    protected MechanicalModelCurveFitOutput CreateCurveFitOutput(ConstitutiveParameters constitutiveParameters, AcceptedRange acceptedRange, double finalError, int iterations) 
        => CreateCurveFitOutput(constitutiveParameters, acceptedRange, RampTimeConsideration.Disregard, finalError, iterations);

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
