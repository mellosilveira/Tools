using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Handlers;

/// <summary>
/// Stream handler that records initial and final simulation outputs to compute absolute and relative percentage deltas.
/// </summary>
public class SimulationDeltaHandler : ISimulationStreamHandler<SimulationDeltaResult>
{
    private MechanicalModelOutput? _initialOutput;
    private MechanicalModelOutput? _lastOutput;

    /// <inheritdoc />
    public ValueTask OnNextAsync(MechanicalModelOutput output, int index)
    {
        if (index == 0)
        {
            _initialOutput = output;
        }

        // Retain the latest output to compute delta against the initial baseline upon completion
        _lastOutput = output;
        
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<SimulationDeltaResult> CompleteAsync()
    {
        MechanicalModelOutput finalOutput = _lastOutput ?? _initialOutput!;
        _initialOutput ??= finalOutput;

        MechanicalModelOutput absoluteDelta = finalOutput.CalculateDelta(_initialOutput);
        MechanicalModelOutput percentageDelta = finalOutput.CalculatePercentageDelta(_initialOutput);

        return ValueTask.FromResult(new SimulationDeltaResult(
            _initialOutput,
            finalOutput,
            absoluteDelta,
            percentageDelta
        ));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
