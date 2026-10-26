using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
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
/// <param name="fileStorageService">The file storage service used for file writing steps.</param>
/// <param name="differentiation">Numerical differentiation provider used for segment classification.</param>
/// <param name="mechanicalModelStepFactory">Factory resolving model-specific curve fitter steps.</param>
/// <param name="repository">Database repository provider for persisting curve fits and simulations.</param>
/// <param name="pipelineFactory"></param>
/// <param name="serviceLocator"></param>
/// <param name="mechanicalModelCalculatorFactory">Factory resolving mechanical model calculators.</param>
/// <param name="settings">Pipeline configuration settings and step options.</param>
public sealed class ExperimentalDataProcessingPipeline(
    ILoggerFactory loggerFactory,
    IFileStorageService fileStorageService,
    IDifferentiation differentiation,
    IMechanicalModelStepFactory mechanicalModelStepFactory,
    IMechanicalModelCalculatorFactory mechanicalModelCalculatorFactory,
    IRepository repository,
    PipelineFactory pipelineFactory,
    ServiceLocator serviceLocator,
    ExperimentalDataSettings settings)
    : IPipeline<ExperimentalDataProcessingInput, string>
{
    /// <inheritdoc/>
    public async Task<Result<string>> ProcessAsync(ExperimentalDataProcessingInput input, CancellationToken cancellationToken = default)
    {
        ILogger<ExperimentalDataProcessingPipeline> logger = loggerFactory.CreateLogger<ExperimentalDataProcessingPipeline>();

        string pipelineIdentifier = string.IsNullOrWhiteSpace(input.Identifier) ? Guid.NewGuid().ToString("N") : input.Identifier;
        IMechanicalModelTypeResolver mechanicalModelTypeResolver = serviceLocator.GetRequiredKeyedService<IMechanicalModelTypeResolver>(input.MechanicalModelName);

        ExperimentalDataSegmenterStep segmenterStep = new(loggerFactory.CreateLogger<ExperimentalDataSegmenterStep>(), differentiation);
        (string experimentalDataFileLocation, ExperimentalDataFileWriterStep experimentalDataFileWriterStep) = await ExperimentalDataFileWriterStep.CreateAsync(fileStorageService, pipelineIdentifier);
        ExperimentalDataDownsamplerStep downsamplerStep = new(input.DownsamplingSkipTimeStep);
        CurveSegmentBuilderStep curveSegmentBuilderStep = new();
        IMechanicalModelCurveFitterStep curveFitterStep = mechanicalModelStepFactory.CreateCurveFitterStep(input.MechanicalModelName, input.TargetSegments, input.CurveFitterType, input.TargetRSquared);
        CurveFitOutputIdentifierBuilderStep curveFitOutputIdentifierBuilderStep = new();
        MechanicalModelSimulationStep mechanicalModelSimulationStep = new(pipelineIdentifier, fileStorageService, mechanicalModelTypeResolver, mechanicalModelCalculatorFactory, input.SimulationTimeStep, input.FinalSimulationTime, input.AsymptoteConsecutivePointsThreshold);
        CurveFitOutputPersistenceStep curveFitPersistenceStep = new(pipelineIdentifier, input.MechanicalModelName, loggerFactory.CreateLogger<CurveFitOutputPersistenceStep>(), repository);
        MechanicalModelOutputPersistenceStep mechanicalModelOutputPersistenceStep = new(pipelineIdentifier, input.MechanicalModelName, loggerFactory.CreateLogger<MechanicalModelOutputPersistenceStep>(), fileStorageService, repository);

        IDataflowPipeline<ExperimentalDataSegmenterInput> pipeline = pipelineFactory
            .CreateDataflow<ExperimentalDataSegmenterInput>(logger, cancellationToken: cancellationToken)
            .WithLoggingErrors()
            .AddStep(segmenterStep, settings.SegmenterOptions)
            .AddBroadcastStep(experimentalDataFileWriterStep, options: settings.FileWriterOptions)
            .AddStep(downsamplerStep, PipelineStepOptions.Synchronous)
            .AddStep(curveSegmentBuilderStep, PipelineStepOptions.Synchronous)
            .AddStep(curveFitterStep, settings.CurveFitterOptions)
            .Fork(
                originalBranch => originalBranch,
                identifierBranch => identifierBranch.AddStep(curveFitOutputIdentifierBuilderStep),
                simulationBranch => simulationBranch.AddStep(mechanicalModelSimulationStep)
            )
            .AddBroadcastStep(curveFitPersistenceStep, t => (t.Item2, t.Item1))
            .AddDataMapping(t => new MechanicalModelOutputPersistenceInput(
                experimentalDataFileLocation,
                CurveFitOutputIdentifier: t.Item2,
                t.Item3.AsymptoteTime,
                t.Item3.DeltaOutput,
                t.Item3.OutputFileMetadata)
            )
            .AddStep(mechanicalModelOutputPersistenceStep)
            .BuildTerminal();

        await using (pipeline)
        {
            bool accepted = await pipeline.SendAsync(input.ToSegmenterInput(), cancellationToken).ConfigureAwait(false);
            if (!accepted)
            {
                logger.LogError("Failed to ingest experimental data into the processing pipeline. Identifier: {Identifier}. Input: {@Input}", pipelineIdentifier, input);
                return Result.CreateBadRequest("Failed to ingest experimental data into the processing pipeline.");
            }

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);
        }

        return pipelineIdentifier;
    }
}
