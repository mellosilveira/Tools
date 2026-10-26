using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that persists the curve-fitted constitutive parameters to the database.
/// This step acts as a broadcast dead-end in the DAG topology.
/// </summary>
/// <param name="pipelineIdentifier"></param>
/// <param name="mechanicalModelName"></param>
/// <param name="logger">The structured logger instance.</param>
/// <param name="repository">The database repository provider for persistence.</param>
public sealed class CurveFitOutputPersistenceStep(
    string pipelineIdentifier,
    string mechanicalModelName,
    ILogger<CurveFitOutputPersistenceStep> logger,
    IRepository repository)
    : IAsyncPipelineStep<(string Identifier, MechanicalModelCurveFitOutput CurveFitOutput)>
{
    /// <inheritdoc />
    public string Name => nameof(CurveFitOutputPersistenceStep);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when insertion of the curve fit entity fails in the database.</exception>
    public async Task ExecuteAsync((string Identifier, MechanicalModelCurveFitOutput CurveFitOutput) input, CancellationToken cancellationToken = default)
    {
        (string identifier, MechanicalModelCurveFitOutput curveFitOutput) = input;
        MechanicalModelCurveFitOutputEntity entity = new()
        {
            Identifier = identifier,
            ExtenalIdentifier = pipelineIdentifier,
            MechanicalModelName = mechanicalModelName,
            InitialAcceptedRange = curveFitOutput.AcceptedRange.InitialPoint,
            FinalAcceptedRange = curveFitOutput.AcceptedRange.FinalPoint,
            LoadResponseRelationship = curveFitOutput.LoadResponseRelationship,
            RampTimeConsideration = curveFitOutput.RampTimeConsideration,
            ViscoelasticEffect = curveFitOutput.ViscoelasticEffect,
            Error = curveFitOutput.FinalError,
            Iterations = curveFitOutput.Iterations,
            ConstitutiveParameters = OptimizationJsonOptions.Serialize(curveFitOutput.ConstitutiveParameters)
        };

        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (insertResult.IsConflict)
            logger.LogWarning(
                "Entity for mechanical model curve fit already exists in the database. Pipeline identifier: {PipelineIdentifier}. Identifier: {Identifier}", 
                pipelineIdentifier, 
                identifier);
        else if (!insertResult.Success)
            logger.LogError(
                "Failed to insert entity for mechanical model curve fit. Pipeline identifier: {PipelineIdentifier}. Identifier: {Identifier}. Entity: {@Entity}. Result: {@InsertResult}", 
                pipelineIdentifier, 
                identifier, 
                entity,
                insertResult);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
