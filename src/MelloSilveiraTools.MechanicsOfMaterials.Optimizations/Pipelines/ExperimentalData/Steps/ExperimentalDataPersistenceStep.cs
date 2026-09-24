using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Converters;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that persists the curve-fitted constitutive parameters to the database.
/// Acts as a pass-through, yielding the output for further in-memory collection and simulation.
/// </summary>
public class ExperimentalDataPersistenceStep(ILogger<ExperimentalDataPersistenceStep> logger, IRepository repository) : IAsyncPipelineStep<MechanicalModelCurveFitOutput, string>
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { Converters = { new SignificantFiguresDoubleJsonConverter(7) } };

    public string Name => nameof(ExperimentalDataPersistenceStep);

    public async Task<string> ExecuteAsync(MechanicalModelCurveFitOutput output, CancellationToken cancellationToken)
    {
        string constitutiveParamsJson = JsonSerializer.Serialize((object)output.ConstitutiveParameters, _jsonOptions);

        string identifierHash = ComputeSha256Hash(constitutiveParamsJson);

        MechanicalModelCurveFitEntity entity = new()
        {
            Identifier = identifierHash,
            MechanicalModelName = output.MechanicalModelName,
            InitialAcceptedRange = output.AcceptedRange.InitialPoint,
            FinalAcceptedRange = output.AcceptedRange.FinalPoint,
            MechanicalBehaviorType = output.MechanicalBehaviorType,
            RampTimeConsideration = output.RampTimeConsideration,
            ViscoelasticEffect = output.ViscoelasticEffect,
            Error = (decimal)output.FinalError,
            Iterations = output.Iterations,
            ConstitutiveParameters = constitutiveParamsJson
        };

        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (insertResult.IsConflict)
        {
            logger.LogWarning("Entity for mechanical model curve fit already exist on database. Identifier: {Identifier}", identifierHash);
        }
        else if (!insertResult.Success)
        {
            logger.LogError("Failed to insert entity for mechanical model curve fit. Result: {InsertResult}", insertResult);
            throw new Exception(string.Join(Environment.NewLine, insertResult.Messages));
        }

        return identifierHash;
    }

    private static string ComputeSha256Hash(string rawData)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
