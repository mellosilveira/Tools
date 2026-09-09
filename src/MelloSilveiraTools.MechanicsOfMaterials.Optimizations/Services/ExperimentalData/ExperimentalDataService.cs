using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;

/// <summary>
/// Service responsible for orchestrating the ingestion, validation, and physical phase segmentation of experimental raw data streams.
/// </summary>
public class ExperimentalDataService(
    ILogger<ExperimentalDataService> logger,
    IDifferentiation differentiation,
    IFileManager fileManager,
    ExperimentalDataSettings settings)
    : IExperimentalDataService
{
    /// <inheritdoc/>
    public async Task<Result<(string OutputFileName, CurveSegment[] CurveSegments)>> ProcessAsync(
        string identifier,
        string outputFileUri,
        Stream strainStream,
        Stream stressStream,
        ExperimentalDataProcessingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= ExperimentalDataProcessingOptions.Default;

        ConcurrentBag<CurveSegment> curveSegments = [];

        FileInfo outputFile = fileManager.BuildTimebasedFileInfo(outputFileUri, identifier, FileExtensions.CommaSeparatedValues);
        StreamWriter writer = fileManager.CreateLargeFileWriter(outputFile);
        await using ExperimentalDataFileWriterStep fileWriterStep = new(writer, outputFile.FullName);
        using CurveSegmentBuilderStep segmentBuilderStep = new(options.SkipTimeStep);
        await using ExperimentalDataSegmenterStep segmenterStep = new(logger, differentiation, options);

        await using IDataflowPipeline<(Stream StrainStream, Stream StressStream)> pipeline = PipelineFactory.StartDataflow<(Stream StrainStream, Stream StressStream)>(logger, cancellationToken: cancellationToken)
            .AddStep(segmenterStep, options: settings.SegmenterOptions)
            .AddBroadcastStep(fileWriterStep, options: settings.FileWriterOptions)
            .AddGroupWhileStep((prev, curr) => prev.SegmentType == curr.SegmentType, options: settings.GroupingOptions)
            .AddStep(segmentBuilderStep, options: settings.SegmentBuilderOptions)
            .BuildTerminal("CollectSegments", curveSegments.Add);

        await pipeline.SendAsync((strainStream, stressStream), cancellationToken).ConfigureAwait(false);

        pipeline.Complete();
        await pipeline.Completion.ConfigureAwait(false);

        return (fileWriterStep.OutputFilePath, [.. curveSegments]);
    }
}