using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

// TODO: REFATORAR SEGUNDO AS CLASSES DO PROJETO SoftTissue.

/// <summary>
/// Pipeline step that serializes the full array of simulated output points into a CSV file.
/// </summary>

/// <param name="fileStorageService">Storage service responsible for resolving paths and opening file streams.</param>
/// <param name="uniqueIdentifier">Unique business identifier prefix for the generated file.</param>
public class MechanicalModelOutputFileWriterStep(IFileStorageService fileStorageService, string uniqueIdentifier) : IAsyncPipelineStep<MechanicalModelSimulationPayload, FileMetadata>
{
    private const string Context = "ExperimentalData";
    private static readonly ConcurrentDictionary<(LoadResponseRelationship, ViscoelasticEffect), (string Header, Func<MechanicalModelOutput, double?>[] Getters)> SchemaCache = new();

    /// <inheritdoc />
    public string Name => nameof(MechanicalModelOutputFileWriterStep);

    /// <inheritdoc />
    public async Task<FileMetadata> ExecuteAsync(MechanicalModelSimulationPayload payload, CancellationToken cancellationToken = default)
    {
        (string header, Func<MechanicalModelOutput, double?>[] getters) = SchemaCache.GetOrAdd(
            (payload.CurveFit.LoadResponseRelationship, payload.CurveFit.ViscoelasticEffect),
            BuildSchema);

        (Stream stream, FileMetadata fileMetada) = fileStorageService.CreateTimebased(Context, uniqueIdentifier, FileExtensions.CommaSeparatedValues, payload.CurveFit.MechanicalModelName);
        await using (stream)
        {
            await using CsvFileWriter session = new(stream, header);

            MechanicalModelOutput[] points = payload.SimulatedPoints;
            for (int i = 0; i < points.Length; i++)
            {
                MechanicalModelOutput point = points[i];
                await session.WriteRowAsync(buffer =>
                {
                    buffer.Append(point.Time.ToString(CultureInfo.InvariantCulture));
                    for (int p = 0; p < getters.Length; p++)
                    {
                        buffer.Append(CsvSeparators.Main);
                        double? val = getters[p](point);
                        if (val.HasValue)
                        {
                            buffer.Append(val.Value.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
        }

        return fileMetada;
    }

    private static (string Header, Func<MechanicalModelOutput, double?>[] Getters) BuildSchema((LoadResponseRelationship Relationship, ViscoelasticEffect Effect) key)
    {
        PropertyInfo[] allProps = typeof(MechanicalModelOutput).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        List<PropertyInfo> matching = [];

        for (int i = 0; i < allProps.Length; i++)
        {
            PropertyInfo prop = allProps[i];
            if (prop.Name == nameof(MechanicalModelOutput.Time))
            {
                continue;
            }

            MechanicalModelParameterAttribute? attr = prop.GetCustomAttribute<MechanicalModelParameterAttribute>();
            if (attr is not null && attr.CanMethodBeInvoked(key.Relationship, key.Effect))
            {
                matching.Add(prop);
            }
        }

        Func<MechanicalModelOutput, double?>[] getters = new Func<MechanicalModelOutput, double?>[matching.Count];
        StringBuilder headerBuilder = new();
        headerBuilder.Append(nameof(MechanicalModelOutput.Time));

        for (int i = 0; i < matching.Count; i++)
        {
            getters[i] = (Func<MechanicalModelOutput, double?>)Delegate.CreateDelegate(typeof(Func<MechanicalModelOutput, double?>), matching[i].GetGetMethod()!);
            headerBuilder.Append(CsvSeparators.Main).Append(matching[i].Name);
        }

        return (headerBuilder.ToString(), getters);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
