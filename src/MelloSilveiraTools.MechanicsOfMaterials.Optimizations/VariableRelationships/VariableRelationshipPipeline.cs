using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Steps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships;

/// <summary>
/// Orchestrates the Variable Relationship and Iso-Stress exploration pipeline using the TPL Dataflow framework.
/// Configures a linear dataflow: Cartesian discretization of $n-1$ parameters, 1D Newton-Raphson root-finding
/// for the dependent parameter $p_k$, tolerance filtering, partitioned CSV writing, and execution persistence.
/// </summary>
/// <param name="logger">Structured logger instance for the pipeline.</param>
/// <param name="calculatorFactory">Factory resolving mechanical model calculator facades.</param>
/// <param name="differentiation">Numerical differentiation provider for gradient evaluation.</param>
/// <param name="fileStorageService">Storage service responsible for creating partitioned CSV files.</param>
/// <param name="repository">Database repository provider for persisting execution metadata.</param>
/// <param name="pipelineFactory">Factory responsible for bootstrapping Dataflow execution topologies.</param>
/// <param name="loggerFactory">Optional logger factory for creating step-specific loggers.</param>
public sealed class VariableRelationshipPipeline(
    ILogger<VariableRelationshipPipeline> logger,
    IMechanicalModelCalculatorFactory calculatorFactory,
    IDifferentiation differentiation,
    IFileStorageService fileStorageService,
    IRepository repository,
    PipelineFactory pipelineFactory,
    ILoggerFactory? loggerFactory = null)
    : IPipeline<VariableRelationshipInput, VariableRelationshipOutput>
{
    private readonly IRepository? _repository = repository;

    /// <summary>
    /// Initializes a secondary instance of the <see cref="VariableRelationshipPipeline"/> class without a mandatory database repository.
    /// </summary>
    /// <param name="logger">Structured logger instance for the pipeline.</param>
    /// <param name="calculatorFactory">Factory resolving mechanical model calculator facades.</param>
    /// <param name="differentiation">Numerical differentiation provider for gradient evaluation.</param>
    /// <param name="fileStorageService">Storage service responsible for creating partitioned CSV files.</param>
    /// <param name="pipelineFactory">Factory responsible for bootstrapping Dataflow execution topologies.</param>
    /// <param name="loggerFactory">Optional logger factory for creating step-specific loggers.</param>
    public VariableRelationshipPipeline(
        ILogger<VariableRelationshipPipeline> logger,
        IMechanicalModelCalculatorFactory calculatorFactory,
        IDifferentiation differentiation,
        IFileStorageService fileStorageService,
        PipelineFactory pipelineFactory,
        ILoggerFactory? loggerFactory = null)
        : this(logger, calculatorFactory, differentiation, fileStorageService, null!, pipelineFactory, loggerFactory)
    {
    }

    /// <inheritdoc />
    public async Task<Result<VariableRelationshipOutput>> ProcessAsync(
        VariableRelationshipInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        string uniqueIdentifier = Guid.NewGuid().ToString("N");

        VariableGridGeneratorStep gridStep = new(calculatorFactory, differentiation);
        IsoStressNewtonRaphsonStep newtonStep = new(calculatorFactory, differentiation);
        IsoStressFilterStep filterStep = new(input.ParameterBounds, input.Tolerance);
        IsoStressCsvWriterStep writerStep = new(fileStorageService, uniqueIdentifier, input.ParameterBounds);

        ILogger<VariableRelationshipPersistenceStep> persistenceLogger = loggerFactory?.CreateLogger<VariableRelationshipPersistenceStep>()
            ?? NullLogger<VariableRelationshipPersistenceStep>.Instance;
        VariableRelationshipPersistenceStep persistenceStep = new(persistenceLogger, _repository);

        IDataflowPipeline<VariableRelationshipInput> pipeline = pipelineFactory
            .CreateDataflow<VariableRelationshipInput>(logger, pipelineIdentifier: uniqueIdentifier, initialBufferSize: 1000, cancellationToken: cancellationToken)
            .AddStep(gridStep)
            .AddStep(newtonStep)
            .AddStep(filterStep)
            .AddStep(writerStep)
            .AddCollectAllStep()
            .AddStep(new VariableRelationshipFinalizationStep(writerStep, uniqueIdentifier, input))
            .AddStep(persistenceStep)
            .BuildTerminal();

        await using (pipeline)
        {
            bool accepted = await pipeline.SendAsync(input, cancellationToken).ConfigureAwait(false);
            if (!accepted)
            {
                logger.LogError("Failed to ingest input into the variable relationship pipeline. Identifier: {Identifier}. Input: {@Input}", uniqueIdentifier, input);
                return Result.CreateBadRequest("Failed to ingest input into the variable relationship pipeline.");
            }

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);

            if (persistenceStep.Output is null)
            {
                logger.LogError("Variable relationship pipeline completed without generating output. Identifier: {Identifier}", uniqueIdentifier);
                return Result.CreateUnknownError("Variable relationship pipeline completed without producing output.");
            }

            return persistenceStep.Output;
        }
    }

    private sealed class VariableRelationshipFinalizationStep(
        IsoStressCsvWriterStep writer,
        string identifier,
        VariableRelationshipInput input)
        : IAsyncPipelineStep<IReadOnlyList<VariableCombinationPoint>, VariableRelationshipPersistenceInput>
    {
        public string Name => nameof(VariableRelationshipFinalizationStep);

        public async Task<VariableRelationshipPersistenceInput> ExecuteAsync(
            IReadOnlyList<VariableCombinationPoint> combinations,
            CancellationToken cancellationToken = default)
        {
            await writer.DisposeAsync().ConfigureAwait(false);
            StorageEntryMetadata metadata = writer.GeneratedFileMetadata
                ?? throw new InvalidOperationException("Failed to generate CSV metadata for variable relationship pipeline execution.");

            return new VariableRelationshipPersistenceInput(
                identifier,
                input,
                combinations,
                metadata);
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }
}
