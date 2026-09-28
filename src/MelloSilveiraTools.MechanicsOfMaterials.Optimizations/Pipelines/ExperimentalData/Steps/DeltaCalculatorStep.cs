using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that computes the absolute and percentage deltas from the simulated points array.
/// </summary>
public class DeltaCalculatorStep : ISyncPipelineStep<MechanicalModelSimulationPayload, SimulationDeltaOutput>
{
    /// <inheritdoc />
    public string Name => nameof(DeltaCalculatorStep);

    /// <inheritdoc />
    public SimulationDeltaOutput Execute(MechanicalModelSimulationPayload payload)
    {
        MechanicalModelOutput[] points = payload.SimulatedPoints;

        MechanicalModelOutput initialOutput = points.Length > 0 ? points[0] : new MechanicalModelOutput();
        MechanicalModelOutput finalOutput = points.Length > 0 ? points[^1] : new MechanicalModelOutput();

        MechanicalModelOutput absoluteDelta = finalOutput.CalculateDelta(initialOutput);
        MechanicalModelOutput percentageDelta = finalOutput.CalculatePercentageDelta(initialOutput);

        return new SimulationDeltaOutput(initialOutput, finalOutput, absoluteDelta, percentageDelta);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
