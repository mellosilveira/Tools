using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Converters;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that persists the curve-fitted constitutive parameters to the database.
/// This step acts as a broadcast dead-end in the DAG topology.
/// </summary>
public class CurveFitOutputPersistenceStep(
    ILogger<CurveFitOutputPersistenceStep> logger, 
    IRepository repository) 
    : IAsyncPipelineStep<(string Identifier, MechanicalModelCurveFitOutput CurveFitOutput)>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { Converters = { new SignificantFiguresDoubleJsonConverter(7) } };

    /// <inheritdoc />
    public string Name => nameof(CurveFitOutputPersistenceStep);

    /// <inheritdoc />
    public async Task ExecuteAsync((string Identifier, MechanicalModelCurveFitOutput CurveFitOutput) input, CancellationToken cancellationToken)
    {
        string constitutiveParamsJson = JsonSerializer.Serialize(input.CurveFitOutput.ConstitutiveParameters, JsonOptions);
        MechanicalModelCurveFitEntity entity = new()
        {
            Identifier = input.Identifier,
            MechanicalModelName = input.CurveFitOutput.MechanicalModelName,
            InitialAcceptedRange = input.CurveFitOutput.AcceptedRange.InitialPoint,
            FinalAcceptedRange = input.CurveFitOutput.AcceptedRange.FinalPoint,
            MechanicalBehaviorType = input.CurveFitOutput.MechanicalBehaviorType,
            RampTimeConsideration = input.CurveFitOutput.RampTimeConsideration,
            ViscoelasticEffect = input.CurveFitOutput.ViscoelasticEffect,
            Error = input.CurveFitOutput.FinalError,
            Iterations = input.CurveFitOutput.Iterations,
            ConstitutiveParameters = constitutiveParamsJson
        };
        
        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (insertResult.IsConflict)
        {
            logger.LogWarning("Entity for mechanical model curve fit already exist on database. Identifier: {Identifier}", input.Identifier);
        }
        else if (!insertResult.Success)
        {
            logger.LogError("Failed to insert entity for mechanical model curve fit. Entity: {@Entity}. Result: {@InsertResult}", entity, insertResult);
            throw new Exception(string.Join(Environment.NewLine, insertResult.Messages));
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
