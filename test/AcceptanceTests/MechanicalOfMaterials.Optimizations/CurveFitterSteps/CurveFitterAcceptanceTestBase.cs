using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using Microsoft.Extensions.DependencyInjection;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.CurveFitterSteps;

public abstract class CurveFitterAcceptanceTestBase<TStep> where TStep : notnull, IMechanicalModelCurveFitterStep
{
    protected readonly TStep Step = GlobalServiceProvider.Provider.GetRequiredService<TStep>();

    protected abstract string Prefix { get; }

    [Fact]
    public async Task ExecuteAsync_WithCsvData_ShouldExecuteCurveFitting()
    {
        // Arrange
        CurveSegment[] segments = await AcceptanceTestHelpers.LoadSegmentsForPrefixAsync(Prefix);

        // Act
        List<MechanicalModelCurveFitOutput> outputs = [];
        await foreach (MechanicalModelCurveFitOutput output in Step.ExecuteAsync(segments))
        {
            outputs.Add(output);
        }

        // Assert
        if (segments.Length > 0)
        {
            Assert.NotEmpty(outputs);
            foreach (MechanicalModelCurveFitOutput output in outputs)
            {
                Assert.NotNull(output.ConstitutiveParameters);
            }
        }
    }
}
