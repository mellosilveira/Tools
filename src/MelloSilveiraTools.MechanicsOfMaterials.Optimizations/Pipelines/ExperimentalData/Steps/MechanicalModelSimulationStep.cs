using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Files;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Collections.Concurrent;
using System.Reflection;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Unified pipeline step that executes the forward numerical simulation across experimental curve segments,
/// computes absolute and percentage stress deltas, monitors asymptotic steady-state convergence,
/// and streams the full time-history simulation results to persistent CSV storage.
/// </summary>
/// <param name="calculatorFactory">Factory providing calculator facades for specific mechanical formulations.</param>
/// <param name="fileStorageService">Storage service responsible for resolving paths and opening file streams.</param>
/// <param name="pipelineIdentifier">Unique business identifier prefix for the generated CSV simulation file.</param>
/// <param name="simulationTimeStep">Optional explicit simulation time increment (Δt). If omitted, the average experimental time step is computed.</param>
/// <param name="finalSimulationTime">Optional target final simulation time for extrapolation beyond the experimental regime.</param>
/// <param name="asymptoteConsecutivePointsThreshold">Number of consecutive stable points required to detect steady-state convergence.</param>
public sealed class MechanicalModelSimulationStep(
    string pipelineIdentifier,
    IFileStorageService fileStorageService,
    IMechanicalModelTypeResolver mechanicalModelTypeResolver,
    IMechanicalModelCalculatorFactory calculatorFactory,
    double? simulationTimeStep = null,
    double? finalSimulationTime = null,
    int asymptoteConsecutivePointsThreshold = 10)
    : IAsyncPipelineStep<MechanicalModelCurveFitOutput, (double? AsymptoteTime, SimulationDeltaOutput DeltaOutput, StorageEntryMetadata OutputFileMetadata)>
{
    private static readonly ConcurrentDictionary<(Type OutputType, LoadResponseRelationship Relationship, ViscoelasticEffect Effect), (string Header, Func<MechanicalModelOutput, double?>[] Getters)> SchemaCache = new();

    /// <inheritdoc />
    public string Name => nameof(MechanicalModelSimulationStep);

    /// <inheritdoc />
    public async Task<(double? AsymptoteTime, SimulationDeltaOutput DeltaOutput, StorageEntryMetadata OutputFileMetadata)> ExecuteAsync(MechanicalModelCurveFitOutput curveFit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        double timeStep = simulationTimeStep ?? CustomMath.CalculateAverageTimeStep(curveFit.CurveSegments.GetTimePoints());
        GenericMechanicalModelInput baseInput = new()
        {
            MechanicalModelName = mechanicalModelTypeResolver.MechanicalModelName,
            AcceptedStrainRange = curveFit.AcceptedRange,
            LoadResponseRelationship = curveFit.LoadResponseRelationship,
            ViscoelasticEffect = curveFit.ViscoelasticEffect,
            RampTimeConsideration = curveFit.RampTimeConsideration,
            RampTime = curveFit.CurveSegments.GetFirstRampTime(),
            TimeStep = timeStep,
            ConstitutiveParameters = curveFit.ConstitutiveParameters
        };

        MechanicalModelOutput initialOutput = null!;
        MechanicalModelOutput lastOutput = null!;
        double? asymptoteTime = null;

        (string location, MechanicalModelOutputCsvFileWriter writer) = await MechanicalModelOutputCsvFileWriter
            .CreateAsync(pipelineIdentifier, fileStorageService, mechanicalModelTypeResolver, curveFit.LoadResponseRelationship, curveFit.ViscoelasticEffect, curveFit.RampTimeConsideration)
            .ConfigureAwait(false);

        await using (writer)
        {
            IMechanicalModelCalculatorFacade facade = null!;
            foreach (CurveSegment segment in curveFit.CurveSegments)
            {
                GenericMechanicalModelInput input = baseInput with { Strain = BuildStrainParameter(segment), Stress = new MechanicalParameter(segment.ExperimentalStress[0]) };
                facade = calculatorFactory.CreateCalculatorFacade(mechanicalModelTypeResolver, input);
                lastOutput = facade.Calculate(segment.TimePoints[0]);

                if (segment.Type is SegmentType.Relaxation or SegmentType.Recovery)
                    initialOutput = lastOutput;

                int asymptoteCount = 0;
                for (int i = 1; i < segment.TimePoints.Length; i++)
                {
                    double time = segment.TimePoints[i];
                    MechanicalModelOutput output = facade.Calculate(time);
                    if (HasReachedAsymptote(lastOutput, output, ref asymptoteCount))
                        asymptoteTime = time;

                    await writer.WriteLineAsync(output).ConfigureAwait(false);
                    lastOutput = output;
                }
            }

            if (finalSimulationTime is not null)
            {
                int asymptoteCount = 0;
                double time = curveFit.CurveSegments[^1].TimePoints[^1];
                while (time < finalSimulationTime + timeStep)
                {
                    MechanicalModelOutput output = facade.Calculate(time);
                    if (asymptoteTime is not null && HasReachedAsymptote(lastOutput, output, ref asymptoteCount))
                        asymptoteTime = time;

                    await writer.WriteLineAsync(output).ConfigureAwait(false);
                    lastOutput = output;
                }
            }
        }

        SimulationDeltaOutput simulationDeltaOutput = CalculateDelta(initialOutput, lastOutput);
        StorageEntryMetadata outputFileMetadata = await fileStorageService.GetMetadataAsync(location, cancellationToken).ConfigureAwait(false);

        return (asymptoteTime, simulationDeltaOutput, outputFileMetadata);
    }

    private SimulationDeltaOutput CalculateDelta(MechanicalModelOutput initialOutput, MechanicalModelOutput lastOutput)
    {
        MechanicalModelOutput absoluteDelta = new();
        MechanicalModelOutput percentageDelta = new();

        foreach (PropertyInfo property in mechanicalModelTypeResolver.Output.GetPropertiesInHierarchy())
        {
            double? initialValue = (double?)property.GetValue(initialOutput);
            double? lastValue = (double?)property.GetValue(lastOutput);

            if (initialValue.HasValue && lastValue.HasValue)
            {
                double absoluteDeltaValue = lastValue.Value - initialValue.Value;
                property.SetValue(absoluteDelta, absoluteDeltaValue);
                property.SetValue(percentageDelta, 100 * absoluteDeltaValue / initialValue.Value);
            }
        }

        return new SimulationDeltaOutput(initialOutput, lastOutput, absoluteDelta, percentageDelta);
    }

    private bool HasReachedAsymptote(MechanicalModelOutput lastOutput, MechanicalModelOutput output, ref int asymptoteCount)
    {
        asymptoteCount = lastOutput == output ? asymptoteCount + 1 : 1;
        return asymptoteCount == asymptoteConsecutivePointsThreshold;
    }

    private static MechanicalParameter BuildStrainParameter(CurveSegment segment)
    {
        double initialStrain = segment.ExperimentalStrain.Length > 0 ? segment.ExperimentalStrain[0] : 0.0;

        if (segment.Type is SegmentType.Ramp or SegmentType.Descent)
        {
            double strainRate = (segment.ExperimentalStrain[^1] - initialStrain) / (segment.TimePoints[^1] - segment.TimePoints[0]);
            return new MechanicalParameter(initialStrain, new PolynomialFunction([0, strainRate]));
        }

        return new MechanicalParameter(initialStrain);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
