using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;

/// <summary>
/// Service responsible for orchestrating the ingestion, validation, and physical phase segmentation of experimental raw data streams.
/// </summary>
public class ExperimentalDataService(
    ILogger<ExperimentalDataService> logger,
    IFileManager fileManager,
    IDifferentiation differentiation,
    IMechanicalModelStepFactory stepFactory,
    IRepository repository,
    IMechanicalModelCalculatorFactory calculatorFactory,
    ExperimentalDataSettings settings)
    : IExperimentalDataService
{
    /// <inheritdoc/>
    public async Task<Result<(string OutputFileName, MechanicalModelCurveFitOutput[] Parameters)>> ProcessAsync(ExperimentalDataProcessingInput input, CancellationToken cancellationToken = default)
    {
        ConcurrentBag<MechanicalModelCurveFitOutput> parameterBatches = [];

        ExperimentalDataSegmenterStep segmenterStep = new(logger, differentiation);
        ExperimentalDataFileWriterStep fileWriterStep = new(fileManager, input.OutputFileUri, input.Identifier);
        CurveSegmentBuilderStep curveSegmentBuilderStep = new();
        IMechanicalModelCurveFitterStep curveFitterStep = stepFactory.Create(input.MechanicalModelName, input.TargetSegments);
        ExperimentalDataPersistenceStep persistenceStep = new(repository);
        MechanicalModelSimulationStep simulationStep = new(
            fileManager,
            calculatorFactory,
            repository,
            input.OutputFileUri,
            input.Identifier,
            input.FinalSimulationTime,
            input.SimulationTimeStep);

        IDataflowPipeline<ExperimentalDataSegmenterInput> pipeline = PipelineFactory
            .StartDataflow<ExperimentalDataSegmenterInput>(logger, cancellationToken: cancellationToken)
            .WithLoggingErrors()
            .AddStep(segmenterStep, settings.SegmenterOptions)
            .AddBroadcastStep(fileWriterStep, options: settings.FileWriterOptions)
            .AddGroupWhileStep((prev, curr) => prev.SegmentType == curr.SegmentType, settings.GroupingOptions)
            .AddDataMapping(points => new CurveSegmentBuilderInput(input.Options.SkipTimeStep, points))
            .AddStep(curveSegmentBuilderStep, settings.SegmentBuilderOptions)
            .AddCollectAllStep()
            .AddStep(curveFitterStep, settings.CurveFitterOptions)
            .AddStep(persistenceStep)
            .AddStep(simulationStep)
            .BuildTerminal("CollectParameters", parameterBatches.Add);

        await using (pipeline)
        {
            await pipeline.SendAsync(input.ToSegmenterInput(), cancellationToken).ConfigureAwait(false);

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);

            MechanicalModelCurveFitOutput[] parameters = [.. parameterBatches];
            return (fileWriterStep.OutputFullFileName, parameters);
        }
    }
}

public record ExperimentalDataProcessingInput
{
    public string MechanicalModelName { get; init; }

    /// <summary>
    /// Segment types to be processed.
    /// If left  empty, all segment types will be considered.
    /// </summary>
    public IReadOnlyList<SegmentType> TargetSegments { get; init; } = [];

    public string Identifier { get; init; }
    public string OutputFileUri { get; init; }
    public Stream StrainStream { get; init; }
    public Stream StressStream { get; init; }
    public ExperimentalDataProcessingOptions Options { get; init; }

    /// <summary>
    /// Optional target final simulation time. When provided and greater than the last experimental time,
    /// the forward numerical simulation continues marching until this time is reached.
    /// </summary>
    public double? FinalSimulationTime { get; init; }

    /// <summary>
    /// Optional time step used during extended simulation. If omitted, defaults to the experimental time step.
    /// </summary>
    public double? SimulationTimeStep { get; init; }

    public ExperimentalDataSegmenterInput ToSegmenterInput() => new() { StrainStream = StrainStream, StressStream = StressStream, Options = Options };
}

public record ExperimentalDataSegmenterInput
{
    public Stream StrainStream { get; init; }
    public Stream StressStream { get; init; }
    public ExperimentalDataProcessingOptions Options { get; init; }
}

/// <summary>
/// 
/// </summary>
/// <param name="SkipTimeStep">The minimum time interval required between consecutive points within a segment. Defaults to 0.0 (no downsampling).</param>
/// <param name="Points"></param>
public record CurveSegmentBuilderInput(double SkipTimeStep, SegmentedDataPoint[] Points);