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
public class CurveFitOutputPersistenceStep(ILogger<CurveFitOutputPersistenceStep> logger, IRepository repository) 
    : IAsyncPipelineStep<(string OutputIdentifier, MechanicalModelCurveFitOutput CurveFit), (string OutputIdentifier, MechanicalModelCurveFitOutput CurveFit)>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { Converters = { new SignificantFiguresDoubleJsonConverter(7) } };

    /// <inheritdoc />
    public string Name => nameof(CurveFitOutputPersistenceStep);

    /// <inheritdoc />
    public async Task<(string OutputIdentifier, MechanicalModelCurveFitOutput CurveFit)> ExecuteAsync((string OutputIdentifier, MechanicalModelCurveFitOutput CurveFit) input, CancellationToken cancellationToken)
    {
        string constitutiveParamsJson = JsonSerializer.Serialize((object)input.CurveFit.ConstitutiveParameters, JsonOptions);

        MechanicalModelCurveFitEntity entity = new()
        {
            Identifier = input.OutputIdentifier,
            MechanicalModelName = input.CurveFit.MechanicalModelName,
            InitialAcceptedRange = input.CurveFit.AcceptedRange.InitialPoint,
            FinalAcceptedRange = input.CurveFit.AcceptedRange.FinalPoint,
            MechanicalBehaviorType = input.CurveFit.MechanicalBehaviorType,
            RampTimeConsideration = input.CurveFit.RampTimeConsideration,
            ViscoelasticEffect = input.CurveFit.ViscoelasticEffect,
            Error = (decimal)input.CurveFit.FinalError,
            Iterations = input.CurveFit.Iterations,
            ConstitutiveParameters = constitutiveParamsJson
        };

        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (insertResult.IsConflict)
        {
            logger.LogWarning("Entity for mechanical model curve fit already exist on database. Identifier: {Identifier}", input.OutputIdentifier);
        }
        else if (!insertResult.Success)
        {
            logger.LogError("Failed to insert entity for mechanical model curve fit. Entity: {@Entity}. Result: {@InsertResult}", entity, insertResult);
            throw new Exception(string.Join(Environment.NewLine, insertResult.Messages));
        }

        return input;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
