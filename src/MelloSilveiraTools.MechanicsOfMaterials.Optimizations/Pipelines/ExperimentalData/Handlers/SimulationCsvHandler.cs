using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Handlers;

/// <summary>
/// Stream handler responsible for serializing calculated <see cref="MechanicalModelOutput"/> points to a CSV file on disk.
/// Automatically detects active parameter columns via reflection based on behavior and viscoelastic effect.
/// </summary>
public class SimulationCsvHandler : ISimulationStreamHandler<FileData>
{
    private readonly FileData _fileData;
    private readonly MechanicalBehaviorType _behaviorType;
    private readonly ViscoelasticEffect _viscoelasticEffect;
    private readonly FileStream _stream;
    private readonly StreamWriter _writer;
    private PropertyInfo[]? _activeProperties;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationCsvHandler"/> class.
    /// </summary>
    /// <param name="fileManager">The file manager used to construct target file information.</param>
    /// <param name="outputFileUri">The destination directory URI for the CSV file.</param>
    /// <param name="identifier">The unique analysis or curve fit identifier.</param>
    /// <param name="modelName">The name of the mechanical model.</param>
    /// <param name="behaviorType">The mechanical behavior type to filter columns.</param>
    /// <param name="viscoelasticEffect">The viscoelastic effect to filter columns.</param>
    public SimulationCsvHandler(
        IFileManager fileManager,
        string outputFileUri,
        string identifier,
        string modelName,
        MechanicalBehaviorType behaviorType,
        ViscoelasticEffect viscoelasticEffect)
    {
        _behaviorType = behaviorType;
        _viscoelasticEffect = viscoelasticEffect;

        string fileIdentifier = $"{identifier}_{modelName}";
        FileInfo fileInfo = fileManager.BuildTimebasedFileInfo(outputFileUri, fileIdentifier, FileExtensions.CommaSeparatedValues);
        _fileData = new FileData(fileInfo);

        _stream = fileInfo.Open(new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.SequentialScan | FileOptions.Asynchronous,
            BufferSize = 128 * 1024
        });
        _writer = new StreamWriter(_stream, new UTF8Encoding(false), 128 * 1024);
    }

    /// <inheritdoc />
    public async ValueTask OnNextAsync(MechanicalModelOutput output, int index)
    {
        if (index == 0)
        {
            // Discover model-specific output properties on the first emitted point and write header
            _activeProperties = DiscoverActiveProperties(output.GetType(), _behaviorType, _viscoelasticEffect);
            await WriteHeaderAsync(_writer, _activeProperties).ConfigureAwait(false);
        }

        await WriteRowAsync(_writer, output, _activeProperties!).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<FileData> CompleteAsync()
    {
        await _writer.FlushAsync().ConfigureAwait(false);
        return _fileData;
    }

    /// <summary>
    /// Inspects the concrete <see cref="MechanicalModelOutput"/> type and extracts properties marked with
    /// <see cref="MechanicalModelParameterAttribute"/> that apply to the current behavior and viscoelastic effect.
    /// </summary>
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

    /// <summary>
    /// Writes the CSV column headers, starting with 'Time' followed by all active property names.
    /// </summary>
    private static async ValueTask WriteHeaderAsync(StreamWriter writer, PropertyInfo[] properties)
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

    /// <summary>
    /// Formats and writes a single CSV row using invariant culture formatting to ensure numeric portability.
    /// </summary>
    private static async ValueTask WriteRowAsync(StreamWriter writer, MechanicalModelOutput output, PropertyInfo[] properties)
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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync().ConfigureAwait(false);
        await _stream.DisposeAsync().ConfigureAwait(false);
    }
}
