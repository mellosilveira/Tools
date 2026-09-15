using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;

/// <summary>
/// Service responsible for orchestrating the ingestion, validation, and physical phase segmentation of experimental raw data streams.
/// </summary>
public class ExperimentalDataService(
    ILogger<ExperimentalDataService> logger,
    IFileManager fileManager,
    IDifferentiation differentiation,
    IMechanicalModelStepFactory stepFactory,
    ExperimentalDataSettings settings)
    : IExperimentalDataService
{
    /// <inheritdoc/>
    public async Task<Result<(string OutputFileName, ConstitutiveParameters[] Parameters)>> ProcessAsync(ExperimentalDataProcessingInput input, CancellationToken cancellationToken = default)
    {
        ConcurrentBag<ConstitutiveParameters[]> parameterBatches = [];

        ExperimentalDataSegmenterStep segmenterStep = new(logger, differentiation);
        ExperimentalDataFileWriterStep fileWriterStep = new(fileManager, input.OutputFileUri, input.Identifier);
        CurveSegmentBuilderStep curveSegmentBuilderStep = new();
        IMechanicalModelCurveFitterStep curveFitterStep = stepFactory.Create(input.MechanicalModelName, input.TargetSegments);

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
            .BuildTerminal("CollectParameters", parameterBatches.Add);

        await using (pipeline)
        {
            await pipeline.SendAsync(input.ToSegmenterInput(), cancellationToken).ConfigureAwait(false);

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);

            ConstitutiveParameters[] parameters = [.. parameterBatches.SelectMany(batch => batch)];
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