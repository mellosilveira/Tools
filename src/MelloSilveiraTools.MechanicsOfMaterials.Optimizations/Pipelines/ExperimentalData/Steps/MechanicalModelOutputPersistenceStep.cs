using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Converters;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that converges all simulation analysis branches (Identifier, Asymptote, Delta, CSV) 
/// and persists the final simulation entity to the database.
/// </summary>
public class MechanicalModelOutputPersistenceStep(IRepository repository)
    : IAsyncPipelineStep<MechanicalModelOutputPersistenceInput, MechanicalModelCurveFitOutput>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { Converters = { new SignificantFiguresDoubleJsonConverter(7) } };

    /// <inheritdoc />
    public string Name => nameof(MechanicalModelOutputPersistenceStep);

    /// <inheritdoc />
    public async Task<MechanicalModelCurveFitOutput> ExecuteAsync(MechanicalModelOutputPersistenceInput input, CancellationToken cancellationToken)
    {
        string initialOutputJson = JsonSerializer.Serialize((object)input.Delta.InitialOutput, JsonOptions);
        string finalOutputJson = JsonSerializer.Serialize((object)input.Delta.FinalOutput, JsonOptions);
        string absoluteDeltaOutputJson = JsonSerializer.Serialize((object)input.Delta.AbsoluteDelta, JsonOptions);
        string percentageDeltaOutputJson = JsonSerializer.Serialize((object)input.Delta.PercentageDelta, JsonOptions);

        string rawDataToHash = string.Concat(initialOutputJson, finalOutputJson, absoluteDeltaOutputJson, percentageDeltaOutputJson);
        string identifierHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawDataToHash)));

        MechanicalModelSimulationEntity simulationEntity = new()
        {
            Identifier = identifierHash,
            CurveFitIdentifier = input.CurveFitIdentifier,
            MechanicalModelName = input.CurveFit.MechanicalModelName,
            OutputFileName = input.FileData.Name,
            AsymptoteTime = input.AsymptoteTime,
            InitialOutputJson = initialOutputJson,
            FinalOutputJson = finalOutputJson,
            AbsoluteDeltaOutputJson = absoluteDeltaOutputJson,
            PercentageDeltaOutputJson = percentageDeltaOutputJson
        };

        await repository.TryInsertAsync(simulationEntity, cancellationToken).ConfigureAwait(false);

        return input.CurveFit;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
