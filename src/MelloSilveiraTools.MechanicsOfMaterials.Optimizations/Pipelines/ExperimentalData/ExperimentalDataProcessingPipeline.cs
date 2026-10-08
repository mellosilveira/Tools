using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;

/// <summary>
/// Service responsible for orchestrating the ingestion, validation, and physical phase segmentation of experimental raw data streams.
/// </summary>
/// <param name="loggerFactory">The logger factory for creating step-specific loggers.</param>
/// <param name="fileManager">The file manager used for file writing steps.</param>
/// <param name="differentiation">Numerical differentiation provider used for segment classification.</param>
/// <param name="stepFactory">Factory resolving model-specific curve fitter steps.</param>
/// <param name="repository">Database repository provider for persisting curve fits and simulations.</param>
/// <param name="calculatorFactory">Factory resolving mechanical model calculators.</param>
/// <param name="settings">Pipeline configuration settings and step options.</param>
public sealed class ExperimentalDataProcessingPipeline(
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
        ILogger<ExperimentalDataProcessingPipeline> logger = loggerFactory.CreateLogger<ExperimentalDataProcessingPipeline>();

        string uniqueIdentifier = string.IsNullOrWhiteSpace(input.Identifier) ? Guid.NewGuid().ToString("N") : input.Identifier;

        ExperimentalDataSegmenterStep segmenterStep = new(loggerFactory.CreateLogger<ExperimentalDataSegmenterStep>(), differentiation);
        ExperimentalDataFileWriterStep fileWriterStep = new(fileManager, input.OutputFileUri, uniqueIdentifier);
        ExperimentalDataDownsamplerStep downsamplerStep = new(input.Options.SkipTimeStep);
        CurveSegmentAccumulatorStep accumulatorStep = new();
        IMechanicalModelCurveFitterStep curveFitterStep = stepFactory.Create(input.MechanicalModelName, input.TargetSegments);

        IdentifierBuilderStep identifierBuilderStep = new();
        CurveFitOutputPersistenceStep curveFitPersistenceStep = new(loggerFactory.CreateLogger<CurveFitOutputPersistenceStep>(), repository);
        NumericalSimulationStep numericalSimulationStep = new(calculatorFactory, input.SimulationTimeStep, input.FinalSimulationTime);
        AsymptoteMonitoringStep asymptoteStep = new(input.AsymptoteConsecutivePointsThreshold);
        DeltaCalculatorStep deltaStep = new();
        MechanicalModelOutputFileWriterStep csvWriterStep = new(fileManager, input.OutputFileUri, uniqueIdentifier);
        MechanicalModelOutputPersistenceStep mechanicalModelOutputPersistenceStep = new(loggerFactory.CreateLogger<MechanicalModelOutputPersistenceStep>(), repository);

        IDataflowPipeline<ExperimentalDataSegmenterInput> pipeline = PipelineFactory
            .StartDataflow<ExperimentalDataSegmenterInput>(logger, cancellationToken: cancellationToken)
            .WithLoggingErrors()
            .AddStep(segmenterStep, settings.SegmenterOptions)
            .AddBroadcastStep(fileWriterStep, options: settings.FileWriterOptions)
            .AddStep(downsamplerStep, PipelineStepOptions.Synchronous)
            .AddStep(accumulatorStep, PipelineStepOptions.Synchronous)
            .AddStep(curveFitterStep, settings.CurveFitterOptions)
            .Fork(
                curveFit => curveFit,
                identifier => identifier.AddStep(identifierBuilderStep),
                simulation => simulation
                    .AddStep(numericalSimulationStep)
                    .Fork(asymptoteStep, deltaStep, csvWriterStep)
            )
            .AddBroadcastStep(curveFitPersistenceStep, t => (t.Item2, t.Item1))
            .AddDataMapping(t => new MechanicalModelOutputPersistenceInput(
                MechanicalModelName: input.MechanicalModelName,
                CurveFitIdentifier: t.Item2,
                AsymptoteTime: t.Item3.Item1,
                Delta: t.Item3.Item2,
                FileData: t.Item3.Item3))
            .AddStep(mechanicalModelOutputPersistenceStep)
            .BuildTerminal();

        await using (pipeline)
        {
            bool accepted = await pipeline.SendAsync(input.ToSegmenterInput(), cancellationToken).ConfigureAwait(false);
            if (!accepted)
            {
                logger.LogError("Failed to ingest experimental data into the processing pipeline. Identifier: {Identifier}. Input: {@Input}", uniqueIdentifier, input);
                return Result.CreateBadRequest("Failed to ingest experimental data into the processing pipeline.");
            }

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);

            return uniqueIdentifier;
        }
    }
}
