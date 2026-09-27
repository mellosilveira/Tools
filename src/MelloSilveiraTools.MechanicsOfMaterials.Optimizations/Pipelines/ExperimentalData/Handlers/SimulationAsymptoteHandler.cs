using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Handlers;

/// <summary>
/// Stream handler that monitors consecutive output points to detect the time at which the material response reaches an asymptote / steady-state.
/// </summary>
/// <param name="viscoelasticEffect">The active viscoelastic effect (Relaxation or Creep) used to select the monitored variable.</param>
/// <param name="asymptoteConsecutivePointsThreshold">The number of consecutive stabilized points required to identify an asymptote. Defaults to 10.</param>
public class SimulationAsymptoteHandler(ViscoelasticEffect viscoelasticEffect, int asymptoteConsecutivePointsThreshold = 10) : ISimulationStreamHandler<double?>
{
    private readonly int _asymptoteConsecutivePointsThreshold = asymptoteConsecutivePointsThreshold;
    private MechanicalModelOutput? _previousOutput;
    private int _consecutiveEqualPoints;
    private double? _asymptoteTime;

    /// <inheritdoc />
    public ValueTask OnNextAsync(MechanicalModelOutput output, int index)
    {
        // Once the asymptote has been identified, subsequent points do not need re-evaluation
        if (_asymptoteTime is null && _previousOutput is not null)
        {
            if (IsAsymptoteReached(_previousOutput, output, viscoelasticEffect))
            {
                _consecutiveEqualPoints++;
                if (_consecutiveEqualPoints >= _asymptoteConsecutivePointsThreshold)
                {
                    _asymptoteTime = output.Time;
                }
            }
            else
            {
                _consecutiveEqualPoints = 0;
            }
        }

        _previousOutput = output;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<double?> CompleteAsync() => ValueTask.FromResult(_asymptoteTime);

    /// <summary>
    /// Checks whether two consecutive outputs have stabilized within numerical tolerance for the specified viscoelastic mode.
    /// </summary>
    private static bool IsAsymptoteReached(MechanicalModelOutput previous, MechanicalModelOutput current, ViscoelasticEffect effect)
    {
        if (effect == ViscoelasticEffect.Relaxation)
        {
            if (previous.Stress.HasValue && current.Stress.HasValue)
            {
                return current.Stress.Value.EqualsWithTolerance(previous.Stress.Value);
            }
        }
        else if (effect == ViscoelasticEffect.Creep)
        {
            if (previous.Strain.HasValue && current.Strain.HasValue)
            {
                return current.Strain.Value.EqualsWithTolerance(previous.Strain.Value);
            }
        }

        return current.Equals(previous);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
