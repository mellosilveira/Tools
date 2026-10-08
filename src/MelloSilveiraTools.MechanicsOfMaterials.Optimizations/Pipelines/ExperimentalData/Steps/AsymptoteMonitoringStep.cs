using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that monitors the simulated points array for asymptotic behavior (steady-state).
/// </summary>
public class AsymptoteMonitoringStep(int asymptoteConsecutivePointsThreshold = 10) : ISyncPipelineStep<MechanicalModelSimulationPayload, double?>
{
    /// <inheritdoc />
    public string Name => nameof(AsymptoteMonitoringStep);

    /// <inheritdoc />
    public double? Execute(MechanicalModelSimulationPayload payload)
    {
        ViscoelasticEffect effect = payload.CurveFit.ViscoelasticEffect;
        MechanicalModelOutput[] points = payload.SimulatedPoints;

        int consecutiveEqualPoints = 0;

        for (int i = 1; i < points.Length; i++)
        {
            if (IsAsymptoteReached(points[i - 1], points[i], effect))
            {
                consecutiveEqualPoints++;
                if (consecutiveEqualPoints >= asymptoteConsecutivePointsThreshold)
                {
                    return points[i].Time;
                }
            }
            else
            {
                consecutiveEqualPoints = 0;
            }
        }

        return null;
    }

    private static bool IsAsymptoteReached(MechanicalModelOutput previous, MechanicalModelOutput current, ViscoelasticEffect effect)
    {
        if (effect == ViscoelasticEffect.Relaxation)
        {
            if (previous.Stress.HasValue && current.Stress.HasValue)
                return current.Stress.Value.EqualsWithTolerance(previous.Stress.Value);
        }
        else if (effect == ViscoelasticEffect.Creep)
        {
            if (previous.Strain.HasValue && current.Strain.HasValue)
                return current.Strain.Value.EqualsWithTolerance(previous.Strain.Value);
        }

        return current.Equals(previous);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
