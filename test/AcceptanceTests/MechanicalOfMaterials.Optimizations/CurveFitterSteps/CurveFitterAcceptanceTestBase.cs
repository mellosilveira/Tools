using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using Microsoft.Extensions.DependencyInjection;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.CurveFitterSteps;

/// <summary>
/// Abstract base class for curve fitter step acceptance tests.
/// </summary>
/// <typeparam name="TStep">The concrete curve fitter step type.</typeparam>
public abstract class CurveFitterAcceptanceTestBase<TStep> where TStep : notnull, IMechanicalModelCurveFitterStep
{
    /// <summary>
    /// The resolved curve fitter step instance under test.
    /// </summary>
    protected readonly TStep Step = GlobalServiceProvider.Provider.GetRequiredService<TStep>();

    /// <summary>
    /// Gets the folder name prefix under TestData where test CSV files are located.
    /// </summary>
    protected abstract string Prefix { get; }

    /// <summary>
    /// Gets the minimum acceptable coefficient of determination (R²) for fitted model outputs.
    /// Default is 0.90 across all curve fitter steps.
    /// </summary>
    protected virtual double MinimumAcceptableRSquared => 0.90;

    /// <summary>
    /// Generic acceptance test that feeds CSV segments to the curve fitter step for all discovered groups,
    /// asserts output generation and quality, and invokes <see cref="AssertPhysicalConstraints(string, IReadOnlyList{CurveSegment}, IReadOnlyList{MechanicalModelCurveFitOutput})"/>
    /// for model-specific verification.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithCsvData_ShouldExecuteCurveFitting()
    {
        // Arrange
        IReadOnlyList<(string GroupName, CurveSegment[] Segments)> groups = await AcceptanceTestHelpers.LoadSegmentGroupsForPrefixAsync(Prefix);

        if (groups.Count == 0)
        {
            return;
        }

        foreach ((string groupName, CurveSegment[] segments) in groups)
        {
            if (segments.Length == 0)
            {
                continue;
            }

            // Act
            List<MechanicalModelCurveFitOutput> outputs = await ExecuteStepPipelineAsync(segments);

            // Assert: Baseline Quality Verification
            Assert.NotEmpty(outputs);

            foreach (MechanicalModelCurveFitOutput output in outputs)
            {
                Assert.NotNull(output.ConstitutiveParameters);
                Assert.False(double.IsNaN(output.RSquared), $"Group '{groupName}': RSquared cannot be NaN.");
                Assert.False(double.IsInfinity(output.RSquared), $"Group '{groupName}': RSquared cannot be Infinity.");
                Assert.True(output.RSquared >= MinimumAcceptableRSquared, $"Group '{groupName}': Step {typeof(TStep).Name} output RSquared ({output.RSquared:F4}) must be >= {MinimumAcceptableRSquared}.");
            }

            // Assert: Model-specific physical constraints hook
            AssertPhysicalConstraints(groupName, segments, outputs);
        }
    }

    /// <summary>
    /// Executes the curve fitter step by feeding all input curve segments sequentially,
    /// followed by a terminal unknown segment to drain multi-segment batch pipelines.
    /// </summary>
    /// <param name="segments">The input curve segments.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list of all yielded model curve fit outputs.</returns>
    protected async Task<List<MechanicalModelCurveFitOutput>> ExecuteStepPipelineAsync(CurveSegment[] segments, CancellationToken cancellationToken = default)
    {
        List<MechanicalModelCurveFitOutput> outputs = [];

        foreach (CurveSegment segment in segments)
        {
            await foreach (MechanicalModelCurveFitOutput output in Step.ExecuteAsync(segment, cancellationToken))
            {
                outputs.Add(output);
            }
        }

        // Emit terminal unknown segment to trigger multi-segment batch completion if applicable
        CurveSegment terminalSegment = new()
        {
            Type = SegmentType.Unknown,
            TimePoints = [],
            ExperimentalStrain = [],
            ExperimentalStress = []
        };

        await foreach (MechanicalModelCurveFitOutput output in Step.ExecuteAsync(terminalSegment, cancellationToken))
        {
            outputs.Add(output);
        }

        return outputs;
    }

    /// <summary>
    /// Hook method for concrete curve fitter acceptance tests to assert model-specific
    /// physical constraints, parameter bounds, and multi-stage convergence behaviors for a specific group.
    /// Default implementation delegates to <see cref="AssertPhysicalConstraints(IReadOnlyList{CurveSegment}, IReadOnlyList{MechanicalModelCurveFitOutput})"/>.
    /// </summary>
    /// <param name="groupName">The name of the specimen or test group.</param>
    /// <param name="segments">The input experimental segments.</param>
    /// <param name="outputs">The yielded curve fit outputs.</param>
    protected virtual void AssertPhysicalConstraints(string groupName, IReadOnlyList<CurveSegment> segments, IReadOnlyList<MechanicalModelCurveFitOutput> outputs)
    {
        AssertPhysicalConstraints(segments, outputs);
    }

    /// <summary>
    /// Hook method for concrete curve fitter acceptance tests to assert model-specific
    /// physical constraints, parameter bounds, and multi-stage convergence behaviors.
    /// Default implementation is a no-op.
    /// </summary>
    /// <param name="segments">The input experimental segments.</param>
    /// <param name="outputs">The yielded curve fit outputs.</param>
    protected virtual void AssertPhysicalConstraints(IReadOnlyList<CurveSegment> segments, IReadOnlyList<MechanicalModelCurveFitOutput> outputs)
    {
    }
}
