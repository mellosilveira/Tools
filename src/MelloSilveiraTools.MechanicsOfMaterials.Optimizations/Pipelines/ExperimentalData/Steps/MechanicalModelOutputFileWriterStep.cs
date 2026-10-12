using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.Common;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that serializes the full array of simulated output points into a CSV file.
/// </summary>
/// <param name="fileManager">File manager used to resolve the destination file path.</param>
/// <param name="outputFileUri">Target destination directory URI.</param>
/// <param name="uniqueIdentifier">Unique business identifier prefix for the generated file.</param>
public class MechanicalModelOutputFileWriterStep(
    IFileManager fileManager,
    string outputFileUri,
    string uniqueIdentifier)
    : BaseCsvWriterStep, IAsyncPipelineStep<MechanicalModelSimulationPayload, FileData>
{
    private readonly List<string> _generatedFilePaths = [];

    /// <inheritdoc />
    public override string Name => nameof(MechanicalModelOutputFileWriterStep);

    /// <summary>
    /// Gets the full name of the generated CSV file, or empty string if no simulation has been processed yet.
    /// </summary>
    public override string OutputFullFileName => _generatedFilePaths.Count > 0 ? _generatedFilePaths[^1] : string.Empty;

    /// <inheritdoc />
    public override IReadOnlyCollection<string> GeneratedFilePaths => [.. _generatedFilePaths];

    /// <inheritdoc />
    public async Task<FileData> ExecuteAsync(MechanicalModelSimulationPayload payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fileIdentifier = $"{uniqueIdentifier}_{payload.CurveFit.MechanicalModelName}";
        FileInfo fileInfo = fileManager.BuildTimebasedFileInfo(outputFileUri, fileIdentifier, FileExtensions.CommaSeparatedValues);
        _generatedFilePaths.Add(fileInfo.FullName);

        PropertyInfo[] activeProperties = DiscoverActiveProperties(typeof(MechanicalModelOutput), payload.CurveFit.LoadResponseRelationship, payload.CurveFit.ViscoelasticEffect);

        Func<MechanicalModelOutput, double?>[] getters = new Func<MechanicalModelOutput, double?>[activeProperties.Length];
        for (int i = 0; i < activeProperties.Length; i++)
        {
            getters[i] = (Func<MechanicalModelOutput, double?>)Delegate.CreateDelegate(typeof(Func<MechanicalModelOutput, double?>), activeProperties[i].GetGetMethod()!);
        }

        StringBuilder headerBuilder = new();
        headerBuilder.Append(nameof(MechanicalModelOutput.Time));
        for (int i = 0; i < activeProperties.Length; i++)
        {
            headerBuilder.Append(Delimiter).Append(activeProperties[i].Name);
        }
        string header = headerBuilder.ToString();

        await using StreamWriter writer = CreateWriter(fileInfo.FullName);
        await writer.WriteLineAsync(header).ConfigureAwait(false);

        StringBuilder buffer = new(BufferCapacity);
        int batchCount = 0;
        MechanicalModelOutput[] points = payload.SimulatedPoints;
        for (int i = 0; i < points.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            MechanicalModelOutput point = points[i];
            buffer.Append(point.Time.ToString(CultureInfo.InvariantCulture));
            for (int p = 0; p < getters.Length; p++)
            {
                buffer.Append(Delimiter);
                double? val = getters[p](point);
                if (val.HasValue)
                {
                    buffer.Append(val.Value.ToString(CultureInfo.InvariantCulture));
                }
            }
            buffer.Append(Environment.NewLine);

            if (++batchCount >= BatchThreshold)
            {
                await writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                buffer.Clear();
                batchCount = 0;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (buffer.Length > 0)
        {
            await writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            buffer.Clear();
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

        return new FileData(fileInfo);
    }

    private static PropertyInfo[] DiscoverActiveProperties(Type outputType, LoadResponseRelationship relationship, ViscoelasticEffect effect)
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
            if (attr is not null && attr.CanMethodBeInvoked(relationship, effect))
            {
                matching.Add(prop);
            }
        }

        return [.. matching];
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
