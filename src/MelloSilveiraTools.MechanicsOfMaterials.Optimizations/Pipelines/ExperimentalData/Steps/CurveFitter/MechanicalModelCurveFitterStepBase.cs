using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

public record MechanicalModelCurveFitOutput(ConstitutiveParameters ConstitutiveParameters, double FinalError, int Iterations);

/// <summary>
/// Thread-safe. Base abstract class for mechanical model curve fitter steps.
/// Implements common boilerplate for constructing inputs, filtering segments, and invoking the curve fitter.
/// </summary>
public abstract class MechanicalModelCurveFitterStepBase<TCalculator, TConstitutiveParameters>(
    TCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) : IMechanicalModelCurveFitterStep
    where TCalculator : IMechanicalModelCalculator<TConstitutiveParameters>
    where TConstitutiveParameters : ConstitutiveParameters
{
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

    protected TCalculator MechanicalModelCalculator { get; } = mechanicalModelCalculator;

    protected ICurveFitter CurveFitter { get; } = curveFitter;

    /// <inheritdoc />
    public virtual async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment[] input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (CurveSegment curveSegment in input)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsAcceptedSegment(curveSegment.Type))
                continue;

            CurveFitInput curveFitInput = new()
            {
                TimePoints = curveSegment.TimePoints,
                StrainPoints = curveSegment.ExperimentalStrain,
                StressPoints = curveSegment.ExperimentalStress,
                CalculateStress = (parameters, time, strain) =>
                {
                    MechanicalModelInput<TConstitutiveParameters> currentInput = new()
                    {
                        MechanicalModelName = MechanicalModelName,
                        AcceptedStrainRange = new AcceptedRange
                        {
                            InitialPoint = curveSegment.ExperimentalStrain[0],
                            FinalPoint = curveSegment.ExperimentalStrain[^1],
                        },
                        MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                        RampTimeConsideration = RampTimeConsideration,
                        ViscoelasticEffect = ViscoelasticEffect,
                        Strain = new MechanicalParameter(curveSegment.ExperimentalStrain[0]),
                        Stress = new MechanicalParameter(curveSegment.ExperimentalStress[0]),
                        TimeStep = curveSegment.TimePoints[1] - curveSegment.TimePoints[0],
                        ConstitutiveParameters = MapArrayToParameters(parameters),
                    };
                    return MechanicalModelCalculator.CalculateStress(currentInput, time, strain);
                },
                LowerBounds = CreateLowerBounds(),
                UpperBounds = CreateUpperBounds(),
                EvaluateConstraintsAndPenalties = CreateEvaluateConstraintsAndPenalties(),
                InitialParameters = CreateInitialParameters(),
            };
            CurveFitOutput curveFitOutput = CurveFitter.Fit(curveFitInput);
            yield return MapToOutput(curveFitOutput);
        }
    }

    protected MechanicalModelCurveFitOutput MapToOutput(CurveFitOutput curveFitOutput) => new(MapArrayToParameters(curveFitOutput.OptimizedParameters), curveFitOutput.FinalError, curveFitOutput.Iterations);

    /// <summary>
    /// Determines whether the given segment type is accepted for curve fitting.
    /// </summary>
    /// <param name="segmentType">The segment type to evaluate.</param>
    /// <returns><c>true</c> if the segment type should be processed; otherwise, <c>false</c>.</returns>
    protected abstract bool IsAcceptedSegment(SegmentType segmentType);

    /// <summary>
    /// Creates the initial constitutive parameters used as a starting point for optimization.
    /// </summary>
    /// <returns>A new instance of <typeparamref name="TConstitutiveParameters"/>.</returns>
    protected abstract double[] CreateInitialParameters();

    /// <summary>
    /// Creates the lower bounds array for the optimization parameters.
    /// </summary>
    protected abstract double[] CreateLowerBounds();

    /// <summary>
    /// Creates the upper bounds array for the optimization parameters.
    /// </summary>
    protected abstract double[] CreateUpperBounds();

    protected abstract Func<double[], double>? CreateEvaluateConstraintsAndPenalties();

    /// <summary>
    /// Maps the flat array of doubles back to a strongly-typed constitutive parameters object.
    /// </summary>
    protected abstract TConstitutiveParameters MapArrayToParameters(double[] array);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // No unmanaged resources to release.
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
