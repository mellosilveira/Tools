using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;

/// <summary>
/// Service responsible for orchestrating the ingestion, validation, and physical phase segmentation of experimental raw data streams.
/// </summary>
public class ExperimentalDataProcessingPipeline(
    ILoggerFactory loggerFactory,
    IFileManager fileManager,
    IDifferentiation differentiation,
    IMechanicalModelStepFactory stepFactory,
    IRepository repository,
    IMechanicalModelCalculatorFactory calculatorFactory,
    ExperimentalDataSettings settings)
    : IExperimentalDataProcessingPipeline
{
    /// <inheritdoc/>
    public async Task<Result<string>> ProcessAsync(ExperimentalDataProcessingInput input, CancellationToken cancellationToken = default)
    {
        string uniqueIdentifier = string.IsNullOrWhiteSpace(input.Identifier) ? Guid.NewGuid().ToString("N") : input.Identifier;

        ExperimentalDataSegmenterStep segmenterStep = new(loggerFactory.CreateLogger<ExperimentalDataSegmenterStep>(), differentiation);
        ExperimentalDataFileWriterStep fileWriterStep = new(fileManager, input.OutputFileUri, uniqueIdentifier);
        CurveSegmentBuilderStep curveSegmentBuilderStep = new();
        IMechanicalModelCurveFitterStep curveFitterStep = stepFactory.Create(input.MechanicalModelName, input.TargetSegments); // TODO: ADICIONAR LOG PARA SEGMENTOS IGNORADOS.
        ExperimentalDataPersistenceStep persistenceStep = new(loggerFactory.CreateLogger<ExperimentalDataPersistenceStep>(), repository);
        SimulationInputBuilderStep simulationInputBuilderStep = new(input.SimulationTimeStep, input.FinalSimulationTime);
        SimulationOrchestratorStep simulationStep = new(fileManager, calculatorFactory, repository, input.OutputFileUri, input.AsymptoteConsecutivePointsThreshold);

        IDataflowPipeline<ExperimentalDataSegmenterInput> pipeline = PipelineFactory
            .StartDataflow<ExperimentalDataSegmenterInput>(loggerFactory.CreateLogger<ExperimentalDataProcessingPipeline>(), cancellationToken: cancellationToken)
            .WithLoggingErrors()
            .AddStep(segmenterStep, settings.SegmenterOptions)
            .AddBroadcastStep(fileWriterStep, options: settings.FileWriterOptions)
            .AddGroupWhileStep((prev, curr) => prev.SegmentType == curr.SegmentType, settings.GroupingOptions)
            .AddDataMapping(points => new CurveSegmentBuilderInput(input.Options.SkipTimeStep, points))
            .AddStep(curveSegmentBuilderStep, settings.SegmentBuilderOptions)
            .AddCollectAllStep()
            .AddStep(curveFitterStep, settings.CurveFitterOptions)
            .AddStep(persistenceStep)
            .AddStep(simulationInputBuilderStep, settings.SimulationInputBuilderOptions)
            .AddStep(simulationStep)
            .BuildTerminal();

        await using (pipeline)
        {
            await pipeline.SendAsync(input.ToSegmenterInput(), cancellationToken).ConfigureAwait(false);

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);

            return uniqueIdentifier;
        }
    }
}

/// <summary>
/// Input parameters for processing experimental raw data streams through the segmentation and optimization pipeline.
/// </summary>
public record ExperimentalDataProcessingInput
{
    /// <summary>
    /// Optional predefined analysis identifier. If not provided, a unique GUID-based identifier is generated.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// The name of the mechanical model to fit (e.g., "Fung", "Schapery").
    /// </summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>
    /// The directory URI where generated output files are saved.
    /// </summary>
    public required string OutputFileUri { get; init; }

    /// <summary>
    /// Segment types to be processed.
    /// If left empty, all segment types will be considered.
    /// </summary>
    public IReadOnlyList<SegmentType> TargetSegments { get; init; } = [];

    /// <summary>
    /// Stream containing raw experimental strain time-history.
    /// </summary>
    public Stream StrainStream { get; init; }

    /// <summary>
    /// Stream containing raw experimental stress time-history.
    /// </summary>
    public Stream StressStream { get; init; }

    /// <summary>
    /// Optional target final simulation time. When provided and greater than the last experimental time,
    /// the forward numerical simulation continues marching until this time is reached.
    /// </summary>
    public double? FinalSimulationTime { get; init; }

    /// <summary>
    /// Optional time step used during extended simulation. If omitted, defaults to the experimental time step.
    /// </summary>
    public double? SimulationTimeStep { get; init; }

    /// <summary>
    /// The consecutive points threshold required to detect that a steady-state asymptote has been reached. Defaults to 10.
    /// </summary>
    public int AsymptoteConsecutivePointsThreshold { get; init; } = 10;

    /// <summary>
    /// Processing options for segmentation tolerances and smoothing.
    /// </summary>
    public ExperimentalDataProcessingOptions Options { get; init; }

    /// <summary>
    /// Converts this processing input into the segmenter ingestion payload.
    /// </summary>
    public ExperimentalDataSegmenterInput ToSegmenterInput() => new() { StrainStream = StrainStream, StressStream = StressStream, Options = Options };
}

/// <summary>
/// Ingestion payload consumed by the <see cref="ExperimentalDataSegmenterStep"/>.
/// </summary>
public record ExperimentalDataSegmenterInput
{
    /// <summary>
    /// Stream containing raw experimental strain time-history.
    /// </summary>
    public Stream StrainStream { get; init; }

    /// <summary>
    /// Stream containing raw experimental stress time-history.
    /// </summary>
    public Stream StressStream { get; init; }

    /// <summary>
    /// Processing options for segmentation tolerances and smoothing.
    /// </summary>
    public ExperimentalDataProcessingOptions Options { get; init; }
}

/// <summary>
/// Input for assembling segmented data points into a physical <see cref="CurveSegment"/>.
/// </summary>
/// <param name="SkipTimeStep">The minimum time interval required between consecutive points within a segment. Defaults to 0.0 (no downsampling).</param>
/// <param name="Points">The array of segmented data points belonging to the segment.</param>
public record CurveSegmentBuilderInput(double SkipTimeStep, SegmentedDataPoint[] Points);