using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step responsible for transforming curve fitting output into the input payload required by the forward numerical simulation step.
/// Extracts strain/stress boundaries, determines loading ramp time, calculates the simulation time step, and prepares the time series.
/// </summary>
/// <param name="simulationTimeStep">Optional user-defined time step. If omitted, computes the average time step from experimental curve segments.</param>
/// <param name="finalSimulationTime">Optional extended simulation horizon time.</param>
public sealed class SimulationInputBuilderStep(double? simulationTimeStep = null, double? finalSimulationTime = null)
    : ISyncPipelineStep<(string CurveFitIdentifier, MechanicalModelCurveFitOutput CurveFit), MechanicalModelSimulationStepInput>
{
    /// <inheritdoc />
    public string Name => nameof(SimulationInputBuilderStep);

    /// <inheritdoc />
    public MechanicalModelSimulationStepInput Execute((string CurveFitIdentifier, MechanicalModelCurveFitOutput CurveFit) input)
    {
        (string curveFitIdentifier, MechanicalModelCurveFitOutput curveFit) = input;

        double initialStrain = curveFit.AcceptedRange.InitialPoint;
        double finalStrain = curveFit.AcceptedRange.FinalPoint;

        // Resolve ramp time if the model specifies ConsiderWithoutViscoelasticEffect
        double? rampTime = curveFit.RampTimeConsideration == RampTimeConsideration.ConsiderWithoutViscoelasticEffect ? curveFit.CurveSegments.GetFirstRampTime() : null;

        // Use custom time step if provided; otherwise compute the average delta across experimental segments
        double timeStep = simulationTimeStep ?? CustomMath.CalculateAverageTimeStep(curveFit.CurveSegments.GetTimePoints());
        double targetFinalTime = finalSimulationTime ?? curveFit.CurveSegments[^1].TimePoints[^1];

        GenericMechanicalModelInput genericInput = new()
        {
            MechanicalModelName = curveFit.MechanicalModelName,
            AcceptedStrainRange = curveFit.AcceptedRange,
            MechanicalBehaviorType = curveFit.MechanicalBehaviorType,
            ViscoelasticEffect = curveFit.ViscoelasticEffect,
            RampTimeConsideration = curveFit.RampTimeConsideration,
            RampTime = rampTime,
            Strain = rampTime.HasValue && rampTime.Value > 0
                ? new MechanicalParameter(initialStrain, new PolynomialFunction([0, (finalStrain - initialStrain) / rampTime.Value]))
                : new MechanicalParameter(initialStrain),
            Stress = new MechanicalParameter(curveFit.CurveSegments[0].ExperimentalStress[0]),
            TimeStep = timeStep,
            ConstitutiveParameters = curveFit.ConstitutiveParameters
        };

        // Reconstruct the unified time points array across all segments
        List<double> times = [];
        foreach (CurveSegment segment in curveFit.CurveSegments)
        {
            times.AddRange(segment.TimePoints);
        }

        // Extrapolate time points if an extended final simulation time is specified
        if (finalSimulationTime.HasValue && times.Count > 0 && targetFinalTime > times[^1])
        {
            double lastTime = times[^1];
            for (double t = lastTime + timeStep; t <= targetFinalTime + (timeStep * 0.01); t += timeStep)
            {
                times.Add(t);
            }
        }

        return new MechanicalModelSimulationStepInput(
            curveFitIdentifier,
            curveFit,
            genericInput,
            [.. times]
        );
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
