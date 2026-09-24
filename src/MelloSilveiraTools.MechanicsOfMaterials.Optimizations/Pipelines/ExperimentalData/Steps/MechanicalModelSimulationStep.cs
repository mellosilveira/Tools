using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Converters;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step responsible for performing numerical forward simulations on fitted mechanical constitutive models.
/// Streams results to CSV, detects asymptote convergence, calculates complete absolute and percentage deltas,
/// persists simulation entities, and attaches simulation output.
/// </summary>
public class MechanicalModelSimulationStep(
    IFileManager fileManager,
    IMechanicalModelCalculatorFactory calculatorFactory,
    IRepository repository,
    string outputFileUri,
    string simulationIdentifierPrefix,
    double? finalSimulationTime = null,
    double? simulationTimeStep = null)
    : IAsyncPipelineStep<MechanicalModelCurveFitOutput, MechanicalModelCurveFitOutput>
{
    private const int LargeFileBufferSize = 128 * 1024;
    private const int AsymptoteConsecutivePointsThreshold = 10;
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);
    private static readonly FileStreamOptions LargeFileStreamOptions = new()
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write,
        Share = FileShare.None,
        Options = FileOptions.SequentialScan | FileOptions.Asynchronous,
        BufferSize = LargeFileBufferSize
    };
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new SignificantFiguresDoubleJsonConverter(7) }
    };

    /// <inheritdoc />
    public string Name => nameof(MechanicalModelSimulationStep);

    /// <inheritdoc />
    public async Task<MechanicalModelCurveFitOutput> ExecuteAsync(MechanicalModelCurveFitOutput output, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        double initialStrain = output.AcceptedRange.InitialPoint;
        double finalStrain = output.AcceptedRange.FinalPoint;

        GenericMechanicalModelInput genericInput = new()
        {
            MechanicalModelName = output.MechanicalModelName,
            AcceptedStrainRange = output.AcceptedRange,
            MechanicalBehaviorType = output.MechanicalBehaviorType,
            ViscoelasticEffect = output.ViscoelasticEffect,
            RampTimeConsideration = output.RampTimeConsideration,
            RampTime = output.RampTime,
            Strain = output.RampTime.HasValue && output.RampTime.Value > 0
                ? new MechanicalParameter(initialStrain, new PolynomialFunction([0, (finalStrain - initialStrain) / output.RampTime.Value]))
                : new MechanicalParameter(initialStrain),
            Stress = new MechanicalParameter(output.ExperimentalStress is { Length: > 0 } ? output.ExperimentalStress[0] : 0.0),
            TimeStep = output.TimeStep > 0 ? output.TimeStep : 0.01,
            ConstitutiveParameters = output.ConstitutiveParameters
        };

        IMechanicalModelCalculatorFacade facade = calculatorFactory.CreateCalculatorFacade(genericInput);

        string fileIdentifier = $"{simulationIdentifierPrefix}_{output.MechanicalModelName}_{output.Identifier}";
        FileInfo fileInfo = fileManager.BuildTimebasedFileInfo(outputFileUri, fileIdentifier, FileExtensions.CommaSeparatedValues);
        string outputFullFileName = fileInfo.FullName;

        List<double> simulationTimes = ResolveSimulationTimes(output.TimePoints, output.TimeStep, finalSimulationTime, simulationTimeStep);

        FileStream stream = fileInfo.Open(LargeFileStreamOptions);
        StreamWriter writer = new(stream, Utf8Encoding, LargeFileBufferSize);

        MechanicalModelOutput? initialOutput = null;
        MechanicalModelOutput? finalOutput = null;
        MechanicalModelOutput? lastOutput = null;
        double? asymptoteTime = null;
        int consecutiveEqualPoints = 0;
        PropertyInfo[]? activeProperties = null;

        await using (writer.ConfigureAwait(false))
        {
            for (int i = 0; i < simulationTimes.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                double time = simulationTimes[i];
                MechanicalModelOutput currentOutput = facade.Calculate(time);

                if (i == 0)
                {
                    initialOutput = currentOutput;
                    activeProperties = DiscoverActiveProperties(currentOutput.GetType(), output.MechanicalBehaviorType, output.ViscoelasticEffect);
                    await WriteHeaderAsync(writer, activeProperties).ConfigureAwait(false);
                }

                await WriteRowAsync(writer, currentOutput, activeProperties!).ConfigureAwait(false);

                if (asymptoteTime is null && lastOutput is not null)
                {
                    if (IsAsymptoteReached(lastOutput, currentOutput, output.ViscoelasticEffect))
                    {
                        consecutiveEqualPoints++;
                        if (consecutiveEqualPoints >= AsymptoteConsecutivePointsThreshold)
                        {
                            asymptoteTime = time;
                        }
                    }
                    else
                    {
                        consecutiveEqualPoints = 0;
                    }
                }

                lastOutput = currentOutput;
            }

            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        finalOutput = lastOutput ?? initialOutput!;
        initialOutput ??= finalOutput;

        MechanicalModelOutput absoluteDelta = finalOutput.CalculateDelta(initialOutput);
        MechanicalModelOutput percentageDelta = finalOutput.CalculatePercentageDelta(initialOutput);

        string initialOutputJson = JsonSerializer.Serialize((object)initialOutput, JsonOptions);
        string finalOutputJson = JsonSerializer.Serialize((object)finalOutput, JsonOptions);
        string absoluteDeltaOutputJson = JsonSerializer.Serialize((object)absoluteDelta, JsonOptions);
        string percentageDeltaOutputJson = JsonSerializer.Serialize((object)percentageDelta, JsonOptions);

        string simulationIdentifier = ComputeSha256Hash($"{output.Identifier}_{finalOutput.Time}_{asymptoteTime}");

        MechanicalModelSimulationEntity simulationEntity = new()
        {
            Identifier = simulationIdentifier,
            CurveFitIdentifier = output.Identifier,
            MechanicalModelName = output.MechanicalModelName,
            OutputFileName = Path.GetFileName(outputFullFileName),
            AsymptoteTime = asymptoteTime,
            InitialOutputJson = initialOutputJson,
            FinalOutputJson = finalOutputJson,
            AbsoluteDeltaOutputJson = absoluteDeltaOutputJson,
            PercentageDeltaOutputJson = percentageDeltaOutputJson
        };

        await repository.TryInsertAsync(simulationEntity, cancellationToken).ConfigureAwait(false);

        MechanicalModelSimulationOutput simulationOutput = new(
            Identifier: simulationIdentifier,
            CurveFitIdentifier: output.Identifier,
            OutputFullFileName: outputFullFileName,
            InitialOutput: initialOutput,
            FinalOutput: finalOutput,
            AbsoluteDeltaOutput: absoluteDelta,
            PercentageDeltaOutput: percentageDelta,
            AsymptoteTime: asymptoteTime);

        return output with { Simulation = simulationOutput };
    }

    private static List<double> ResolveSimulationTimes(double[]? timePoints, double timeStep, double? finalSimulationTime, double? simulationTimeStep)
    {
        List<double> times = [];

        if (timePoints is { Length: > 0 })
        {
            times.AddRange(timePoints);
        }
        else
        {
            double end = finalSimulationTime ?? 10.0;
            double dt = timeStep > 0 ? timeStep : 0.01;
            for (double t = 0; t <= end + (dt * 0.01); t += dt)
            {
                times.Add(t);
            }
        }

        if (finalSimulationTime.HasValue && times.Count > 0 && finalSimulationTime.Value > times[^1])
        {
            double step = simulationTimeStep ?? (timeStep > 0 ? timeStep : 0.01);
            if (step <= 0)
            {
                step = 0.01;
            }

            double lastTime = times[^1];
            for (double t = lastTime + step; t <= finalSimulationTime.Value + (step * 0.01); t += step)
            {
                times.Add(t);
            }
        }

        return times;
    }

    private static PropertyInfo[] DiscoverActiveProperties(Type outputType, MechanicalBehaviorType behavior, ViscoelasticEffect effect)
    {
        PropertyInfo[] allProps = outputType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        List<PropertyInfo> matching = [];

        for (int i = 0; i < allProps.Length; i++)
        {
            PropertyInfo prop = allProps[i];
            if (prop.Name == nameof(MechanicalModelOutput.Time))
            {
                continue;
            }

            MechanicalModelParameterAttribute? attr = prop.GetCustomAttribute<MechanicalModelParameterAttribute>();
            if (attr is not null && attr.CanMethodBeInvoked(behavior, effect))
            {
                matching.Add(prop);
            }
        }

        return [.. matching];
    }

    private static async Task WriteHeaderAsync(StreamWriter writer, PropertyInfo[] properties)
    {
        StringBuilder sb = new();
        sb.Append(nameof(MechanicalModelOutput.Time));

        for (int i = 0; i < properties.Length; i++)
        {
            sb.Append(',');
            sb.Append(properties[i].Name);
        }

        await writer.WriteLineAsync(sb.ToString()).ConfigureAwait(false);
    }

    private static async Task WriteRowAsync(StreamWriter writer, MechanicalModelOutput output, PropertyInfo[] properties)
    {
        StringBuilder sb = new();
        sb.Append(output.Time.ToString(CultureInfo.InvariantCulture));

        for (int i = 0; i < properties.Length; i++)
        {
            sb.Append(',');
            object? val = properties[i].GetValue(output);
            if (val is double d)
            {
                sb.Append(d.ToString(CultureInfo.InvariantCulture));
            }
            else if (val is not null)
            {
                sb.Append(Convert.ToString(val, CultureInfo.InvariantCulture));
            }
        }

        await writer.WriteLineAsync(sb.ToString()).ConfigureAwait(false);
    }

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

    private static string ComputeSha256Hash(string rawData)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
