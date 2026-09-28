using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Converters;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Text.Json;
using MelloSilveiraTools.Core.Managers.File;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that converges all simulation analysis branches (Identifier, Asymptote, Delta, CSV) 
/// and persists the final simulation entity to the database.
/// </summary>
public class MechanicalModelOutputPersistenceStep(IRepository repository)
    : IAsyncPipelineStep<Tuple<ValueTuple<string, MechanicalModelCurveFitOutput>, Tuple<double?, SimulationDeltaResult, FileData>>, MechanicalModelCurveFitOutput>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { Converters = { new SignificantFiguresDoubleJsonConverter(7) } };

    /// <inheritdoc />
    public string Name => nameof(MechanicalModelOutputPersistenceStep);

    /// <inheritdoc />
    public async Task<MechanicalModelCurveFitOutput> ExecuteAsync(Tuple<ValueTuple<string, MechanicalModelCurveFitOutput>, Tuple<double?, SimulationDeltaResult, FileData>> input, CancellationToken cancellationToken)
    {
        string identifier = input.Item1.Item1;
        MechanicalModelCurveFitOutput curveFit = input.Item1.Item2;
        
        double? asymptoteTime = input.Item2.Item1;
        SimulationDeltaResult delta = input.Item2.Item2;
        FileData fileData = input.Item2.Item3;

        MechanicalModelSimulationEntity simulationEntity = new()
        {
            Identifier = identifier,
            CurveFitIdentifier = identifier,
            MechanicalModelName = curveFit.MechanicalModelName,
            OutputFileName = fileData.Name,
            AsymptoteTime = asymptoteTime,
            InitialOutputJson = JsonSerializer.Serialize((object)delta.InitialOutput, JsonOptions),
            FinalOutputJson = JsonSerializer.Serialize((object)delta.FinalOutput, JsonOptions),
            AbsoluteDeltaOutputJson = JsonSerializer.Serialize((object)delta.AbsoluteDelta, JsonOptions),
            PercentageDeltaOutputJson = JsonSerializer.Serialize((object)delta.PercentageDelta, JsonOptions)
        };

        await repository.TryInsertAsync(simulationEntity, cancellationToken).ConfigureAwait(false);

        return curveFit;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
