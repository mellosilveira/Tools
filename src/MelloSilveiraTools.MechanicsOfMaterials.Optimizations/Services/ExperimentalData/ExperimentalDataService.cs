using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Abstractions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Factories;
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
    ExperimentalDataSettings settings,
    IMechanicalModelStepFactory stepFactory)
    : IExperimentalDataService
{
    /// <inheritdoc/>
    public async Task<Result<(string OutputFileName, ConstitutiveParameters[] Parameters)>> ProcessAsync(
        string mechanicalModelName,
        string identifier,
        string outputFileUri,
        Stream strainStream,
        Stream stressStream,
        ExperimentalDataProcessingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= ExperimentalDataProcessingOptions.Default;

        ConcurrentBag<ConstitutiveParameters[]> parameterBatches = [];

        ExperimentalDataSegmenterStep segmenterStep = new(logger, differentiation, options);
        ExperimentalDataFileWriterStep fileWriterStep = new(fileManager, outputFileUri, identifier);
        CurveSegmentBuilderStep segmentBuilderStep = new(options.SkipTimeStep);
        IMechanicalModelCurveFitterStep curveFitterStep = stepFactory.Create(mechanicalModelName);

        IDataflowPipeline<(Stream StrainStream, Stream StressStream)> pipeline = PipelineFactory
            .StartDataflow<(Stream StrainStream, Stream StressStream)>(logger, cancellationToken: cancellationToken)
            .WithLoggingErrors()
            .AddStep(segmenterStep, options: settings.SegmenterOptions)
            .AddBroadcastStep(fileWriterStep, options: settings.FileWriterOptions)
            .AddGroupWhileStep((prev, curr) => prev.SegmentType == curr.SegmentType, options: settings.GroupingOptions)
            .AddStep(segmentBuilderStep, options: settings.SegmentBuilderOptions)
            .AddCollectAllStep()
            .AddStep(curveFitterStep, options: settings.CurveFitterOptions)
            .BuildTerminal("CollectParameters", parameterBatches.Add);

        await using (pipeline)
        {
            await pipeline.SendAsync((strainStream, stressStream), cancellationToken).ConfigureAwait(false);

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);

            ConstitutiveParameters[] parameters = [.. parameterBatches.SelectMany(batch => batch)];
            return (fileWriterStep.OutputFullFileName, parameters);
        }
    }
}
