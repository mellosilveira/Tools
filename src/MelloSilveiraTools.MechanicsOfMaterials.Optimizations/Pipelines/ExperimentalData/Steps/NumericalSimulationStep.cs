using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that executes the full forward numerical simulation, generating an array of mechanical output points.
/// </summary>
public sealed class NumericalSimulationStep(
    IMechanicalModelCalculatorFactory calculatorFactory,
    double? simulationTimeStep = null,
    double? finalSimulationTime = null)
    : ISyncPipelineStep<MechanicalModelCurveFitOutput, MechanicalModelSimulationPayload>
{
    /// <inheritdoc />
    public string Name => nameof(NumericalSimulationStep);

    /// <inheritdoc />
    public MechanicalModelSimulationPayload Execute(MechanicalModelCurveFitOutput curveFit)
    {
        double initialStrain = curveFit.AcceptedRange.InitialPoint;
        double finalStrain = curveFit.AcceptedRange.FinalPoint;

        double? rampTime = curveFit.RampTimeConsideration == RampTimeConsideration.ConsiderWithoutViscoelasticEffect ? curveFit.CurveSegments.GetFirstRampTime() : null;

        double timeStep = simulationTimeStep ?? CustomMath.CalculateAverageTimeStep(curveFit.CurveSegments.GetTimePoints());
        double targetFinalTime = finalSimulationTime ?? curveFit.CurveSegments[^1].TimePoints[^1];

        GenericMechanicalModelInput genericInput = new()
        {
            MechanicalModelName = curveFit.MechanicalModelName,
            AcceptedStrainRange = curveFit.AcceptedRange,
            LoadResponseRelationship = curveFit.LoadResponseRelationship,
            ViscoelasticEffect = curveFit.ViscoelasticEffect,
            RampTimeConsideration = curveFit.RampTimeConsideration,
            RampTime = rampTime,
            Strain = rampTime.HasValue && rampTime.Value > 0
                ? new MechanicalParameter(initialStrain, new PolynomialFunction([0, (finalStrain - initialStrain) / rampTime.Value]))
                : new MechanicalParameter(initialStrain),
            Stress = new MechanicalParameter(curveFit.CurveSegments[0].ExperimentalStress[0]),
            TimeStep = timeStep,
            ConstitutiveParameters = curveFit.ConstitutiveParameters,
        };

        List<double> times = [];
        foreach (CurveSegment segment in curveFit.CurveSegments)
        {
            times.AddRange(segment.TimePoints);
        }

        if (finalSimulationTime.HasValue && times.Count > 0 && targetFinalTime > times[^1])
        {
            double lastTime = times[^1];
            for (double t = lastTime + timeStep; t <= targetFinalTime + (timeStep * 0.01); t += timeStep)
            {
                times.Add(t);
            }
        }

        IMechanicalModelCalculatorFacade facade = calculatorFactory.CreateCalculatorFacade(genericInput);

        MechanicalModelOutput[] points = new MechanicalModelOutput[times.Count];
        for (int i = 0; i < times.Count; i++)
        {
            points[i] = facade.Calculate(times[i]);
        }

        return new MechanicalModelSimulationPayload(curveFit, points);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
