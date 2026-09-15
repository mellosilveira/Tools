using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Base abstract class for mechanical model curve fitter steps.
/// Implements common boilerplate for constructing inputs, filtering segments, and invoking the curve fitter.
/// </summary>
public abstract class MechanicalModelCurveFitterStepBase<TCalculator, TParameters>(
    TCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) : IMechanicalModelCurveFitterStep
    where TCalculator : IMechanicalModelCalculator<TParameters>
    where TParameters : ConstitutiveParameters
{
    /// <summary>
    /// The mechanical model calculator instance.
    /// </summary>
    protected readonly TCalculator _mechanicalModelCalculator = mechanicalModelCalculator;

    /// <summary>
    /// The curve fitter instance.
    /// </summary>
    protected readonly ICurveFitter _curveFitter = curveFitter;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <summary>
    /// Gets the name of the mechanical model (e.g., nameof(MechanicalModel.Fung)).
    /// </summary>
    protected abstract string MechanicalModelName { get; }

    /// <summary>
    /// Gets the viscoelastic effect to be considered during curve fitting.
    /// </summary>
    protected abstract ViscoelasticEffect ViscoelasticEffect { get; }

    /// <summary>
    /// Gets the ramp time consideration to be used.
    /// </summary>
    protected abstract RampTimeConsideration RampTimeConsideration { get; }

    /// <summary>
    /// Determines whether the given segment type is accepted for curve fitting.
    /// </summary>
    /// <param name="segmentType">The segment type to evaluate.</param>
    /// <returns><c>true</c> if the segment type should be processed; otherwise, <c>false</c>.</returns>
    protected abstract bool IsAcceptedSegment(SegmentType segmentType);

    /// <summary>
    /// Creates the initial constitutive parameters used as a starting point for optimization.
    /// </summary>
    /// <returns>A new instance of <typeparamref name="TParameters"/>.</returns>
    protected abstract TParameters CreateInitialParameters();

    /// <summary>
    /// Maps the constitutive parameters object to a flat array of doubles for the curve fitter.
    /// </summary>
    protected abstract double[] MapParametersToArray(TParameters parameters);

    /// <summary>
    /// Maps the flat array of doubles back to a strongly-typed constitutive parameters object.
    /// </summary>
    protected abstract TParameters MapArrayToParameters(double[] array);

    /// <summary>
    /// Creates the lower bounds array for the optimization parameters.
    /// </summary>
    protected abstract double[] CreateLowerBounds();

    /// <summary>
    /// Creates the upper bounds array for the optimization parameters.
    /// </summary>
    protected abstract double[] CreateUpperBounds();

    /// <inheritdoc />
    public async IAsyncEnumerable<ConstitutiveParameters> ExecuteAsync(CurveSegment[] input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (CurveSegment curve in input)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsAcceptedSegment(curve.Type))
                continue;

            TParameters initialParams = CreateInitialParameters();

            MechanicalModelInput<TParameters> initialMechanicalModelInput = new()
            {
                MechanicalModelName = MechanicalModelName,
                AcceptedStrainRange = new AcceptedRange
                {
                    InitialPoint = curve.ExperimentalStrain[0],
                    FinalPoint = curve.ExperimentalStrain[^1],
                },
                MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                RampTimeConsideration = RampTimeConsideration,
                ViscoelasticEffect = ViscoelasticEffect,
                Strain = new MechanicalParameter(curve.ExperimentalStrain[0]),
                Stress = new MechanicalParameter(curve.ExperimentalStress[0]),
                TimeStep = curve.TimePoints[1] - curve.TimePoints[0],
                ConstitutiveParameters = initialParams,
            };

            CurveFitInput curveFitInput = new()
            {
                TimePoints = curve.TimePoints,
                StrainPoints = curve.ExperimentalStrain,
                StressPoints = curve.ExperimentalStress,
                CalculateStress = (parameters, time, strain) => 
                {
                    MechanicalModelInput<TParameters> currentInput = initialMechanicalModelInput with 
                    { 
                        ConstitutiveParameters = MapArrayToParameters(parameters) 
                    };
                    return _mechanicalModelCalculator.CalculateStress(currentInput, time, strain);
                },
                LowerBounds = CreateLowerBounds(),
                UpperBounds = CreateUpperBounds(),
                EvaluateConstraintsAndPenalties = (parameters) => 0.0,
                InitialParameters = MapParametersToArray(initialParams),
            };

            CurveFitOutput curveFitOutput = _curveFitter.Fit(curveFitInput);
            yield return MapArrayToParameters(curveFitOutput.OptimizedParameters);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // No unmanaged resources to release.
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
