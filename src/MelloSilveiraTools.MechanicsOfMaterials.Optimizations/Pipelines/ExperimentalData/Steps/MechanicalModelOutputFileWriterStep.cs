using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that serializes the full array of simulated output points into a CSV file.
/// </summary>
public class MechanicalModelOutputFileWriterStep(
    IFileManager fileManager,
    string outputFileUri,
    string uniqueIdentifier)
    : IAsyncPipelineStep<MechanicalModelSimulationPayload, FileData>
{
    /// <inheritdoc />
    public string Name => nameof(MechanicalModelOutputFileWriterStep);

    /// <inheritdoc />
    public async Task<FileData> ExecuteAsync(MechanicalModelSimulationPayload payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fileIdentifier = $"{uniqueIdentifier}_{payload.CurveFit.MechanicalModelName}";
        FileInfo fileInfo = fileManager.BuildTimebasedFileInfo(outputFileUri, fileIdentifier, FileExtensions.CommaSeparatedValues);

        await using FileStream stream = fileInfo.Open(new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.SequentialScan,
            BufferSize = 128 * 1024
        });

        await using StreamWriter writer = new(stream, new UTF8Encoding(false), 128 * 1024);

        PropertyInfo[] activeProperties = DiscoverActiveProperties(typeof(MechanicalModelOutput), payload.CurveFit.MechanicalBehaviorType, payload.CurveFit.ViscoelasticEffect);

        var getters = new Func<MechanicalModelOutput, double?>[activeProperties.Length];
        for (int i = 0; i < activeProperties.Length; i++)
        {
            getters[i] = (Func<MechanicalModelOutput, double?>)Delegate.CreateDelegate(typeof(Func<MechanicalModelOutput, double?>), activeProperties[i].GetGetMethod()!);
        }

        StringBuilder header = new();
        header.Append(nameof(MechanicalModelOutput.Time));
        for (int i = 0; i < activeProperties.Length; i++)
        {
            header.Append(',').Append(activeProperties[i].Name);
        }
        await writer.WriteLineAsync(header.ToString()).ConfigureAwait(false);

        MechanicalModelOutput[] points = payload.SimulatedPoints;
        for (int i = 0; i < points.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            writer.Write(points[i].Time.ToString(CultureInfo.InvariantCulture));
            for (int p = 0; p < getters.Length; p++)
            {
                writer.Write(',');
                double? val = getters[p](points[i]);
                if (val.HasValue)
                {
                    writer.Write(val.Value.ToString(CultureInfo.InvariantCulture));
                }
            }
            await writer.WriteLineAsync().ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await writer.FlushAsync().ConfigureAwait(false);

        return new FileData(fileInfo);
    }

    private static PropertyInfo[] DiscoverActiveProperties(Type outputType, MechanicalBehaviorType behavior, ViscoelasticEffect effect)
    {
        PropertyInfo[] allProps = outputType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        List<PropertyInfo> matching = [];

        for (int i = 0; i < allProps.Length; i++)
        {
            PropertyInfo prop = allProps[i];
            if (prop.Name == nameof(MechanicalModelOutput.Time)) continue;

            MechanicalModelParameterAttribute? attr = prop.GetCustomAttribute<MechanicalModelParameterAttribute>();
            if (attr is not null && attr.CanMethodBeInvoked(behavior, effect))
            {
                matching.Add(prop);
            }
        }

        return [.. matching];
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
