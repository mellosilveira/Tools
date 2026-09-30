using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that persists the curve-fitted constitutive parameters to the database.
/// This step acts as a broadcast dead-end in the DAG topology.
/// </summary>
/// <param name="logger">The structured logger instance.</param>
/// <param name="repository">The database repository provider for persistence.</param>
public sealed class CurveFitOutputPersistenceStep(
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
        ConstitutiveParameters constitutiveParameters = input.CurveFitOutput.ConstitutiveParameters;
        string constitutiveParamsJson = JsonSerializer.Serialize(constitutiveParameters, constitutiveParameters.GetType(), OptimizationJsonOptions.SignificantFigures7);

        MechanicalModelCurveFitEntity entity = new()
        {
            Identifier = input.Identifier,
            MechanicalModelName = input.CurveFitOutput.MechanicalModelName,
            InitialAcceptedRange = input.CurveFitOutput.AcceptedRange.InitialPoint,
            FinalAcceptedRange = input.CurveFitOutput.AcceptedRange.FinalPoint,
            LoadResponseRelationship = input.CurveFitOutput.LoadResponseRelationship,
            RampTimeConsideration = input.CurveFitOutput.RampTimeConsideration,
            ViscoelasticEffect = input.CurveFitOutput.ViscoelasticEffect,
            Error = input.CurveFitOutput.FinalError,
            Iterations = input.CurveFitOutput.Iterations,
            ConstitutiveParameters = constitutiveParamsJson
        };

        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (insertResult.IsConflict)
        {
            logger.LogWarning("Entity for mechanical model curve fit already exists in the database. Identifier: {Identifier}", input.Identifier);
        }
        else if (!insertResult.Success)
        {
            logger.LogError("Failed to insert entity for mechanical model curve fit. Entity: {@Entity}. Result: {@InsertResult}", entity, insertResult);
            throw new InvalidOperationException(string.Join(Environment.NewLine, insertResult.Messages));
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
